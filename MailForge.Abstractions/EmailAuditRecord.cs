using System;
using System.Collections.Generic;

namespace MailForge.Abstractions
{
    /// <summary>A record of a single send attempt produced by the framework.</summary>
    public sealed class EmailAuditRecord
    {
        /// <summary>The unique identifier of the message.</summary>
        public string MessageId { get; }

        /// <summary>The provider that handled the attempt.</summary>
        public string ProviderName { get; }

        /// <summary>The overall delivery status.</summary>
        public EmailDeliveryStatus Status { get; }

        /// <summary>The provider-specific message identifier, when available.</summary>
        public string ProviderMessageId { get; }

        /// <summary>A description of the outcome.</summary>
        public string Details { get; }

        /// <summary>The number of delivery attempts made.</summary>
        public int Attempt { get; }

        /// <summary>The time at which the attempt completed.</summary>
        public DateTimeOffset CompletedAt { get; }

        /// <summary>The message that was sent.</summary>
        public EmailMessage Message { get; }

        /// <summary>Custom fields written by audit middleware.</summary>
        public IDictionary<string, object> Data { get; }

        /// <summary>Creates an audit record.</summary>
        public EmailAuditRecord(string messageId, string providerName, EmailDeliveryStatus status, string providerMessageId, string details, int attempt, DateTimeOffset completedAt, EmailMessage message)
        {
            MessageId = messageId;
            ProviderName = providerName;
            Status = status;
            ProviderMessageId = providerMessageId;
            Details = details;
            Attempt = attempt;
            CompletedAt = completedAt;
            Message = message;
            Data = new Dictionary<string, object>();
        }
    }
}
