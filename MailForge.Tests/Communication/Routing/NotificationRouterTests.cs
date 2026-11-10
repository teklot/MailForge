using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Communication.Routing;
using MailForge.Models;

namespace MailForge.Tests.Communication.Routing
{
    public class NotificationRouterTests
    {
        private static Notification Create(ChannelType channel, NotificationContent? content = null, NotificationPriority priority = NotificationPriority.Normal, IDictionary<string, string>? tags = null, string? @event = null)
        {
            var message = EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Hi")
                .Text("Hi")
                .Build();
            return new Notification(channel, content ?? new EmailContent(message), priority, headers: null, tags: tags, @event: @event);
        }

        [Fact]
        public async Task RouteAsync_NoRules_FallsBackToOriginalChannel()
        {
            var router = new NotificationRouter(Array.Empty<RoutingRule>());
            var notification = Create(ChannelType.Telegram);

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Telegram, targets[0].Channel);
            Assert.Same(notification, targets[0].Notification);
        }

        [Fact]
        public async Task RouteAsync_WithDefaultChannel_FallsBackToDefault()
        {
            var router = new NotificationRouter(Array.Empty<RoutingRule>(), defaultChannel: ChannelType.Email);
            var notification = Create(ChannelType.Telegram);

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Email, targets[0].Channel);
        }

        [Fact]
        public async Task RouteAsync_WhenEvent_Matches()
        {
            var router = new NotificationRouter(new[]
            {
                new RoutingRule { Name = "breach", Match = n => n.Event == "security.breach", Channels = new[] { ChannelType.Telegram } }
            });
            var notification = Create(ChannelType.Email, @event: "security.breach");

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Telegram, targets[0].Channel);
            Assert.Equal("breach", targets[0].RouteName);
        }

        [Fact]
        public async Task RouteAsync_EventFromTag_IsUsed()
        {
            var router = new NotificationRouter(new[]
            {
                new RoutingRule { Match = n => n.Event == "device.offline", Channels = new[] { ChannelType.Telegram } }
            });
            var notification = Create(ChannelType.Email, tags: new Dictionary<string, string> { ["event"] = "device.offline" });

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Telegram, targets[0].Channel);
        }

        [Fact]
        public async Task RouteAsync_FirstMatchWins_WhenStopOnMatchTrue()
        {
            var router = new NotificationRouter(new[]
            {
                new RoutingRule { Priority = 0, Match = n => n.Priority == NotificationPriority.Urgent, Channels = new[] { ChannelType.Telegram }, StopOnMatch = true },
                new RoutingRule { Priority = 1, Match = n => true, Channels = new[] { ChannelType.Email }, StopOnMatch = true }
            });
            var notification = Create(ChannelType.Email, priority: NotificationPriority.Urgent);

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Telegram, targets[0].Channel);
        }

        [Fact]
        public async Task RouteAsync_MultipleTargets_WhenFanOut()
        {
            var router = new NotificationRouter(new[]
            {
                new RoutingRule { Match = n => true, Channels = new[] { ChannelType.Email, ChannelType.Telegram, ChannelType.Webhook }, StopOnMatch = true }
            });
            var notification = Create(ChannelType.Email);

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Equal(3, targets.Count);
            Assert.Equal(ChannelType.Email, targets[0].Channel);
            Assert.Equal(ChannelType.Telegram, targets[1].Channel);
            Assert.Equal(ChannelType.Webhook, targets[2].Channel);
        }

        [Fact]
        public async Task RouteAsync_TransformApplied()
        {
            var router = new NotificationRouter(new[]
            {
                new RoutingRule
                {
                    Match = n => true,
                    Channels = new[] { ChannelType.Telegram },
                    Transform = n =>
                    {
                        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var kv in n.Tags)
                            tags[kv.Key] = kv.Value;
                        tags["routed"] = "true";
                        return new Notification(ChannelType.Telegram, ((EmailContent)n.Content), n.Priority, new Dictionary<string, string>(n.Headers, StringComparer.OrdinalIgnoreCase), tags, n.MessageId, n.Event);
                    }
                }
            });
            var notification = Create(ChannelType.Email);

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.True(targets[0].Notification.Tags.ContainsKey("routed"));
        }

        [Fact]
        public async Task RouteAsync_RulesEvaluatedByPriority()
        {
            var router = new NotificationRouter(new[]
            {
                new RoutingRule { Priority = 10, Match = n => n.Priority == NotificationPriority.High, Channels = new[] { ChannelType.Email } },
                new RoutingRule { Priority = 0, Match = n => n.Priority == NotificationPriority.High, Channels = new[] { ChannelType.Telegram } }
            });
            var notification = Create(ChannelType.Email, priority: NotificationPriority.High);

            var targets = await router.RouteAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Telegram, targets[0].Channel);
        }
    }
}
