using System;
using MailForge.Models;

namespace MailForge.Communication.Models
{
    /// <summary>
    /// The email channel payload: a fully built <see cref="EmailMessage"/> ready for the
    /// email pipeline.
    /// </summary>
    public sealed class EmailContent : NotificationContent
    {
        /// <summary>The email message to deliver.</summary>
        public EmailMessage Message { get; }

        /// <summary>Creates an email channel payload.</summary>
        public EmailContent(EmailMessage message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>Creates an email channel payload from a message.</summary>
        public static EmailContent FromMessage(EmailMessage message) => new EmailContent(message);
    }
}