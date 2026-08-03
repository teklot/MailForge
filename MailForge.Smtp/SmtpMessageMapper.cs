using System;
using System.IO;
using System.Linq;
using MimeKit;

namespace MailForge.Smtp
{
    /// <summary>
    /// Converts a MailForge <see cref="EmailMessage"/> into a MimeKit <see cref="MimeMessage"/>.
    /// </summary>
    public static class SmtpMessageMapper
    {
        private static readonly string[] ReservedHeaders =
        {
            "subject", "from", "to", "cc", "bcc", "date", "message-id", "mime-version", "content-type", "content-transfer-encoding"
        };

        /// <summary>Builds a MIME message from a MailForge message.</summary>
        public static MimeMessage ToMimeMessage(EmailMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var mime = new MimeMessage();

            if (message.From != null)
                mime.From.Add(new MailboxAddress(message.From.DisplayName, message.From.Address));

            foreach (var recipient in message.ToRecipients)
                mime.To.Add(new MailboxAddress(recipient.Address.DisplayName, recipient.Address.Address));
            foreach (var recipient in message.CcRecipients)
                mime.Cc.Add(new MailboxAddress(recipient.Address.DisplayName, recipient.Address.Address));
            foreach (var recipient in message.BccRecipients)
                mime.Bcc.Add(new MailboxAddress(recipient.Address.DisplayName, recipient.Address.Address));

            mime.Subject = message.Subject;
            mime.Priority = MapPriority(message.Priority);

            var body = new BodyBuilder();

            if (!string.IsNullOrEmpty(message.HtmlBody))
                body.HtmlBody = message.HtmlBody;
            if (!string.IsNullOrEmpty(message.TextBody))
                body.TextBody = message.TextBody;

            foreach (var attachment in message.Attachments)
            {
                if (attachment.IsInline)
                {
                    var inline = new MimePart(ContentType.Parse(attachment.MediaType))
                    {
                        Content = new MimeContent(new MemoryStream(attachment.Content)),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Inline),
                        ContentId = string.IsNullOrEmpty(attachment.ContentId)
                            ? Guid.NewGuid().ToString("N")
                            : attachment.ContentId,
                        FileName = attachment.FileName
                    };
                    body.LinkedResources.Add(inline);
                }
                else
                {
                    var part = new MimePart(ContentType.Parse(attachment.MediaType))
                    {
                        Content = new MimeContent(new MemoryStream(attachment.Content)),
                        ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                        FileName = attachment.FileName
                    };
                    body.Attachments.Add(part);
                }
            }

            mime.Body = body.ToMessageBody();

            foreach (var header in message.Headers)
            {
                var name = header.Key.Trim();
                if (ReservedHeaders.Contains(name.ToLowerInvariant()))
                    continue;
                mime.Headers.Add(name, header.Value);
            }

            foreach (var tag in message.Tags)
                mime.Headers.Add($"X-MailForge-Tag-{tag.Key}", tag.Value);

            return mime;
        }

        private static MessagePriority MapPriority(EmailPriority priority)
        {
            switch (priority)
            {
                case EmailPriority.Low:
                    return MessagePriority.NonUrgent;
                case EmailPriority.High:
                    return MessagePriority.Urgent;
                default:
                    return MessagePriority.Normal;
            }
        }
    }
}
