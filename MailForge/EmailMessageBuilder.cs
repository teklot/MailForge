using System;
using System.Collections.Generic;

namespace MailForge
{
    /// <summary>Fluent builder used to construct an <see cref="EmailMessage"/>.</summary>
    public sealed class EmailMessageBuilder
    {
        private EmailAddress _from;
        private string _subject;
        private string _htmlBody;
        private string _textBody;
        private EmailPriority _priority = EmailPriority.Normal;
        private string _messageId;
        private readonly List<EmailRecipient> _recipients = new List<EmailRecipient>();
        private readonly List<EmailAttachment> _attachments = new List<EmailAttachment>();
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Sets the sender address.</summary>
        public EmailMessageBuilder From(EmailAddress from)
        {
            _from = from ?? throw new ArgumentNullException(nameof(from));
            return this;
        }

        /// <summary>Sets the sender address from a raw address string.</summary>
        public EmailMessageBuilder From(string address, string displayName = null) => From(new EmailAddress(address, displayName));

        /// <summary>Adds a primary recipient.</summary>
        public EmailMessageBuilder To(string address, string displayName = null) => AddRecipient(EmailRecipientType.To, address, displayName);

        /// <summary>Adds a primary recipient.</summary>
        public EmailMessageBuilder To(EmailAddress address) => AddRecipient(EmailRecipientType.To, address);

        /// <summary>Adds a carbon-copy recipient.</summary>
        public EmailMessageBuilder Cc(string address, string displayName = null) => AddRecipient(EmailRecipientType.Cc, address, displayName);

        /// <summary>Adds a carbon-copy recipient.</summary>
        public EmailMessageBuilder Cc(EmailAddress address) => AddRecipient(EmailRecipientType.Cc, address);

        /// <summary>Adds a blind-carbon-copy recipient.</summary>
        public EmailMessageBuilder Bcc(string address, string displayName = null) => AddRecipient(EmailRecipientType.Bcc, address, displayName);

        /// <summary>Adds a blind-carbon-copy recipient.</summary>
        public EmailMessageBuilder Bcc(EmailAddress address) => AddRecipient(EmailRecipientType.Bcc, address);

        /// <summary>Sets the message subject.</summary>
        public EmailMessageBuilder Subject(string subject)
        {
            _subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        /// <summary>Sets the HTML body.</summary>
        public EmailMessageBuilder Html(string htmlBody)
        {
            _htmlBody = htmlBody;
            return this;
        }

        /// <summary>Sets the plain-text body.</summary>
        public EmailMessageBuilder Text(string textBody)
        {
            _textBody = textBody;
            return this;
        }

        /// <summary>Sets the message importance level.</summary>
        public EmailMessageBuilder Priority(EmailPriority priority)
        {
            _priority = priority;
            return this;
        }

        /// <summary>Adds an attachment or inline image.</summary>
        public EmailMessageBuilder Attachment(EmailAttachment attachment)
        {
            _attachments.Add(attachment ?? throw new ArgumentNullException(nameof(attachment)));
            return this;
        }

        /// <summary>Adds a custom message header.</summary>
        public EmailMessageBuilder Header(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A header name is required.", nameof(name));
            _headers[name] = value;
            return this;
        }

        /// <summary>Adds a tag used for routing and analytics.</summary>
        public EmailMessageBuilder Tag(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A tag name is required.", nameof(name));
            _tags[name] = value;
            return this;
        }

        /// <summary>Overrides the auto-generated message identifier.</summary>
        public EmailMessageBuilder MessageId(string messageId)
        {
            _messageId = messageId;
            return this;
        }

        /// <summary>Builds the message.</summary>
        public EmailMessage Build() => new EmailMessage(
            _from,
            _recipients,
            _subject,
            _htmlBody,
            _textBody,
            _priority,
            _attachments,
            _headers,
            _tags,
            _messageId);

        private EmailMessageBuilder AddRecipient(EmailRecipientType type, string address, string displayName) =>
            AddRecipient(type, new EmailAddress(address, displayName));

        private EmailMessageBuilder AddRecipient(EmailRecipientType type, EmailAddress address)
        {
            _recipients.Add(new EmailRecipient(address, type));
            return this;
        }
    }
}
