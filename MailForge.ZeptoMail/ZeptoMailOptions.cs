using System;

namespace MailForge.ZeptoMail
{
    /// <summary>Configuration for the ZeptoMail (Zoho) provider.</summary>
    public sealed class ZeptoMailOptions
    {
        /// <summary>The Zoho ZeptoMail API key.</summary>
        public string? SendApiKey { get; set; }

        /// <summary>The ZeptoMail API base address (defaults to https://zeptomail.zoho.com/).</summary>
        public Uri BaseUri { get; set; } = new Uri("https://zeptomail.zoho.com/");

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
