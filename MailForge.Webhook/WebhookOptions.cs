using System;
using System.Collections.Generic;

namespace MailForge.Webhook
{
    /// <summary>Configuration for the Webhook provider.</summary>
    public sealed class WebhookOptions
    {
        /// <summary>Initializes a new instance of the <see cref="WebhookOptions"/> class.</summary>
        public WebhookOptions() { }

        /// <summary>The absolute URL that receives the notification as an HTTP POST.</summary>
        public Uri? Endpoint { get; set; }

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Additional request headers sent with every delivery (for example, an
        /// <c>Authorization</c> token). Keys are compared case-insensitively.
        /// </summary>
        public IDictionary<string, string> Headers { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Optional HMAC-SHA256 signing secret. When set, each request body is signed and the
        /// digest is written to <see cref="SignatureHeader"/>.
        /// </summary>
        public string? SigningSecret { get; set; }

        /// <summary>The header that carries the request signature (default <c>X-MailForge-Signature</c>).</summary>
        public string SignatureHeader { get; set; } = "X-MailForge-Signature";

        /// <summary>An optional prefix prepended to the signature value (for example, <c>sha256=</c>).</summary>
        public string? SignaturePrefix { get; set; }
    }
}
