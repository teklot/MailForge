using MailForge.Builders;
using MailForge.Extensions;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Providers;
using MailForge.Templates;
using MailForge.Validation;

namespace MailForge.Tests
{
    public class TypedEmailTests
    {
        public sealed class FullModel
        {
            public string Name { get; set; } = "Alice";
            public string Code { get; set; } = "ABC-123";
        }

        private sealed class FullEmail : Email<FullModel>
        {
            public FullEmail(FullModel model) : base(model)
            {
                SetFrom("sender@example.com");
                AddTo("to@example.com", "Primary");
                AddCc("cc@example.com", "CC User");
                AddBcc("bcc@example.com", "BCC User");
                SetSubject("Hello {{Name}}");
                SetHtml("<h1>Hello {{Name}}</h1><p>Code: {{Code}}</p>");
                SetText("Hello {{Name}}, code: {{Code}}");
                SetPriority(EmailPriority.High);
                Attach(new EmailAttachment("data.txt", new byte[] { 1, 2, 3 }));
                AddHeader("X-Custom", "custom-value");
                AddTag("purpose", "notification");
            }
        }

        private sealed class TagOnlyEmail : Email<FullModel>
        {
            public TagOnlyEmail(FullModel model) : base(model)
            {
                SetFrom("sender@example.com");
                AddTo("to@example.com");
                SetSubject("Tags only");
                SetText("body");
                AddTag("env", "test");
                AddTag("region", "us-east-1");
            }
        }

        private sealed class HeaderOnlyEmail : Email<FullModel>
        {
            public HeaderOnlyEmail(FullModel model) : base(model)
            {
                SetFrom("sender@example.com");
                AddTo("to@example.com");
                SetSubject("Headers only");
                SetText("body");
                AddHeader("X-Request-Id", "req-42");
                AddHeader("X-Session", "sess-99");
            }
        }

        private static (FakeEmailProvider Provider, EmailSender Sender) CreateSender()
        {
            var provider = new FakeEmailProvider();
            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: new TemplateRegistry(),
                defaultFrom: null);
            return (provider, sender);
        }

        [Fact]
        public async Task SendAsync_FullEmail_MapsAllFields()
        {
            var (provider, sender) = CreateSender();

            var result = await sender.SendAsync(new FullEmail(new FullModel()), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            var sent = provider.SentMessages[0];
            Assert.Equal("Hello Alice", sent.Subject);
            Assert.Contains("Alice", sent.HtmlBody);
            Assert.Contains("ABC-123", sent.TextBody);
            Assert.Equal(EmailPriority.High, sent.Priority);
            Assert.Single(sent.Attachments);
            Assert.Equal("data.txt", sent.Attachments[0].FileName);
            Assert.Equal("custom-value", sent.Headers["X-Custom"]);
            Assert.Equal("notification", sent.Tags["purpose"]);
        }

        [Fact]
        public async Task SendAsync_FullEmail_RecipientsIncludeCcAndBcc()
        {
            var (provider, sender) = CreateSender();

            await sender.SendAsync(new FullEmail(new FullModel()), TestContext.Current.CancellationToken);

            var sent = provider.SentMessages[0];
            Assert.Single(sent.ToRecipients);
            Assert.Equal("to@example.com", sent.ToRecipients[0].Address.Address);
            Assert.Single(sent.CcRecipients);
            Assert.Equal("cc@example.com", sent.CcRecipients[0].Address.Address);
            Assert.Single(sent.BccRecipients);
            Assert.Equal("bcc@example.com", sent.BccRecipients[0].Address.Address);
        }

        [Fact]
        public async Task SendAsync_MultipleTags_AllMapped()
        {
            var (provider, sender) = CreateSender();

            await sender.SendAsync(new TagOnlyEmail(new FullModel()), TestContext.Current.CancellationToken);

            var sent = provider.SentMessages[0];
            Assert.Equal(2, sent.Tags.Count);
            Assert.Equal("test", sent.Tags["env"]);
            Assert.Equal("us-east-1", sent.Tags["region"]);
        }

        [Fact]
        public async Task SendAsync_MultipleHeaders_AllMapped()
        {
            var (provider, sender) = CreateSender();

            await sender.SendAsync(new HeaderOnlyEmail(new FullModel()), TestContext.Current.CancellationToken);

            var sent = provider.SentMessages[0];
            Assert.Equal(2, sent.Headers.Count);
            Assert.Equal("req-42", sent.Headers["X-Request-Id"]);
            Assert.Equal("sess-99", sent.Headers["X-Session"]);
        }

        [Fact]
        public async Task SendAsync_TypedEmail_ModelPropertiesRendered()
        {
            var (provider, sender) = CreateSender();

            await sender.SendAsync(new FullEmail(new FullModel { Name = "Bob", Code = "XYZ" }), TestContext.Current.CancellationToken);

            var sent = provider.SentMessages[0];
            Assert.Equal("Hello Bob", sent.Subject);
            Assert.Contains("Bob", sent.HtmlBody);
            Assert.Contains("XYZ", sent.TextBody);
        }
    }
}
