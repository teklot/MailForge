using System;
using System.Collections.Generic;
using System.IO;

namespace MailForge
{
    /// <summary>An attachment (or inline image) attached to an email message.</summary>
    public sealed class EmailAttachment
    {
        private static readonly Dictionary<string, string> MediaTypes = new Dictionary<string, string>
        {
            { ".txt", "text/plain" },
            { ".csv", "text/csv" },
            { ".html", "text/html" },
            { ".htm", "text/html" },
            { ".json", "application/json" },
            { ".xml", "application/xml" },
            { ".pdf", "application/pdf" },
            { ".zip", "application/zip" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".svg", "image/svg+xml" },
            { ".ics", "text/calendar" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" }
        };

        /// <summary>The file name of the attachment.</summary>
        public string FileName { get; }

        /// <summary>The MIME media type (for example, "application/pdf").</summary>
        public string MediaType { get; }

        /// <summary>The attachment content bytes.</summary>
        public byte[] Content { get; }

        /// <summary>True when the attachment is an inline image referenced via a content id (cid:).</summary>
        public bool IsInline { get; }

        /// <summary>The content id used to reference inline images (cid:value) from the HTML body.</summary>
        public string ContentId { get; }

        /// <summary>Creates an attachment from a byte array.</summary>
        public EmailAttachment(string fileName, byte[] content, string mediaType = null, bool isInline = false, string contentId = null)
        {
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Content = content ?? throw new ArgumentNullException(nameof(content));
            MediaType = mediaType ?? GuessMediaType(fileName);
            IsInline = isInline;
            ContentId = contentId;
        }

        /// <summary>Creates an attachment by reading all bytes from a stream.</summary>
        public EmailAttachment(string fileName, Stream content, string mediaType = null, bool isInline = false, string contentId = null)
            : this(fileName, ReadAllBytes(content), mediaType, isInline, contentId)
        {
        }

        /// <summary>Creates an attachment from a file on disk.</summary>
        public static EmailAttachment FromFile(string path, string mediaType = null, bool isInline = false, string contentId = null)
            => new EmailAttachment(Path.GetFileName(path), File.ReadAllBytes(path), mediaType, isInline, contentId);

        /// <summary>Infers a MIME media type from a file name extension.</summary>
        public static string GuessMediaType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return MediaTypes.TryGetValue(extension, out var mediaType)
                ? mediaType
                : "application/octet-stream";
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }
}
