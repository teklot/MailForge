using System;
using System.Collections.Generic;

namespace MailForge
{
    /// <summary>
    /// Base class for strongly typed transactional emails. Subclasses configure the message
    /// (sender, recipients, subject, and either a named template or an inline body) in their
    /// constructor and carry a model used to render it.
    /// </summary>
    public abstract class Email<TModel>
    {
        private readonly List<EmailRecipient> _to = new List<EmailRecipient>();
        private readonly List<EmailRecipient> _cc = new List<EmailRecipient>();
        private readonly List<EmailRecipient> _bcc = new List<EmailRecipient>();
        private readonly List<EmailAttachment> _attachments = new List<EmailAttachment>();
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Creates an email bound to a model instance.</summary>
        protected Email(TModel model)
        {
            Model = model;
        }

        /// <summary>The model used to render this email's templates.</summary>
        public TModel Model { get; }

        /// <summary>The sender address (optional when a global default is configured).</summary>
        public EmailAddress From { get; private set; }

        /// <summary>The message subject.</summary>
        public string Subject { get; private set; }

        /// <summary>The name of a registered template used to render this email.</summary>
        public string TemplateName { get; private set; }

        /// <summary>An inline HTML template (used when no named template is set).</summary>
        public string InlineHtml { get; private set; }

        /// <summary>An inline plain-text template (used when no named template is set).</summary>
        public string InlineText { get; private set; }

        /// <summary>The message importance level.</summary>
        public EmailPriority Priority { get; private set; } = EmailPriority.Normal;

        /// <summary>The primary recipients.</summary>
        public IReadOnlyList<EmailRecipient> To => _to;

        /// <summary>The carbon-copy recipients.</summary>
        public IReadOnlyList<EmailRecipient> Cc => _cc;

        /// <summary>The blind-carbon-copy recipients.</summary>
        public IReadOnlyList<EmailRecipient> Bcc => _bcc;

        /// <summary>Attachments and inline images.</summary>
        public IReadOnlyList<EmailAttachment> Attachments => _attachments;

        /// <summary>Custom message headers.</summary>
        public IReadOnlyDictionary<string, string> Headers => _headers;

        /// <summary>Tags used for routing and analytics.</summary>
        public IReadOnlyDictionary<string, string> Tags => _tags;

        /// <summary>Sets the sender address.</summary>
        protected Email<TModel> SetFrom(EmailAddress from)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            return this;
        }

        /// <summary>Sets the sender address from a raw address string.</summary>
        protected Email<TModel> SetFrom(string address, string displayName = null) => SetFrom(new EmailAddress(address, displayName));

        /// <summary>Adds a primary recipient.</summary>
        protected Email<TModel> AddTo(string address, string displayName = null) =>
            AddRecipient(_to, EmailRecipientType.To, new EmailAddress(address, displayName));

        /// <summary>Adds a primary recipient.</summary>
        protected Email<TModel> AddTo(EmailAddress address) => AddRecipient(_to, EmailRecipientType.To, address);

        /// <summary>Adds a carbon-copy recipient.</summary>
        protected Email<TModel> AddCc(string address, string displayName = null) =>
            AddRecipient(_cc, EmailRecipientType.Cc, new EmailAddress(address, displayName));

        /// <summary>Adds a carbon-copy recipient.</summary>
        protected Email<TModel> AddCc(EmailAddress address) => AddRecipient(_cc, EmailRecipientType.Cc, address);

        /// <summary>Adds a blind-carbon-copy recipient.</summary>
        protected Email<TModel> AddBcc(string address, string displayName = null) =>
            AddRecipient(_bcc, EmailRecipientType.Bcc, new EmailAddress(address, displayName));

        /// <summary>Adds a blind-carbon-copy recipient.</summary>
        protected Email<TModel> AddBcc(EmailAddress address) => AddRecipient(_bcc, EmailRecipientType.Bcc, address);

        /// <summary>Sets the message subject.</summary>
        protected Email<TModel> SetSubject(string subject)
        {
            Subject = subject ?? throw new ArgumentNullException(nameof(subject));
            return this;
        }

        /// <summary>Selects a named template registered on the MailForge builder.</summary>
        protected Email<TModel> UseTemplate(string templateName)
        {
            TemplateName = templateName ?? throw new ArgumentNullException(nameof(templateName));
            return this;
        }

        /// <summary>Sets an inline HTML template using {{Property.Path}} placeholders.</summary>
        protected Email<TModel> SetHtml(string template)
        {
            InlineHtml = template;
            return this;
        }

        /// <summary>Sets an inline plain-text template using {{Property.Path}} placeholders.</summary>
        protected Email<TModel> SetText(string template)
        {
            InlineText = template;
            return this;
        }

        /// <summary>Sets the message importance level.</summary>
        protected Email<TModel> SetPriority(EmailPriority priority)
        {
            Priority = priority;
            return this;
        }

        /// <summary>Adds an attachment or inline image.</summary>
        protected Email<TModel> Attach(EmailAttachment attachment)
        {
            _attachments.Add(attachment ?? throw new ArgumentNullException(nameof(attachment)));
            return this;
        }

        /// <summary>Adds a custom message header.</summary>
        protected Email<TModel> AddHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A header name is required.", nameof(name));
            _headers[name] = value;
            return this;
        }

        /// <summary>Adds a tag used for routing and analytics.</summary>
        protected Email<TModel> AddTag(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A tag name is required.", nameof(name));
            _tags[name] = value;
            return this;
        }

        private Email<TModel> AddRecipient(List<EmailRecipient> target, EmailRecipientType type, EmailAddress address)
        {
            target.Add(new EmailRecipient(address, type));
            return this;
        }
    }
}
