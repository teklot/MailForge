using System;
using MailForge.Models;

namespace MailForge.Studio.Capture.Entities
{
    /// <summary>A captured recipient and its role on the message.</summary>
    public sealed class CapturedRecipient
    {
        /// <summary>The surrogate primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>The owning captured message.</summary>
        public Guid MessageId { get; set; }

        /// <summary>The role of this recipient (To, Cc, or Bcc).</summary>
        public EmailRecipientType Type { get; set; }

        /// <summary>The recipient's address.</summary>
        public string? Address { get; set; }

        /// <summary>An optional display name.</summary>
        public string? DisplayName { get; set; }
    }
}