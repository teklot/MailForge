using MailForge.Communication;
using MailForge.Communication.Models;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Providers;
using MailForge.Templates;
using MailForge.Validation;

namespace MailForge.Tests
{
    public class EmailChannelTests
    {
        private static (FakeEmailProvider Provider, EmailSender Sender) CreateSender(EmailAddress? defaultFrom = null)
        {
            var provider = new FakeEmailProvider();
            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: new TemplateRegistry(),
                defaultFrom: defaultFrom);
            return (provider, sender);
        }

        private static EmailMessage CreateMessage() =>
            EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Hello")
                .Text("Hello")
                .Build();

        [Fact]
        public async Task SendAsync_ValidEmail_MapsToSent()
        {
            var (_, sender) = CreateSender();
            var channel = new EmailChannel(sender);
            var notification = new Notification(ChannelType.Email, new EmailContent(CreateMessage()));

            var result = await channel.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.Sent, result.Status);
            Assert.Same(notification, result.Notification);
            Assert.NotNull(result.ProviderResult.ProviderMessageId);
        }

        [Fact]
        public async Task SendAsync_WrongPayload_MapsToFailedValidation()
        {
            var (_, sender) = CreateSender();
            var channel = new EmailChannel(sender);
            var notification = new Notification(ChannelType.Email, new FakeContent());

            var result = await channel.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.Equal(NotificationDeliveryStatus.FailedValidation, result.Status);
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task SendAsync_DelegatesToTheConfiguredSender()
        {
            var provider = new FakeEmailProvider();
            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: new TemplateRegistry(),
                defaultFrom: new EmailAddress("default@example.com"));
            var channel = new EmailChannel(sender);

            var result = await channel.SendAsync(new Notification(ChannelType.Email, new EmailContent(CreateMessage())), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Single(provider.SentMessages);
        }

        [Fact]
        public async Task SendAsync_InvalidEmail_MapsToFailedValidation()
        {
            var (_, sender) = CreateSender();
            var channel = new EmailChannel(sender);
            var message = EmailMessage.Create().From("noreply@example.com").To("user@example.com").Build();
            var notification = new Notification(ChannelType.Email, new EmailContent(message));

            var result = await channel.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.Equal(NotificationDeliveryStatus.FailedValidation, result.Status);
            Assert.False(result.Succeeded);
        }

        private sealed class FakeContent : NotificationContent
        {
        }
    }
}