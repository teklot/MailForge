namespace MailForge.Models
{
    /// <summary>Describes the capabilities a provider offers.</summary>
    public sealed class ProviderCapabilities
    {
        /// <summary>True when the provider supports sending attachments.</summary>
        public bool SupportsAttachments { get; }

        /// <summary>True when the provider supports inline images (cid: references).</summary>
        public bool SupportsInlineImages { get; }

        /// <summary>True when the provider supports custom headers.</summary>
        public bool SupportsHeaders { get; }

        /// <summary>True when the provider supports arbitrary tags for analytics.</summary>
        public bool SupportsTags { get; }

        /// <summary>True when the provider is limited to HTML-only or text-only messages.</summary>
        public bool RequiresBothBodies { get; }

        /// <summary>Creates a capabilities descriptor.</summary>
        public ProviderCapabilities(
            bool supportsAttachments = true,
            bool supportsInlineImages = true,
            bool supportsHeaders = true,
            bool supportsTags = true,
            bool requiresBothBodies = false)
        {
            SupportsAttachments = supportsAttachments;
            SupportsInlineImages = supportsInlineImages;
            SupportsHeaders = supportsHeaders;
            SupportsTags = supportsTags;
            RequiresBothBodies = requiresBothBodies;
        }

        /// <summary>A capabilities descriptor representing every option.</summary>
        public static ProviderCapabilities All { get; } = new ProviderCapabilities();

        /// <summary>A capabilities descriptor representing a provider that only sends text/HTML bodies.</summary>
        public static ProviderCapabilities BodyOnly { get; } = new ProviderCapabilities(
            supportsAttachments: false,
            supportsInlineImages: false,
            supportsHeaders: false,
            supportsTags: false);
    }
}
