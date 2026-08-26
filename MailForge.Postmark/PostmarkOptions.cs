using System;

namespace MailForge.Postmark
{
    /// <summary>Configuration for the Postmark provider.</summary>
    public sealed class PostmarkOptions
    {
        /// <summary>The Postmark server token.</summary>
        public string? ServerToken { get; set; }

        /// <summary>The Postmark API base address (defaults to https://api.postmarkapp.com/).</summary>
        public Uri BaseUri { get; set; } = new Uri("https://api.postmarkapp.com/");

        /// <summary>The message stream to use (defaults to "outbound").</summary>
        public string? MessageStream { get; set; } = "outbound";

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
