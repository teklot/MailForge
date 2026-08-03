using System.Collections.Generic;
using MailForge.Smtp;

namespace MailForge.Tests
{
    public class SmtpMessageMapperTests
    {
        [Fact]
        public void ToMimeMessage_MapsRecipientsSubjectBodiesAndPriority()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com", "Sender")
                .To("a@example.com", "A")
                .Cc("b@example.com")
                .Bcc("c@example.com")
                .Subject("Test")
                .Html("<p>Hi</p>")
                .Text("Hi")
                .Priority(EmailPriority.High)
                .Build();

            var mime = SmtpMessageMapper.ToMimeMessage(message);

            Assert.Equal("sender@example.com", mime.From.Mailboxes.Single().Address);
            Assert.Equal("Sender", mime.From.Mailboxes.Single().Name);
            Assert.Equal("a@example.com", mime.To.Mailboxes.Single().Address);
            Assert.Equal("b@example.com", mime.Cc.Mailboxes.Single().Address);
            Assert.Equal("c@example.com", mime.Bcc.Mailboxes.Single().Address);
            Assert.Equal("Test", mime.Subject);
            Assert.Contains("<p>Hi</p>", mime.HtmlBody);
            Assert.Contains("Hi", mime.TextBody);
            Assert.Equal(MimeKit.MessagePriority.Urgent, mime.Priority);
        }

        [Fact]
        public void ToMimeMessage_AddsAttachmentsAndInlineImages()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("Test")
                .Html("<img src=\"cid:logo\">")
                .Text("plain")
                .Attachment(new EmailAttachment("report.pdf", new byte[] { 1, 2, 3 }))
                .Attachment(new EmailAttachment("logo.png", new byte[] { 4, 5, 6 }, "image/png", isInline: true, contentId: "logo"))
                .Build();

            var mime = SmtpMessageMapper.ToMimeMessage(message);
            var parts = Descendants(mime.Body!).OfType<MimeKit.MimePart>().ToArray();

            var attachments = parts.Where(p => p.ContentDisposition?.Disposition == MimeKit.ContentDisposition.Attachment).ToArray();
            Assert.Single(attachments);
            Assert.Equal("report.pdf", attachments[0].FileName);

            var inline = parts.Where(p => p.ContentDisposition?.Disposition == MimeKit.ContentDisposition.Inline).ToArray();
            Assert.Single(inline);
            Assert.Equal("logo", inline[0].ContentId);
        }

        private static IEnumerable<MimeKit.MimeEntity> Descendants(MimeKit.MimeEntity entity)
        {
            if (entity is MimeKit.Multipart multipart)
            {
                foreach (var child in multipart)
                {
                    foreach (var descendant in Descendants(child))
                        yield return descendant;
                }
            }
            else
            {
                yield return entity;
            }
        }

        [Fact]
        public void ToMimeMessage_MapsTagsToHeaders()
        {
            var message = EmailMessage.Create()
                .From("sender@example.com")
                .To("a@example.com")
                .Subject("Test")
                .Text("Hi")
                .Tag("purpose", "welcome")
                .Build();

            var mime = SmtpMessageMapper.ToMimeMessage(message);

            Assert.Equal("welcome", mime.Headers["X-MailForge-Tag-purpose"]);
        }
    }
}
