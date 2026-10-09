using Microsoft.Extensions.DependencyInjection;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Extensions;
using MailForge.Communication.Models;
using MailForge.Telegram;
using MailForge.Telegram.Extensions;

namespace MailForge.Tests
{
    public class TelegramDiTests
    {
        [Fact]
        public void UseTelegramChannel_RegistersTelegramChannel()
        {
            var services = new ServiceCollection();
            services.AddCommunication(builder => builder
                .UseTelegramChannel(new TelegramOptions { BotToken = "123:abc" }));
            using var sp = services.BuildServiceProvider();

            var channel = sp.GetRequiredService<IChannelRegistry>().Get(ChannelType.Telegram);

            Assert.NotNull(channel);
            Assert.Equal(ChannelType.Telegram, channel!.Channel);
            Assert.IsType<TelegramChannel>(channel);
        }

        [Fact]
        public void UseTelegramChannel_ConfigureOverload_RegistersTelegramChannel()
        {
            var services = new ServiceCollection();
            services.AddCommunication(builder => builder
                .UseTelegramChannel(options => options.BotToken = "123:abc"));
            using var sp = services.BuildServiceProvider();

            var channel = sp.GetRequiredService<IChannelRegistry>().Get(ChannelType.Telegram);

            Assert.NotNull(channel);
            Assert.IsType<TelegramChannel>(channel);
        }

        [Fact]
        public async Task SendAsync_ThroughRegisteredTelegramChannel_DeliversViaProvider()
        {
            var fake = new TelegramChannelTests.FakeNotificationProvider();
            var services = new ServiceCollection();
            services.AddCommunication(builder => builder.UseTelegramChannel(fake));
            using var sp = services.BuildServiceProvider();

            var sender = sp.GetRequiredService<INotificationSender>();
            var notification = new Notification(ChannelType.Telegram, new TelegramContent("42", "Hi"));
            var result = await sender.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.Sent, result.Status);
            Assert.Same(notification, fake.Received);
        }

        [Fact]
        public async Task UnknownChannel_WithoutTelegramChannel_Fails()
        {
            var services = new ServiceCollection();
            services.AddCommunication(builder => { });
            using var sp = services.BuildServiceProvider();

            var sender = sp.GetRequiredService<INotificationSender>();
            var result = await sender.SendAsync(
                new Notification(ChannelType.Telegram, new TelegramContent("42", "Hi")),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("telegram", result.Details);
        }
    }
}
