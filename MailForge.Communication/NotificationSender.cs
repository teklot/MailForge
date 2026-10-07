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

        /// <summary>Creates a notification sender from its dependencies.</summary>
        public NotificationSender(IChannelRegistry registry, IEnumerable<INotificationMiddleware> middleware)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _middleware = (middleware ?? System.Linq.Enumerable.Empty<INotificationMiddleware>()).ToArray();
        }

        /// <summary>Sends a notification through its configured channel.</summary>
        public Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var channel = _registry.Get(notification.Channel);
            if (channel == null)
                return Task.FromResult(NotificationDeliveryResult.Failed(
                    notification,
                    new ProviderDeliveryResult(false, null, $"No channel is registered for '{notification.Channel.Name}'.")));

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