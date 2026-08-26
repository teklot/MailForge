using System;

namespace MailForge.Brevo
{
    /// <summary>Configuration for the Brevo provider.</summary>
    public sealed class BrevoOptions
    {
        /// <summary>The Brevo API key.</summary>
        public string? ApiKey { get; set; }

        /// <summary>The Brevo API base address (defaults to https://api.brevo.com/).</summary>
        public Uri BaseUri { get; set; } = new Uri("https://api.brevo.com/");

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
