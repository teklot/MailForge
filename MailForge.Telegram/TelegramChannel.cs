using System;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Telegram
{
    /// <summary>
    /// The Telegram channel. It validates the payload shape and hands the notification to the
    /// configured <see cref="INotificationProvider"/>, translating the provider outcome into a
    /// <see cref="NotificationDeliveryResult"/>. Provider transport and rate-limit failures
    /// surface as <see cref="NotificationException"/> for the notification pipeline.
    /// </summary>
    public sealed class TelegramChannel : IChannel
    {
        private readonly INotificationProvider _provider;

        /// <summary>Creates a Telegram channel over a provider.</summary>
        public TelegramChannel(INotificationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>The channel delivered by this instance.</summary>
        public ChannelType Channel => ChannelType.Telegram;

        /// <summary>
        /// Sends a Telegram notification. The payload must be a <see cref="TelegramContent"/>.
        /// </summary>
        public async Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (!(notification.Content is TelegramContent telegramContent))
                return NotificationDeliveryResult.FailedValidation(
                    notification,
                    $"The Telegram channel requires a {nameof(TelegramContent)} payload but received '{notification.Content.GetType().Name}'.");

            var providerResult = await _provider.SendAsync(notification, cancellationToken);

            return providerResult.Succeeded
                ? NotificationDeliveryResult.Sent(notification, providerResult)
                : NotificationDeliveryResult.Failed(notification, providerResult);
        }
    }
}
