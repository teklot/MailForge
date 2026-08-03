using System;

namespace MailForge.AmazonSES
{
    /// <summary>Configuration for the Amazon SES provider.</summary>
    public sealed class AmazonSesOptions
    {
        /// <summary>
        /// The AWS region name (for example, "us-east-1"). Required unless the SDK is configured
        /// with a default region through the environment or a profile.
        /// </summary>
        public string Region { get; set; }

        /// <summary>The AWS access key (optional; the SDK default credential chain is used when absent).</summary>
        public string AccessKey { get; set; }

        /// <summary>The AWS secret key (optional; the SDK default credential chain is used when absent).</summary>
        public string SecretKey { get; set; }

        /// <summary>The configuration set used for event tracking (optional).</summary>
        public string ConfigurationSetName { get; set; }

        /// <summary>The maximum number of times to retry transient service errors internally (default 3).</summary>
        public int MaxErrorRetry { get; set; } = 3;
    }
}
