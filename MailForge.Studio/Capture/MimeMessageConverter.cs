using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MailForge.Builders;
using MailForge.Models;
using MimeKit;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// Converts between MailForge <see cref="EmailMessage"/> instances and MimeKit
    /// <see cref="MimeMessage"/> instances. Used by the SMTP relay to capture messages
    /// submitted by non-MailForge clients.
    /// </summary>
    public static class MimeMessageConverter
    {
        private static readonly string[] ReservedHeaders =
        {
            "subject", "from", "to", "cc", "bcc", "date", "message-id", "mime-version",
            "content-type", "content-transfer-encoding"
        };

        /// <summary>
        /// Builds a MailForge message from a MIME message. The envelope sender is used as a
        /// fallback when no From header is present.
        /// </summary>
        public static EmailMessage ToEmailMessage(MimeMessage mime, string? envelopeFrom = null)
        {
            if (mime == null)
                throw new ArgumentNullException(nameof(mime));

            var builder = EmailMessage.Create();

            var from = Enumerable.FirstOrDefault(mime.From.Mailboxes);
            if (from != null)
                builder.From(from.Address, from.Name);
            else if (!string.IsNullOrEmpty(envelopeFrom))
                builder.From(envelopeFrom!);
            else
                builder.From(new EmailAddress("sender@localhost"));

            var messageId = string.IsNullOrEmpty(mime.MessageId)
                ? Guid.NewGuid().ToString("N")
                : mime.MessageId;
            builder.MessageId(messageId);

            if (!string.IsNullOrEmpty(mime.Subject))
                builder.Subject(mime.Subject);

            if (!string.IsNullOrEmpty(mime.HtmlBody))
                builder.Html(TrimTrailingLineEndings(mime.HtmlBody));
            if (!string.IsNullOrEmpty(mime.TextBody))
                builder.Text(TrimTrailingLineEndings(mime.TextBody));

            builder.Priority(MapPriority(mime.Priority));

            foreach (var mailbox in mime.To.Mailboxes)
                builder.To(mailbox.Address, mailbox.Name);
            foreach (var mailbox in mime.Cc.Mailboxes)
                builder.Cc(mailbox.Address, mailbox.Name);
            foreach (var mailbox in mime.Bcc.Mailboxes)
                builder.Bcc(mailbox.Address, mailbox.Name);

            foreach (var part in EnumerateParts(mime.Body))
                builder.Attachment(new EmailAttachment(
                    part.FileName ?? "attachment",
                    ReadAllBytes(part.Content),
                    part.ContentType?.MimeType,
                    part.ContentDisposition?.Disposition == ContentDisposition.Inline,
                    part.ContentId));

            foreach (var header in mime.Headers)
            {
                var name = header.Id == HeaderId.Unknown ? header.Field : header.Id.ToString();
                if (ReservedHeaders.Contains(name.ToLowerInvariant()))
                    continue;
                builder.Header(name, header.Value);
            }

            return builder.Build();
        }

        /// <summary>Serializes a MIME message to raw .eml bytes.</summary>
        public static byte[] ToRawMime(MimeMessage mime)
        {
            if (mime == null)
                throw new ArgumentNullException(nameof(mime));

            using var stream = new MemoryStream();
            mime.WriteTo(stream);
            return stream.ToArray();
        }

        private static IEnumerable<MimePart> EnumerateParts(MimeEntity? entity)
        {
            if (entity is null)
                yield break;

            if (entity is MessagePart messagePart && messagePart.Message != null)
            {
                foreach (var part in EnumerateParts(messagePart.Message.Body))
                    yield return part;
                yield break;
            }

            if (entity is Multipart multipart)
            {
                foreach (var child in multipart)
                {
                    foreach (var part in EnumerateParts(child))
                        yield return part;
                }
                yield break;
            }

            if (entity is MimePart mimePart)
                yield return mimePart;
        }

        private static byte[] ReadAllBytes(IMimeContent? content)
        {
            if (content == null)
                return Array.Empty<byte>();

            using var stream = new MemoryStream();
            content.DecodeTo(stream);
            return stream.ToArray();
        }

        private static string TrimTrailingLineEndings(string value) =>
            value.TrimEnd('\r', '\n');

        private static EmailPriority MapPriority(MessagePriority priority)
        {
            switch (priority)
            {
                case MessagePriority.Urgent:
                    return EmailPriority.High;
                case MessagePriority.NonUrgent:
                    return EmailPriority.Low;
                default:
                    return EmailPriority.Normal;
            }
        }
    }
}