using Microsoft.Extensions.DependencyInjection;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Extensions;
using MailForge.Communication.Models;
using MailForge.Webhook;
using MailForge.Webhook.Extensions;

namespace MailForge.Tests
{
    public class WebhookDiTests
    {
        [Fact]
        public void UseWebhookChannel_RegistersWebhookChannel()
        {
            var services = new ServiceCollection();
            services.AddCommunication(builder => builder
                .UseWebhookChannel(new WebhookOptions { Endpoint = new Uri("https://hooks.example.com/notify") }));
            using var sp = services.BuildServiceProvider();

            var channel = sp.GetRequiredService<IChannelRegistry>().Get(ChannelType.Webhook);

            Assert.NotNull(channel);
            Assert.Equal(ChannelType.Webhook, channel!.Channel);
            Assert.IsType<WebhookChannel>(channel);
        }

        [Fact]
        public void UseWebhookChannel_ConfigureOverload_RegistersWebhookChannel()
        {
            var services = new ServiceCollection();
            services.AddCommunication(builder => builder
                .UseWebhookChannel(options => options.Endpoint = new Uri("https://hooks.example.com/notify")));
            using var sp = services.BuildServiceProvider();

            var channel = sp.GetRequiredService<IChannelRegistry>().Get(ChannelType.Webhook);

            Assert.NotNull(channel);
            Assert.IsType<WebhookChannel>(channel);
        }

        [Fact]
        public async Task SendAsync_ThroughRegisteredWebhookChannel_DeliversViaProvider()
        {
            var fake = new WebhookChannelTests.FakeNotificationProvider();
            var services = new ServiceCollection();
            services.AddCommunication(builder => builder.UseWebhookChannel(fake));
            using var sp = services.BuildServiceProvider();

            var sender = sp.GetRequiredService<INotificationSender>();
            var notification = new Notification(ChannelType.Webhook, new WebhookContent("{}"));
            var result = await sender.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.Sent, result.Status);
            Assert.Same(notification, fake.Received);
        }

        [Fact]
        public async Task UnknownChannel_WithoutWebhookChannel_Fails()
        {
            var services = new ServiceCollection();
            services.AddCommunication(builder => { });
            using var sp = services.BuildServiceProvider();

            var sender = sp.GetRequiredService<INotificationSender>();
            var result = await sender.SendAsync(
                new Notification(ChannelType.Webhook, new WebhookContent("{}")),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("webhook", result.Details);
        }
    }
}
