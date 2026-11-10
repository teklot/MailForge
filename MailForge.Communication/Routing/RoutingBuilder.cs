using System;
using System.Collections.Generic;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;

namespace MailForge.Communication.Routing
{
    /// <summary>Fluent builder for configuring notification routing rules.</summary>
    public sealed class RoutingBuilder
    {
        private readonly List<RoutingRule> _rules = new List<RoutingRule>();
        private ChannelType? _defaultChannel;

        /// <summary>Adds a named rule with custom configuration.</summary>
        public RoutingBuilder Route(string name, Action<RoutingRuleBuilder> configure)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Route name must not be empty.", nameof(name));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var rule = new RoutingRule { Name = name, Match = _ => false };
            configure(new RoutingRuleBuilder(rule));
            _rules.Add(rule);
            return this;
        }

        /// <summary>Adds a rule that matches when the predicate returns true.</summary>
        public RoutingBuilder When(Func<Notification, bool> predicate, params ChannelType[] channels)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            var rule = new RoutingRule { Match = predicate };
            if (channels != null && channels.Length > 0)
            {
                var list = new List<ChannelType>(channels.Length);
                for (var i = 0; i < channels.Length; i++)
                    if (channels[i] != null) list.Add(channels[i]);
                rule.Channels = list;
            }

            _rules.Add(rule);
            return this;
        }

        /// <summary>Adds a rule that matches notifications with the specified priority.</summary>
        public RoutingBuilder WhenPriority(NotificationPriority priority, params ChannelType[] channels)
        {
            var rule = new RoutingRule { Match = n => n.Priority == priority };
            if (channels != null && channels.Length > 0)
            {
                var list = new List<ChannelType>(channels.Length);
                for (var i = 0; i < channels.Length; i++)
                    if (channels[i] != null) list.Add(channels[i]);
                rule.Channels = list;
            }

            _rules.Add(rule);
            return this;
        }

        /// <summary>Adds a rule that matches notifications with the specified event type.</summary>
        public RoutingBuilder WhenEvent(string @event, params ChannelType[] channels)
        {
            if (string.IsNullOrWhiteSpace(@event))
                throw new ArgumentException("Event must not be empty.", nameof(@event));

            var rule = new RoutingRule
            {
                Match = n => string.Equals(n.Event, @event, StringComparison.OrdinalIgnoreCase)
            };

            if (channels != null && channels.Length > 0)
            {
                var list = new List<ChannelType>(channels.Length);
                for (var i = 0; i < channels.Length; i++)
                    if (channels[i] != null) list.Add(channels[i]);
                rule.Channels = list;
            }

            _rules.Add(rule);
            return this;
        }

        /// <summary>Adds a rule that matches notifications with the specified tag key/value.</summary>
        public RoutingBuilder WhenTag(string key, string value, params ChannelType[] channels)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Tag key must not be empty.", nameof(key));

            var rule = new RoutingRule
            {
                Match = n => n.Tags.TryGetValue(key, out var v) && string.Equals(v, value, StringComparison.OrdinalIgnoreCase)
            };

            if (channels != null && channels.Length > 0)
            {
                var list = new List<ChannelType>(channels.Length);
                for (var i = 0; i < channels.Length; i++)
                    if (channels[i] != null) list.Add(channels[i]);
                rule.Channels = list;
            }

            _rules.Add(rule);
            return this;
        }

        /// <summary>Sets the fallback channel used when no rules match.</summary>
        public RoutingBuilder Default(ChannelType channel)
        {
            _defaultChannel = channel ?? throw new ArgumentNullException(nameof(channel));
            return this;
        }

        /// <summary>Builds the configured router.</summary>
        public INotificationRouter Build()
        {
            return new NotificationRouter(_rules, _defaultChannel);
        }
    }
}
