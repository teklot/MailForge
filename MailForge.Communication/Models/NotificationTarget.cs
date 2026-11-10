using System;

namespace MailForge.Communication.Models
{
    /// <summary>
    /// A routing target produced by <see cref="Abstractions.INotificationRouter"/> describing
    /// which channel to deliver through and which notification instance to send.
    /// </summary>
    public sealed class NotificationTarget
    {
        /// <summary>The channel that should deliver this target.</summary>
        public ChannelType Channel { get; }

        /// <summary>The notification to deliver for this target (may be transformed by a route).</summary>
        public Notification Notification { get; }

        /// <summary>An optional name of the route that produced this target (for diagnostics and auditing).</summary>
        public string? RouteName { get; }

        /// <summary>Creates a notification target.</summary>
        /// <param name="channel">The target channel.</param>
        /// <param name="notification">The notification to deliver.</param>
        /// <param name="routeName">An optional route name.</param>
        public NotificationTarget(ChannelType channel, Notification notification, string? routeName = null)
        {
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Notification = notification ?? throw new ArgumentNullException(nameof(notification));
            RouteName = routeName;
        }
    }
}
