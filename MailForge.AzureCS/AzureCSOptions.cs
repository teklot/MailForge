using System;

namespace MailForge.AzureCS
{
    /// <summary>Configuration for the Azure Communication Services email provider.</summary>
    public sealed class AzureCSOptions
    {
        /// <summary>
        /// The Azure Communication Services resource endpoint
        /// (for example, "https://my-resource.communication.azure.com").
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// The access key used for HMAC-SHA256 request signing, exactly as it appears
        /// in the connection string (<c>endpoint=...;accesskey=...</c>). It is Base64-decoded before signing.
        /// </summary>
        public string? AccessKey { get; set; }

        /// <summary>
        /// The verified sender address (must match a domain validated for this resource),
        /// for example "noreply@contoso.com". Azure does not allow arbitrary From addresses.
        /// </summary>
        public string? SenderAddress { get; set; }

        /// <summary>The ACS API version to use.</summary>
        public string ApiVersion { get; set; } = "2023-03-31";

        /// <summary>The HTTP timeout in seconds (default 30).</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}