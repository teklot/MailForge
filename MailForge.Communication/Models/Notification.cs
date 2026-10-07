using System;
using System.Collections.Generic;

namespace MailForge.Communication.Models
{
    /// <summary>
    /// A channel-neutral notification produced by application code. It selects the delivery
    /// channel, carries a channel-specific <see cref="Content"/> payload, and shares metadata
    /// (priority, headers, tags) that channels and future routing can read.
    /// </summary>
    public sealed class Notification
    {
        /// <summary>The channel that will deliver this notification.</summary>
        public ChannelType Channel { get; }

        /// <summary>The channel-specific payload.</summary>
        public NotificationContent Content { get; }

        /// <summary>The importance level.</summary>
        public NotificationPriority Priority { get; }

        /// <summary>Custom headers forwarded to the channel provider where supported.</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>Arbitrary key/value tags used for routing and analytics.</summary>
        public IReadOnlyDictionary<string, string> Tags { get; }

        /// <summary>A unique identifier for this notification (used by auditing and delivery tracking).</summary>
        public string MessageId { get; }

        /// <summary>The time at which the notification was created.</summary>
        public DateTimeOffset CreatedAt { get; }

        /// <summary>Creates a notification.</summary>
        /// <param name="channel">The delivery channel.</param>
        /// <param name="content">The channel-specific payload.</param>
        /// <param name="priority">The importance level.</param>
        /// <param name="headers">Custom headers forwarded to the provider where supported.</param>
        /// <param name="tags">Arbitrary key/value tags for routing and analytics.</param>
        /// <param name="messageId">An optional explicit identifier; a new one is generated when omitted.</param>
        public Notification(
            ChannelType channel,
            NotificationContent content,
            NotificationPriority priority = NotificationPriority.Normal,
            IDictionary<string, string>? headers = null,
            IDictionary<string, string>? tags = null,
            string? messageId = null)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Priority = priority;
            Headers = headers == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase);
            Tags = tags == null
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(tags, StringComparer.OrdinalIgnoreCase);
            MessageId = messageId ?? Guid.NewGuid().ToString("N");
            CreatedAt = DateTimeOffset.UtcNow;
        }
    }
}