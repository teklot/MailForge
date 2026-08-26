using System;

namespace MailForge.Mailgun
{
    /// <summary>Configuration for the Mailgun provider.</summary>
    public sealed class MailgunOptions
    {
        /// <summary>The Mailgun API key.</summary>
        public string? ApiKey { get; set; }

        /// <summary>The Mailgun sending domain (for example, "mg.example.com").</summary>
        public string? Domain { get; set; }

        /// <summary>The Mailgun API base address (defaults to https://api.mailgun.net/).</summary>
        public Uri BaseUri { get; set; } = new Uri("https://api.mailgun.net/");

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
