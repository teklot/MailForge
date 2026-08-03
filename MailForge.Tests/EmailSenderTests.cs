using MailForge.Templates;
using MailForge.Validation;

namespace MailForge.Tests
{
    public class EmailSenderTests
    {
        public sealed class WelcomeModel
        {
            public string Name { get; set; } = "Jane";
        }

        private sealed class WelcomeEmail : Email<WelcomeModel>
        {
            public WelcomeEmail(WelcomeModel model) : base(model)
            {
                SetFrom("hello@example.com");
                AddTo("user@example.com");
                SetSubject("Welcome {{Name}}");
                SetHtml("<h1>Hi {{Name}}</h1>");
            }
        }

        private sealed class NoSenderEmail : Email<WelcomeModel>
        {
            public NoSenderEmail(WelcomeModel model) : base(model)
            {
                AddTo("user@example.com");
                SetSubject("No sender");
                SetText("No sender");
            }
        }

        private sealed class NamedTemplateEmail : Email<WelcomeModel>
        {
            public NamedTemplateEmail(WelcomeModel model) : base(model)
            {
                SetFrom("hello@example.com");
                AddTo("user@example.com");
                SetSubject("Named template");
                UseTemplate("welcome");
            }
        }

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

        [Fact]
        public async Task SendAsync_TypedEmail_RendersAndDelivers()
        {
            var (provider, sender) = CreateSender();

            var result = await sender.SendAsync(new WelcomeEmail(new WelcomeModel()), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Single(provider.SentMessages);

            var sent = provider.SentMessages[0];
            Assert.Equal("Welcome Jane", sent.Subject);
            Assert.Equal("<h1>Hi Jane</h1>", sent.HtmlBody);
            Assert.False(string.IsNullOrEmpty(sent.TextBody));
            Assert.Contains("Jane", sent.TextBody);
            Assert.Equal("user@example.com", sent.ToRecipients[0].Address.Address);
        }

        [Fact]
        public async Task SendAsync_TypedEmail_UsesDefaultFromWhenNotSpecified()
        {
            var (provider, sender) = CreateSender(new EmailAddress("noreply@example.com"));
            await sender.SendAsync(new NoSenderEmail(new WelcomeModel()), TestContext.Current.CancellationToken);

            Assert.Equal("noreply@example.com", provider.SentMessages[0].From.Address);
        }

        [Fact]
        public async Task SendAsync_NamedTemplate_UsesRegisteredRazorTemplate()
        {
            var provider = new FakeEmailProvider();
            var registry = new TemplateRegistry();
            registry.Register("welcome", "<p>Hello @Model.Name</p>");

            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: registry);

            var email = new NamedTemplateEmail(new WelcomeModel());
            var result = await sender.SendAsync(email, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("<p>Hello Jane</p>", provider.SentMessages[0].HtmlBody);
        }

        [Fact]
        public async Task SendAsync_InvalidMessage_FailsValidationWithoutSending()
        {
            var (provider, sender) = CreateSender();

            var message = EmailMessage.Create()
                .From("sender@example.com")
                .Subject("No recipients")
                .Text("body")
                .Build();

            var result = await sender.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(EmailDeliveryStatus.FailedValidation, result.Status);
            Assert.Empty(provider.SentMessages);
        }

        [Fact]
        public async Task SendAsync_RawMessage_WithAutoPlainTextDisabled_KeepsTextNull()
        {
            var provider = new FakeEmailProvider();
            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: new TemplateRegistry(),
                defaultFrom: null,
                autoPlainText: false);

            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("user@example.com")
                .Subject("Hi")
                .Html("<p>Body</p>")
                .Build();

            await sender.SendAsync(message, TestContext.Current.CancellationToken);

            Assert.Null(provider.SentMessages[0].TextBody);
        }
    }
}
