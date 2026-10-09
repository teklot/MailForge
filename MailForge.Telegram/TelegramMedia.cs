using System;

namespace MailForge.Telegram
{
    /// <summary>The kind of media carried by a <see cref="TelegramMedia"/>.</summary>
    public enum TelegramMediaKind
    {
        /// <summary>A photo referenced by URL.</summary>
        Photo = 0,

        /// <summary>A document referenced by URL or by inline bytes.</summary>
        Document = 1
    }

    /// <summary>
    /// An optional photo or document attached to a Telegram message. When present, the
    /// <see cref="TelegramContent.Text"/> is sent as the media caption.
    /// </summary>
    public sealed class TelegramMedia
    {
        /// <summary>The media kind.</summary>
        public TelegramMediaKind Kind { get; }

        /// <summary>The remote URL of the photo or document (null for inline bytes).</summary>
        public string? Url { get; }

        /// <summary>The inline document content (null for URL-based media).</summary>
        public byte[]? Content { get; }

        /// <summary>The file name for an inline document.</summary>
        public string? FileName { get; }

        private TelegramMedia(TelegramMediaKind kind, string? url, byte[]? content, string? fileName)
        {
            Kind = kind;
            Url = url;
            Content = content;
            FileName = fileName;
        }

        /// <summary>Creates a photo payload from a remote URL.</summary>
        /// <param name="url">The photo URL.</param>
        public static TelegramMedia FromPhoto(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("A photo URL is required.", nameof(url));
            return new TelegramMedia(TelegramMediaKind.Photo, url, null, null);
        }

        /// <summary>Creates a document payload from a remote URL.</summary>
        /// <param name="url">The document URL.</param>
        public static TelegramMedia FromDocument(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("A document URL is required.", nameof(url));
            return new TelegramMedia(TelegramMediaKind.Document, url, null, null);
        }

        /// <summary>Creates a document payload from inline bytes.</summary>
        /// <param name="content">The document content.</param>
        /// <param name="fileName">The file name presented to Telegram.</param>
        public static TelegramMedia FromDocument(byte[] content, string fileName)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (content.Length == 0)
                throw new ArgumentException("Document content cannot be empty.", nameof(content));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("A file name is required.", nameof(fileName));
            return new TelegramMedia(TelegramMediaKind.Document, null, content, fileName);
        }
    }
}
