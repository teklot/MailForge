using MailForge.Abstractions;
using MailForge.AmazonSES;

namespace MailForge.Tests
{
    public class SesMessageMapperTests
    {
        [Fact]
        public void ToMimeMessage_MapsRecipientsSubjectAndBodies()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Cc("b@example.com")
                .Subject("Test")
                .Html("<p>Hi</p>")
                .Text("Hi")
                .Build();

            var mime = SesMessageMapper.ToMimeMessage(message);

            Assert.Equal("sender@example.com", mime.From.ToString());
            Assert.Equal("a@example.com", mime.To.ToString());
            Assert.Equal("b@example.com", mime.Cc.ToString());
            Assert.Equal("Test", mime.Subject);
            Assert.Contains("<p>Hi</p>", mime.HtmlBody);
            Assert.Contains("Hi", mime.TextBody);
        }

        [Fact]
        public void ToRawMessage_ProducesNonEmptyMimeBytes()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("Test")
                .Html("<p>Hi</p>")
                .Attachment(new EmailAttachment("file.pdf", new byte[] { 1, 2, 3 }))
                .Build();

            var raw = SesMessageMapper.ToRawMessage(message);

            Assert.NotEmpty(raw);
            var text = System.Text.Encoding.UTF8.GetString(raw);
            Assert.Contains("Subject: Test", text);
            Assert.Contains("file.pdf", text);
        }
    }
}
