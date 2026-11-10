using System;
using System.Collections.Generic;
using MailForge.Communication.Models;

namespace MailForge.Communication.Routing
{
    /// <summary>Fluent builder for a single <see cref="RoutingRule"/>.</summary>
    public sealed class RoutingRuleBuilder
    {
        private readonly RoutingRule _rule;

        internal RoutingRuleBuilder(RoutingRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        /// <summary>Sets the channels this rule routes to.</summary>
        public RoutingRuleBuilder To(params ChannelType[] channels)
        {
            if (channels == null)
                throw new ArgumentNullException(nameof(channels));

            var list = new List<ChannelType>(channels.Length);
            for (var i = 0; i < channels.Length; i++)
            {
                var channel = channels[i];
                if (channel != null)
                    list.Add(channel);
            }

            _rule.Channels = list;
            return this;
        }

        /// <summary>Applies a transformation to notifications sent through this route.</summary>
        public RoutingRuleBuilder Transform(Func<Notification, Notification> transform)
        {
            _rule.Transform = transform ?? throw new ArgumentNullException(nameof(transform));
            return this;
        }

        /// <summary>Configures whether evaluation stops after this rule matches.</summary>
        public RoutingRuleBuilder Stop(bool stop = true)
        {
            _rule.StopOnMatch = stop;
            return this;
        }

        /// <summary>Sets the evaluation priority (lower values are evaluated first).</summary>
        public RoutingRuleBuilder Priority(int priority)
        {
            _rule.Priority = priority;
            return this;
        }
    }
}
