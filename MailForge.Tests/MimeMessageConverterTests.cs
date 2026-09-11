using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Studio.Capture;
using MimeKit;

namespace MailForge.Tests
{
    public class MimeMessageConverterTests
    {
        [Fact]
        public void ToEmailMessage_MapsAllMessageFields()
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress("Sender", "sender@example.com"));
            mime.From.Add(new MailboxAddress("Sender2", "sender2@example.com"));
            mime.To.Add(new MailboxAddress("A", "a@example.com"));
            mime.Cc.Add(new MailboxAddress("B", "b@example.com"));
            mime.Bcc.Add(new MailboxAddress("C", "c@example.com"));
            mime.Subject = "Hello";
            mime.Priority = MessagePriority.Urgent;
            mime.MessageId = "<mime-id@example.com>";
            mime.Headers.Add("X-Custom", "custom-value");

            var body = new BodyBuilder
            {
                TextBody = "Plain",
                HtmlBody = "<b>Html</b>"
            };
            body.Attachments.Add("report.pdf", new byte[] { 1, 2, 3 });
            mime.Body = body.ToMessageBody();

            var email = MimeMessageConverter.ToEmailMessage(mime, "envelope@example.com");

            Assert.Equal("sender@example.com", email.From.Address);
            Assert.Contains(email.ToRecipients, r => r.Address.Address == "a@example.com");
            Assert.Contains(email.CcRecipients, r => r.Address.Address == "b@example.com");
            Assert.Contains(email.BccRecipients, r => r.Address.Address == "c@example.com");
            Assert.Equal("Hello", email.Subject);
            Assert.Contains("Plain", email.TextBody);
            Assert.Contains("<b>Html</b>", email.HtmlBody);
            Assert.Equal(EmailPriority.High, email.Priority);
            Assert.Equal("mime-id@example.com", email.MessageId);
            Assert.Contains(email.Headers, h => h.Key == "X-Custom" && h.Value == "custom-value");
            Assert.Single(email.Attachments, a => a.FileName == "report.pdf");
        }

        [Fact]
        public void ToEmailMessage_UsesEnvelopeFromWhenNoFromHeader()
        {
            var mime = new MimeMessage();
            mime.To.Add(new MailboxAddress("A", "a@example.com"));
            mime.Subject = "No sender";
            mime.Body = new BodyBuilder { TextBody = "Body" }.ToMessageBody();

            var email = MimeMessageConverter.ToEmailMessage(mime, "envelope@example.com");

            Assert.Equal("envelope@example.com", email.From.Address);
        }

        [Fact]
        public void ToEmailMessage_AlwaysProducesFrom()
        {
            var mime = new MimeMessage();
            mime.To.Add(new MailboxAddress("A", "a@example.com"));

            var email = MimeMessageConverter.ToEmailMessage(mime);

            Assert.NotNull(email.From);
            Assert.False(string.IsNullOrEmpty(email.From.Address));
        }

        [Fact]
        public async Task ToRawMime_RoundTripsThroughMimeKit()
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress("Sender", "sender@example.com"));
            mime.To.Add(new MailboxAddress("A", "a@example.com"));
            mime.Subject = "Round trip";
            mime.Body = new BodyBuilder { TextBody = "Body" }.ToMessageBody();

            var raw = MimeMessageConverter.ToRawMime(mime);

            using var stream = new MemoryStream(raw);
            var parsed = await MimeMessage.LoadAsync(stream, TestContext.Current.CancellationToken);
            Assert.Equal("Round trip", parsed.Subject);
            Assert.Equal("sender@example.com", parsed.From.Mailboxes.Single().Address);
        }
    }
}