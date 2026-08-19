using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailForge.Models
{
    /// <summary>
    /// The outcome of sending an email through the framework pipeline.
    /// </summary>
    public sealed class EmailDeliveryResult
    {
        /// <summary>True when the configured provider accepted the message.</summary>
        public bool Succeeded => Status == EmailDeliveryStatus.Sent;

        /// <summary>The overall delivery status.</summary>
        public EmailDeliveryStatus Status { get; }

        /// <summary>The message that was sent.</summary>
        public EmailMessage Message { get; }

        /// <summary>The result reported by the provider.</summary>
        public ProviderDeliveryResult ProviderResult { get; }

        /// <summary>The time at which the attempt finished.</summary>
        public DateTimeOffset CompletedAt { get; }

        /// <summary>A description of the outcome.</summary>
        public string? Details => ProviderResult?.Details;

        private EmailDeliveryResult(EmailDeliveryStatus status, EmailMessage message, ProviderDeliveryResult providerResult)
        {
            Status = status;
            Message = message;
            ProviderResult = providerResult;
            CompletedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>Creates a successful result.</summary>
        public static EmailDeliveryResult Sent(EmailMessage message, ProviderDeliveryResult providerResult) =>
            new EmailDeliveryResult(EmailDeliveryStatus.Sent, message, providerResult);

        /// <summary>Creates a result for a message that did not pass validation.</summary>
        public static EmailDeliveryResult FailedValidation(EmailMessage message, string details) =>
            new EmailDeliveryResult(EmailDeliveryStatus.FailedValidation, message, ProviderDeliveryResult.Failure(details));

        /// <summary>Creates a result for a message that failed all delivery attempts.</summary>
        public static EmailDeliveryResult Failed(EmailMessage message, ProviderDeliveryResult providerResult) =>
            new EmailDeliveryResult(EmailDeliveryStatus.Failed, message, providerResult);
    }
}
