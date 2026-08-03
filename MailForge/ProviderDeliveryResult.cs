using System;

namespace MailForge
{
    /// <summary>
    /// The outcome of a single delivery attempt reported by an <see cref="IEmailProvider"/>.
    /// Providers report transient service failures by throwing <see cref="EmailException"/>
    /// with <see cref="EmailException.IsTransient"/> set to true.
    /// </summary>
    public sealed class ProviderDeliveryResult
    {
        /// <summary>True when the provider accepted the message for delivery.</summary>
        public bool Succeeded { get; }

        /// <summary>A provider-specific message identifier (for example, a Resend or SES id).</summary>
        public string ProviderMessageId { get; }

        /// <summary>A human-readable description of the outcome.</summary>
        public string Details { get; }

        /// <summary>Creates a result.</summary>
        public ProviderDeliveryResult(bool succeeded, string providerMessageId = null, string details = null)
        {
            Succeeded = succeeded;
            ProviderMessageId = providerMessageId;
            Details = details;
        }

        /// <summary>A successful result without a provider message id.</summary>
        public static ProviderDeliveryResult Success(string providerMessageId = null, string details = null) =>
            new ProviderDeliveryResult(true, providerMessageId, details);

        /// <summary>A failed result.</summary>
        public static ProviderDeliveryResult Failure(string details) =>
            new ProviderDeliveryResult(false, null, details);
    }
}
