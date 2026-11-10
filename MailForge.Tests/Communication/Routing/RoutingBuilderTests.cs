using MailForge.Communication.Models;
using MailForge.Communication.Routing;
using MailForge.Models;

namespace MailForge.Tests.Communication.Routing
{
    public class RoutingBuilderTests
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
        public async Task RoutingBuilder_WhenEvent_RoutesCorrectly()
        {
            var router = new RoutingBuilder()
                .WhenEvent("device.offline", ChannelType.Telegram)
                .Build();

            var targets = await router.RouteAsync(Create(ChannelType.Email, @event: "device.offline"), TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Telegram, targets[0].Channel);
        }

        [Fact]
        public async Task RoutingBuilder_WhenPriority_RoutesCorrectly()
        {
            var router = new RoutingBuilder()
                .WhenPriority(NotificationPriority.Urgent, ChannelType.Webhook)
                .Build();

            var targets = await router.RouteAsync(Create(ChannelType.Email, priority: NotificationPriority.Urgent), TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Webhook, targets[0].Channel);
        }

        [Fact]
        public async Task RoutingBuilder_WhenTag_RoutesCorrectly()
        {
            var router = new RoutingBuilder()
                .WhenTag("severity", "warning", ChannelType.Email)
                .Build();

            var targets = await router.RouteAsync(Create(ChannelType.Telegram, tags: new Dictionary<string, string> { ["severity"] = "warning" }), TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Email, targets[0].Channel);
        }

        [Fact]
        public async Task RoutingBuilder_WhenPredicate_RoutesCorrectly()
        {
            var router = new RoutingBuilder()
                .When(n => n.Tags.ContainsKey("alert"), ChannelType.Telegram)
                .Build();

            var targets = await router.RouteAsync(Create(ChannelType.Email, tags: new Dictionary<string, string> { ["alert"] = "true" }), TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Telegram, targets[0].Channel);
        }

        [Fact]
        public async Task RoutingBuilder_Default_Fallback()
        {
            var router = new RoutingBuilder()
                .WhenEvent("other", ChannelType.Telegram)
                .Default(ChannelType.Email)
                .Build();

            var targets = await router.RouteAsync(Create(ChannelType.Webhook, @event: "something"), TestContext.Current.CancellationToken);

            Assert.Single(targets);
            Assert.Equal(ChannelType.Email, targets[0].Channel);
        }

        [Fact]
        public async Task RoutingBuilder_RouteWithConfigure()
        {
            var router = new RoutingBuilder()
                .Route("critical", r => r.Priority(0).Stop(true).To(ChannelType.Telegram))
                .Build();

            var targets = await router.RouteAsync(Create(ChannelType.Email, priority: NotificationPriority.Urgent), TestContext.Current.CancellationToken);

            // Match defaults to false in Route(); without setting a predicate it won't match
            Assert.Single(targets);
            Assert.Equal(ChannelType.Email, targets[0].Channel);
        }
    }
}
