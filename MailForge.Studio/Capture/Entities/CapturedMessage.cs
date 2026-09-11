using System;
using System.Collections.Generic;
using MailForge.Models;

namespace MailForge.Studio.Capture.Entities
{
    /// <summary>A message captured by MailForge Studio.</summary>
    public sealed class CapturedMessage
    {
        /// <summary>The surrogate primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>The MailForge message id.</summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>The SMTP envelope sender (MAIL FROM), when captured via the SMTP relay.</summary>
        public string? EnvelopeFrom { get; set; }

        /// <summary>The displayed sender (From header).</summary>
        public string? From { get; set; }

        /// <summary>The message subject.</summary>
        public string? Subject { get; set; }

        /// <summary>The HTML body.</summary>
        public string? HtmlBody { get; set; }

        /// <summary>The plain-text body.</summary>
        public string? TextBody { get; set; }

        /// <summary>The message importance level.</summary>
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;

        /// <summary>The raw MIME message (.eml), when captured via the SMTP relay.</summary>
        public byte[]? RawMime { get; set; }

        /// <summary>The time at which the message was captured.</summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>The message recipients.</summary>
        public ICollection<CapturedRecipient> Recipients { get; set; } = new List<CapturedRecipient>();

        /// <summary>The message attachments and inline images.</summary>
        public ICollection<CapturedAttachment> Attachments { get; set; } = new List<CapturedAttachment>();

        /// <summary>The custom message headers.</summary>
        public ICollection<CapturedHeader> Headers { get; set; } = new List<CapturedHeader>();
    }
}