using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;

namespace MailForge.Communication.Routing
{
    /// <summary>
    /// Default implementation of <see cref="INotificationRouter"/> that evaluates
    /// <see cref="RoutingRule"/> instances in priority order.
    /// </summary>
    public sealed class NotificationRouter : INotificationRouter
    {
        private readonly IReadOnlyList<RoutingRule> _rules;
        private readonly ChannelType? _defaultChannel;

        /// <summary>Creates a router.</summary>
        /// <param name="rules">The rules to evaluate.</param>
        /// <param name="defaultChannel">Fallback channel when no rules match.</param>
        public NotificationRouter(IEnumerable<RoutingRule> rules, ChannelType? defaultChannel = null)
        {
            if (rules == null)
                throw new ArgumentNullException(nameof(rules));

            _rules = rules
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            _defaultChannel = defaultChannel;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<NotificationTarget>> RouteAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            cancellationToken.ThrowIfCancellationRequested();

            var targets = new List<NotificationTarget>();
            foreach (var rule in _rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool matches;
                try
                {
                    matches = rule.Match(notification);
                }
                catch
                {
                    matches = false;
                }

                if (!matches)
                    continue;

                if (rule.Channels.Count == 0)
                {
                    if (rule.StopOnMatch)
                        break;
                    continue;
                }

                foreach (var channel in rule.Channels)
                {
                    if (channel == null)
                        continue;

                    var targetNotification = rule.Transform?.Invoke(notification) ?? notification;
                    targets.Add(new NotificationTarget(channel, targetNotification, rule.Name));
                }

                if (rule.StopOnMatch)
                    break;
            }

            if (targets.Count > 0)
                return Task.FromResult<IReadOnlyList<NotificationTarget>>(targets);

            var fallbackChannel = _defaultChannel ?? notification.Channel;
            return Task.FromResult<IReadOnlyList<NotificationTarget>>(new[]
            {
                new NotificationTarget(fallbackChannel, notification)
            });
        }
    }
}
