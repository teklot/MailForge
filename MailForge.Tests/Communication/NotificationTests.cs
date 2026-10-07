using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Tests
{
    public class NotificationTests
    {
        private static EmailMessage CreateMessage() =>
            EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Hello")
                .Text("Hello")
                .Build();

        [Fact]
        public void Create_AssignsDefaults()
        {
            var notification = new Notification(ChannelType.Email, new EmailContent(CreateMessage()));

            Assert.Equal(ChannelType.Email, notification.Channel);
            Assert.NotNull(notification.Content);
            Assert.Equal(NotificationPriority.Normal, notification.Priority);
            Assert.Empty(notification.Headers);
            Assert.Empty(notification.Tags);
            Assert.False(string.IsNullOrWhiteSpace(notification.MessageId));
            Assert.Equal(notification.CreatedAt, notification.CreatedAt);
        }

        [Fact]
        public void Create_WithExplicitValues_RetainsThem()
        {
            var content = new EmailContent(CreateMessage());
            var headers = new Dictionary<string, string> { { "X-Sev", "2" } };
            var tags = new Dictionary<string, string> { { "kind", "order" } };

            var notification = new Notification(
                ChannelType.Telegram,
                content,
                NotificationPriority.Urgent,
                headers,
                tags,
                "id-1");

            Assert.Equal(ChannelType.Telegram, notification.Channel);
            Assert.Same(content, notification.Content);
            Assert.Equal(NotificationPriority.Urgent, notification.Priority);
            Assert.Equal("2", notification.Headers["x-sev"]);
            Assert.Equal("order", notification.Tags["kind"]);
            Assert.Equal("id-1", notification.MessageId);
        }

        [Fact]
        public void Create_NullChannel_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new Notification(null!, new EmailContent(CreateMessage())));

        [Fact]
        public void Create_NullContent_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new Notification(ChannelType.Email, null!));

        [Fact]
        public void ChannelType_EqualityIsCaseInsensitive()
        {
            Assert.Equal(ChannelType.Email, new ChannelType("EMAIL"));
            Assert.Equal(new ChannelType("email").GetHashCode(), new ChannelType("Email").GetHashCode());
            Assert.NotEqual(ChannelType.Email, ChannelType.Sms);
        }

        [Fact]
        public void ChannelType_ToStringReturnsName() =>
            Assert.Equal("email", ChannelType.Email.ToString());

        [Fact]
        public void NotificationDeliveryResult_Sent()
        {
            var notification = new Notification(ChannelType.Email, new EmailContent(CreateMessage()));
            var result = NotificationDeliveryResult.Sent(notification, ProviderDeliveryResult.Success("m-1"));

            Assert.True(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.Sent, result.Status);
            Assert.Same(notification, result.Notification);
            Assert.Equal("m-1", result.ProviderResult.ProviderMessageId);
        }

        [Fact]
        public void NotificationDeliveryResult_FailedValidation()
        {
            var notification = new Notification(ChannelType.Email, new EmailContent(CreateMessage()));
            var result = NotificationDeliveryResult.FailedValidation(notification, "bad email");

            Assert.False(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.FailedValidation, result.Status);
            Assert.Equal("bad email", result.Details);
        }

        [Fact]
        public void EmailContent_WrapsMessage()
        {
            var message = CreateMessage();
            var content = new EmailContent(message);

            Assert.Same(message, content.Message);
            Assert.Throws<ArgumentNullException>(() => new EmailContent(null!));
        }
    }
}