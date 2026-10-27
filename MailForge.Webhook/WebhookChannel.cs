using System;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Webhook
{
    /// <summary>
    /// The Webhook channel. It validates the payload shape and hands the notification to the
    /// configured <see cref="INotificationProvider"/>, translating the provider outcome into a
    /// <see cref="NotificationDeliveryResult"/>. Provider transport and rate-limit failures
    /// surface as <see cref="NotificationException"/> for the notification pipeline.
    /// </summary>
    public sealed class WebhookChannel : IChannel
    {
        private readonly INotificationProvider _provider;

        /// <summary>Creates a Webhook channel over a provider.</summary>
        public WebhookChannel(INotificationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>The channel delivered by this instance.</summary>
        public ChannelType Channel => ChannelType.Webhook;

        /// <summary>
        /// Sends a Webhook notification. The payload must be a <see cref="WebhookContent"/>.
        /// </summary>
        public async Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (!(notification.Content is WebhookContent))
                return NotificationDeliveryResult.FailedValidation(
                    notification,
                    $"The Webhook channel requires a {nameof(WebhookContent)} payload but received '{notification.Content.GetType().Name}'.");

            var providerResult = await _provider.SendAsync(notification, cancellationToken);

            return providerResult.Succeeded
                ? NotificationDeliveryResult.Sent(notification, providerResult)
                : NotificationDeliveryResult.Failed(notification, providerResult);
        }
    }
}
