using System;
using System.Collections.Generic;
using MailForge.Communication.Models;

namespace MailForge.Communication.Routing
{
    /// <summary>
    /// A single rule evaluated by <see cref="NotificationRouter"/> to produce one or more
    /// <see cref="NotificationTarget"/> instances.
    /// </summary>
    public sealed class RoutingRule
    {
        /// <summary>An optional friendly name for diagnostics.</summary>
        public string? Name { get; set; }

        /// <summary>Evaluation priority; lower values are evaluated first. Defaults to 0.</summary>
        public int Priority { get; set; }

        /// <summary>Predicate that determines whether this rule matches the notification.</summary>
        public Func<Notification, bool> Match { get; set; } = _ => true;

        /// <summary>Target channels for matched notifications.</summary>
        public IReadOnlyList<ChannelType> Channels { get; set; } = Array.Empty<ChannelType>();

        /// <summary>Optional transformer applied to the notification per target.</summary>
        public Func<Notification, Notification>? Transform { get; set; }

        /// <summary>
        /// When true (the default), evaluation stops after this rule matches. Set to false to
        /// allow later rules to append additional targets (fan-out across rules).
        /// </summary>
        public bool StopOnMatch { get; set; } = true;
    }
}
