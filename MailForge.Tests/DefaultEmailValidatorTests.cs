using MailForge.Models;
using MailForge.Validation;

namespace MailForge.Tests
{
    public class DefaultEmailValidatorTests
    {
        private readonly DefaultEmailValidator _validator = new();

        [Fact]
        public async Task ValidMessage_ReturnsNoProblems()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("recipient@example.com")
                .Subject("Hello")
                .Text("Body")
                .Build();

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Empty(problems);
        }

        [Fact]
        public async Task MissingSender_ReturnsMissingSender()
        {
            var message = new EmailMessage(
                from: null!,
                recipients: new[] { new EmailRecipient(new EmailAddress("a@example.com"), EmailRecipientType.To) },
                subject: "Hi",
                textBody: "body");

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(problems, p => p.Code == "MissingSender");
        }

        [Fact]
        public async Task NullSender_ReturnsMissingSender()
        {
            var message = new EmailMessage(
                from: null!,
                recipients: new[] { new EmailRecipient(new EmailAddress("a@example.com"), EmailRecipientType.To) },
                subject: "Hi",
                textBody: "body");

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(problems, p => p.Code == "MissingSender");
        }

        [Fact]
        public async Task InvalidSender_ReturnsInvalidSender()
        {
            var message = EmailMessage.Create()
                .From("not-an-email")
                .To("a@example.com")
                .Subject("Hi")
                .Text("body")
                .Build();

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(problems, p => p.Code == "InvalidSender");
        }

        [Fact]
        public async Task MissingRecipients_ReturnsMissingRecipients()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .Subject("Hi")
                .Text("body")
                .Build();

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(problems, p => p.Code == "MissingRecipients");
        }

        [Fact]
        public async Task InvalidRecipient_ReturnsInvalidRecipient()
        {
            var message = new EmailMessage(
                from: new EmailAddress("sender@example.com"),
                recipients: new[] { new EmailRecipient(new EmailAddress("bad"), EmailRecipientType.To) },
                subject: "Hi",
                textBody: "body");

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(problems, p => p.Code == "InvalidRecipient");
        }

        [Fact]
        public async Task MissingSubject_ReturnsMissingSubject()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Text("body")
                .Build();

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(problems, p => p.Code == "MissingSubject");
        }

        [Fact]
        public async Task MissingBody_ReturnsMissingBody()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("Hi")
                .Build();

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Contains(problems, p => p.Code == "MissingBody");
        }

        [Fact]
        public async Task BothBodiesPresent_ReturnsNoProblems()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("Hi")
                .Html("<p>Hi</p>")
                .Text("Hi")
                .Build();

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.Empty(problems);
        }

        [Fact]
        public async Task OnlyHtmlBody_ReturnsNoBodyProblem()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("Hi")
                .Html("<p>Hi</p>")
                .Build();

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            Assert.DoesNotContain(problems, p => p.Code == "MissingBody");
        }

        [Fact]
        public async Task MultipleProblems_ReturnsAll()
        {
            var message = new EmailMessage(
                from: null!,
                recipients: Array.Empty<EmailRecipient>(),
                subject: null,
                htmlBody: null,
                textBody: null);

            var problems = await _validator.ValidateAsync(message, TestContext.Current.CancellationToken);

            var codes = problems.Select(p => p.Code).ToArray();
            Assert.Contains("MissingSender", codes);
            Assert.Contains("MissingRecipients", codes);
            Assert.Contains("MissingSubject", codes);
            Assert.Contains("MissingBody", codes);
        }
    }
}
