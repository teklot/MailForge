using System;

namespace MailForge.Studio.Capture.Entities
{
    /// <summary>A captured attachment or inline image.</summary>
    public sealed class CapturedAttachment
    {
        /// <summary>The surrogate primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>The owning captured message.</summary>
        public Guid MessageId { get; set; }

        /// <summary>The file name of the attachment.</summary>
        public string? FileName { get; set; }

        /// <summary>The MIME media type.</summary>
        public string? MediaType { get; set; }

        /// <summary>True when the attachment is an inline image referenced via a content id (cid:).</summary>
        public bool IsInline { get; set; }

        /// <summary>The content id used to reference inline images.</summary>
        public string? ContentId { get; set; }

        /// <summary>The attachment content bytes.</summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();
    }
}