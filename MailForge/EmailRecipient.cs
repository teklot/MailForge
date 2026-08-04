using System;

namespace MailForge
{
    /// <summary>A single message recipient together with its role (To, Cc, or Bcc).</summary>
    public sealed class EmailRecipient
    {
        /// <summary>The recipient's address.</summary>
        public EmailAddress Address { get; }

        /// <summary>The role of this recipient on the message.</summary>
        public EmailRecipientType Type { get; }

        /// <summary>Creates a recipient from an address and role.</summary>
        public EmailRecipient(EmailAddress address, EmailRecipientType type = EmailRecipientType.To)
        {
            Address = address ?? throw new ArgumentNullException(nameof(address));
            Type = type;
        }

        /// <summary>Creates a recipient from a raw address string and role.</summary>
        public EmailRecipient(string address, EmailRecipientType type = EmailRecipientType.To, string? displayName = null)
            : this(new EmailAddress(address, displayName), type)
        {
        }

        /// <summary>Returns a string representation of this recipient.</summary>
        public override string ToString() => $"{Type}: {Address}";
    }
}
