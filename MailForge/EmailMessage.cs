using System;
using System.Collections.Generic;
using System.Linq;

namespace MailForge
{
    /// <summary>
    /// An immutable, provider-agnostic email message. This is the payload exchanged between
    /// the framework, the middleware pipeline, and every <see cref="IEmailProvider"/>.
    /// </summary>
    public sealed class EmailMessage
    {
        /// <summary>The sender of the message.</summary>
        public EmailAddress From { get; }

        /// <summary>The message subject.</summary>
        public string Subject { get; }

        /// <summary>The HTML body (may be null when only a plain-text body is provided).</summary>
        public string HtmlBody { get; }

        /// <summary>The plain-text body (may be null when only an HTML body is provided).</summary>
        public string TextBody { get; }

        /// <summary>The message importance level.</summary>
        public EmailPriority Priority { get; }

        /// <summary>All recipients, each tagged with its role (To, Cc, or Bcc).</summary>
        public IReadOnlyList<EmailRecipient> Recipients { get; }

        /// <summary>Attachments and inline images.</summary>
        public IReadOnlyList<EmailAttachment> Attachments { get; }

        /// <summary>Custom message headers.</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>Arbitrary key/value tags used for routing and analytics.</summary>
        public IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>A unique identifier for this message (used by auditing and delivery tracking).</summary>
        public string MessageId { get; }

        /// <summary>The time at which the message was created.</summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>Creates a new message.</summary>
        public EmailMessage(
            EmailAddress from,
            IEnumerable<EmailRecipient> recipients,
            string subject,
            string htmlBody = null,
            string textBody = null,
            EmailPriority priority = EmailPriority.Normal,
            IEnumerable<EmailAttachment> attachments = null,
            IDictionary<string, string> headers = null,
            IDictionary<string, string> tags = null,
            string messageId = null)
        {
            From = from;
            Subject = subject;
            HtmlBody = htmlBody;
            TextBody = textBody;
            Priority = priority;
            Recipients = (recipients ?? Enumerable.Empty<EmailRecipient>()).ToArray();
            Attachments = (attachments ?? Enumerable.Empty<EmailAttachment>()).ToArray();
            Headers = headers == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
            Tags = tags == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(tags, StringComparer.OrdinalIgnoreCase);
            MessageId = messageId ?? Guid.NewGuid().ToString("N");
            CreatedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>The primary (To) recipients.</summary>
        public IReadOnlyList<EmailRecipient> ToRecipients => Recipients.Where(r => r.Type == EmailRecipientType.To).ToArray();

        /// <summary>The carbon-copy (Cc) recipients.</summary>
        public IReadOnlyList<EmailRecipient> CcRecipients => Recipients.Where(r => r.Type == EmailRecipientType.Cc).ToArray();

        /// <summary>The blind-carbon-copy (Bcc) recipients.</summary>
        public IReadOnlyList<EmailRecipient> BccRecipients => Recipients.Where(r => r.Type == EmailRecipientType.Bcc).ToArray();

        /// <summary>Returns a fluent builder for constructing a message.</summary>
        public static EmailMessageBuilder Create() => new EmailMessageBuilder();
    }
}
