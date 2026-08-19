using MailForge.Models;

namespace MailForge.Tests
{
    public class EmailMessageBuilderTests
    {
        [Fact]
        public void Build_PopulatesAllFields()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com", "Sender")
                .To("a@example.com")
                .Cc("b@example.com")
                .Bcc("c@example.com")
                .Subject("Hello")
                .Html("<p>Hi</p>")
                .Text("Hi")
                .Priority(EmailPriority.High)
                .Header("X-Test", "yes")
                .Tag("purpose", "demo")
                .Attachment(new EmailAttachment("doc.txt", new byte[] { 1, 2, 3 }))
                .Build();

            Assert.Equal("sender@example.com", message.From.Address);
            Assert.Equal("Sender", message.From.DisplayName);
            Assert.Equal("Hello", message.Subject);
            Assert.Equal("<p>Hi</p>", message.HtmlBody);
            Assert.Equal("Hi", message.TextBody);
            Assert.Equal(EmailPriority.High, message.Priority);
            Assert.Single(message.ToRecipients);
            Assert.Single(message.CcRecipients);
            Assert.Single(message.BccRecipients);
            Assert.Equal("yes", message.Headers["X-Test"]);
            Assert.Equal("demo", message.Tags["purpose"]);
            Assert.Single(message.Attachments);
            Assert.False(string.IsNullOrEmpty(message.MessageId));
            Assert.True(message.CreatedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public void Headers_AreCaseInsensitive()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("s")
                .Text("t")
                .Header("X-Key", "value")
                .Build();

            Assert.Equal("value", message.Headers["x-key"]);
        }

        [Fact]
        public void EmailAddress_ToString_IncludesDisplayName()
        {
            var address = new EmailAddress("a@example.com", "Alice");
            Assert.Equal("Alice <a@example.com>", address.ToString());
            Assert.Equal("a@example.com", new EmailAddress("a@example.com").ToString());
        }

        [Fact]
        public void EmailAddress_Equality_IsCaseInsensitiveOnAddress()
        {
            Assert.True(new EmailAddress("A@Example.com").Equals(new EmailAddress("a@example.com")));
        }

        [Fact]
        public void EmailAttachment_GuessesMediaTypeFromExtension()
        {
            var attachment = new EmailAttachment("invoice.pdf", new byte[] { 1 });
            Assert.Equal("application/pdf", attachment.MediaType);

            var unknown = new EmailAttachment("file.xyz", new byte[] { 1 });
            Assert.Equal("application/octet-stream", unknown.MediaType);
        }
    }
}
