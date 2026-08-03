using System;

namespace MailForge.Resend
{
    /// <summary>Configuration for the Resend provider.</summary>
    public sealed class ResendOptions
    {
        /// <summary>The Resend API key.</summary>
        public string ApiKey { get; set; }

        /// <summary>The Resend API base address (defaults to https://api.resend.com/).</summary>
        public Uri BaseUri { get; set; } = new Uri("https://api.resend.com/");

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
