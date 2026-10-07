using System.Collections.Generic;

namespace MailForge.Communication.Models
{
    /// <summary>
    /// Immutable state shared by every <see cref="MailForge.Communication.Abstractions.INotificationMiddleware"/>
    /// in the channel pipeline for a single send operation.
    /// </summary>
    public sealed class NotificationDeliveryContext
    {
        /// <summary>The notification being sent.</summary>
        public Notification Notification { get; }

        /// <summary>Metadata written by middleware for other stages to consume.</summary>
        public IDictionary<string, object> Items { get; }

        /// <summary>Creates a delivery context.</summary>
        public NotificationDeliveryContext(Notification notification)
        {
            Notification = notification;
            Items = new Dictionary<string, object>();
        }
    }
}