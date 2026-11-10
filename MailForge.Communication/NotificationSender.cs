using System;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.Communication
{
    /// <summary>
    /// The default <see cref="INotificationSender"/> implementation. It resolves the channel
    /// for a notification, runs the configured middleware pipeline, and delegates delivery to
    /// the channel.
    /// </summary>
    public sealed class NotificationSender : INotificationSender
    {
        private readonly IChannelRegistry _registry;
        private readonly IReadOnlyList<INotificationMiddleware> _middleware;
        private readonly INotificationRouter? _router;

        /// <summary>Creates a notification sender from its dependencies.</summary>
        public NotificationSender(IChannelRegistry registry, IEnumerable<INotificationMiddleware> middleware, INotificationRouter? router = null)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _middleware = (middleware ?? System.Linq.Enumerable.Empty<INotificationMiddleware>()).ToArray();
            _router = router;
        }

        /// <summary>Sends a notification through its configured channel.</summary>
        public async Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (_router == null)
                return await SendToChannelAsync(notification, notification.Channel, cancellationToken);

            IReadOnlyList<NotificationTarget> targets;
            try
            {
                targets = await _router.RouteAsync(notification, cancellationToken);
            }
            catch (NotificationException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new NotificationException(
                    $"The notification router failed to route the notification: {exception.Message}",
                    exception,
                    isTransient: true);
            }

            if (targets == null || targets.Count == 0)
                return NotificationDeliveryResult.Failed(
                    notification,
                    new ProviderDeliveryResult(false, null, "No route targets resolved."));

            if (targets.Count == 1)
            {
                var single = targets[0];
                var targetChannel = single?.Channel ?? notification.Channel;
                var targetNotification = single?.Notification ?? notification;
                return await SendToChannelAsync(targetNotification, targetChannel, cancellationToken);
            }

            var results = new List<NotificationDeliveryResult>(targets.Count);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                if (target == null)
                    continue;

                var targetChannel = target.Channel ?? notification.Channel;
                var targetNotification = target.Notification ?? notification;
                results.Add(await SendToChannelAsync(targetNotification, targetChannel, cancellationToken));
            }

            if (results.Count == 0)
                return NotificationDeliveryResult.Failed(
                    notification,
                    new ProviderDeliveryResult(false, null, "No route targets resolved."));

            if (results.Count == 1)
                return results[0];

            var anySuccess = false;
            var detailsParts = new System.Collections.Generic.List<string>(results.Count);
            for (var i = 0; i < results.Count; i++)
            {
                var result = results[i];
                if (result.Succeeded)
                    anySuccess = true;
                var detail = string.IsNullOrWhiteSpace(result.Details) ? result.Status.ToString() : result.Details!;
                detailsParts.Add(detail);
            }

            var aggregateDetails = string.Join(" | ", detailsParts);
            string? providerMessageId = null;
            if (anySuccess)
            {
                for (var i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    if (r.Succeeded && r.ProviderResult != null && !string.IsNullOrWhiteSpace(r.ProviderResult.ProviderMessageId))
                    {
                        providerMessageId = r.ProviderResult.ProviderMessageId;
                        break;
                    }
                }
            }
            var aggregateProvider = new ProviderDeliveryResult(
                anySuccess,
                providerMessageId,
                aggregateDetails);

            if (anySuccess)
                return NotificationDeliveryResult.Sent(notification, aggregateProvider);

            return NotificationDeliveryResult.Failed(notification, aggregateProvider);
        }

        private Task<NotificationDeliveryResult> SendToChannelAsync(Notification notification, ChannelType channelType, CancellationToken cancellationToken)
        {
            var channel = _registry.Get(channelType);
            if (channel == null)
                return Task.FromResult(NotificationDeliveryResult.Failed(
                    notification,
                    new ProviderDeliveryResult(false, null, $"No channel is registered for '{channelType.Name}'.")));

            return RunPipelineAsync(new NotificationDeliveryContext(notification), 0, channel, cancellationToken);
        }

        private async Task<NotificationDeliveryResult> RunPipelineAsync(NotificationDeliveryContext context, int index, IChannel channel, CancellationToken cancellationToken)
        {
            if (index < _middleware.Count)
                return await _middleware[index].InvokeAsync(context, () => RunPipelineAsync(context, index + 1, channel, cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await channel.SendAsync(context.Notification, cancellationToken);
            }
            catch (NotificationException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new NotificationException(
                    $"The channel '{channel.Channel.Name}' failed to send the notification: {exception.Message}",
                    exception,
                    isTransient: true);
            }
        }
    }
}