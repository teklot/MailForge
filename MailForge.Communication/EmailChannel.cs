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
    /// The email channel. It adapts the existing email pipeline to the notification model:
    /// a <see cref="Notification"/> carrying an <see cref="EmailContent"/> payload is handed
    /// to the configured <see cref="IEmailSender"/>, which keeps the exact email validation,
    /// middleware, retry, and audit semantics. The email public API surface is unchanged.
    /// </summary>
    public sealed class EmailChannel : IChannel
    {
        private readonly IEmailSender _sender;

        /// <summary>Creates an email channel over the email pipeline.</summary>
        public EmailChannel(IEmailSender sender)
        {
            _sender = sender ?? throw new ArgumentNullException(nameof(sender));
        }

        /// <summary>The channel delivered by this instance.</summary>
        public ChannelType Channel => ChannelType.Email;

        /// <summary>
        /// Sends an email notification. The payload must be an <see cref="EmailContent"/>.
        /// </summary>
        public async Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (!(notification.Content is EmailContent emailContent))
                return NotificationDeliveryResult.FailedValidation(
                    notification,
                    $"The email channel requires an {nameof(EmailContent)} payload but received '{notification.Content.GetType().Name}'.");

            var emailResult = await _sender.SendAsync(emailContent.Message, cancellationToken);

            switch (emailResult.Status)
            {
                case EmailDeliveryStatus.Sent:
                    return NotificationDeliveryResult.Sent(notification, emailResult.ProviderResult);
                case EmailDeliveryStatus.FailedValidation:
                    return NotificationDeliveryResult.FailedValidation(notification, emailResult.Details ?? "The email did not pass validation.");
                default:
                    return NotificationDeliveryResult.Failed(notification, emailResult.ProviderResult);
            }
        }
    }
}