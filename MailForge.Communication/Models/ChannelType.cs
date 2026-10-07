using System;

namespace MailForge.Communication.Models
{
    /// <summary>
    /// Identifies a communication channel such as Email, Telegram, or Webhook. Channels are
    /// compared by name (case-insensitively), and new channels are added as constants without
    /// breaking existing consumers.
    /// </summary>
    public sealed class ChannelType : IEquatable<ChannelType>
    {
        /// <summary>The email channel (SMTP, SES, Resend, Postmark, Mailgun, Brevo, ZeptoMail, Azure CS).</summary>
        public static readonly ChannelType Email = new ChannelType("email");

        /// <summary>The SMS channel.</summary>
        public static readonly ChannelType Sms = new ChannelType("sms");

        /// <summary>The Telegram channel.</summary>
        public static readonly ChannelType Telegram = new ChannelType("telegram");

        /// <summary>The webhook channel.</summary>
        public static readonly ChannelType Webhook = new ChannelType("webhook");

        /// <summary>The WhatsApp channel.</summary>
        public static readonly ChannelType WhatsApp = new ChannelType("whatsapp");

        /// <summary>The push-notification channel.</summary>
        public static readonly ChannelType Push = new ChannelType("push");

        /// <summary>The channel name.</summary>
        public string Name { get; }

        /// <summary>Creates a channel type with the given name.</summary>
        public ChannelType(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>Compares two channel types by name (case-insensitively).</summary>
        public bool Equals(ChannelType? other) =>
            other != null && string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);

        /// <summary>Compares an object to this channel type.</summary>
        public override bool Equals(object? obj) => obj is ChannelType other && Equals(other);

        /// <summary>Returns a hash code derived from the name.</summary>
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);

        /// <summary>Returns the channel name.</summary>
        public override string ToString() => Name;
    }
}