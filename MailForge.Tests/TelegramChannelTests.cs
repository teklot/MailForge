using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Models;
using MailForge.Telegram;

namespace MailForge.Tests
{
    public class TelegramChannelTests
    {
        internal sealed class FakeNotificationProvider : INotificationProvider
        {
            public string Name => "Fake";
            public ChannelType Channel => ChannelType.Telegram;
            public ProviderDeliveryResult Result { get; set; } = ProviderDeliveryResult.Success("fake-1");
            public Exception? Throw { get; set; }
            public Notification? Received { get; private set; }

            public Task<ProviderDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
            {
                Received = notification;
                if (Throw != null)
                    return Task.FromException<ProviderDeliveryResult>(Throw);
                return Task.FromResult(Result);
            }
        }

        [Fact]
        public void Channel_DeliversTelegram()
        {
            var channel = new TelegramChannel(new FakeNotificationProvider());
            Assert.Equal(ChannelType.Telegram, channel.Channel);
        }

        [Fact]
        public async Task SendAsync_TelegramContent_MapsSuccessToSent()
        {
            var provider = new FakeNotificationProvider();
            var channel = new TelegramChannel(provider);
            var notification = new Notification(ChannelType.Telegram, new TelegramContent("42", "Hi"));

            var result = await channel.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.Sent, result.Status);
            Assert.Same(notification, result.Notification);
            Assert.Same(notification, provider.Received);
            Assert.Equal("fake-1", result.ProviderResult.ProviderMessageId);
        }

        [Fact]
        public async Task SendAsync_ProviderFailure_MapsToFailed()
        {
            var provider = new FakeNotificationProvider
            {
                Result = ProviderDeliveryResult.Failure("Telegram rejected the request.")
            };
            var channel = new TelegramChannel(provider);
            var notification = new Notification(ChannelType.Telegram, new TelegramContent("42", "Hi"));

            var result = await channel.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
            Assert.Equal("Telegram rejected the request.", result.Details);
        }

        [Fact]
        public async Task SendAsync_WrongPayload_MapsToFailedValidation()
        {
            var channel = new TelegramChannel(new FakeNotificationProvider());
            var message = EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Test")
                .Build();
            var notification = new Notification(ChannelType.Telegram, new EmailContent(message));

            var result = await channel.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.Equal(NotificationDeliveryStatus.FailedValidation, result.Status);
            Assert.False(result.Succeeded);
            Assert.Contains("TelegramContent", result.Details);
        }

        [Fact]
        public async Task SendAsync_ProviderThrows_Propagates()
        {
            var provider = new FakeNotificationProvider
            {
                Throw = new NotificationException("rate limited", isTransient: true)
            };
            var channel = new TelegramChannel(provider);
            var notification = new Notification(ChannelType.Telegram, new TelegramContent("42", "Hi"));

            var exception = await Assert.ThrowsAsync<NotificationException>(
                () => channel.SendAsync(notification, TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Equal("rate limited", exception.Message);
        }

        [Fact]
        public async Task SendAsync_NullNotification_Throws()
        {
            var channel = new TelegramChannel(new FakeNotificationProvider());
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => channel.SendAsync(null!, TestContext.Current.CancellationToken));
        }
    }
}
