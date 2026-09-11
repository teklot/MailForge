using System;

namespace MailForge.Studio.Capture.Entities
{
    /// <summary>A captured message header.</summary>
    public sealed class CapturedHeader
    {
        /// <summary>The surrogate primary key.</summary>
        public Guid Id { get; set; }

        /// <summary>The owning captured message.</summary>
        public Guid MessageId { get; set; }

        /// <summary>The header name.</summary>
        public string? Name { get; set; }

        /// <summary>The header value.</summary>
        public string? Value { get; set; }
    }
}