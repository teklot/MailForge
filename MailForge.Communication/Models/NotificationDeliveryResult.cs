using System;
using MailForge.Models;

namespace MailForge.Communication.Models
{
    /// <summary>
    /// The outcome of sending a notification through a channel.
    /// </summary>
    public sealed class NotificationDeliveryResult
    {
        /// <summary>True when the channel's provider accepted the notification.</summary>
        public bool Succeeded => Status == NotificationDeliveryStatus.Sent;

        /// <summary>The overall delivery status.</summary>
        public NotificationDeliveryStatus Status { get; }

        /// <summary>The notification that was sent.</summary>
        public Notification Notification { get; }

        /// <summary>The result reported by the provider.</summary>
        public ProviderDeliveryResult ProviderResult { get; }

        /// <summary>The time at which the attempt finished.</summary>
        public DateTimeOffset CompletedAt { get; }

        /// <summary>A description of the outcome.</summary>
        public string? Details => ProviderResult?.Details;

        private NotificationDeliveryResult(NotificationDeliveryStatus status, Notification notification, ProviderDeliveryResult providerResult)
        {
            Status = status;
            Notification = notification;
            ProviderResult = providerResult;
            CompletedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>Creates a successful result.</summary>
        public static NotificationDeliveryResult Sent(Notification notification, ProviderDeliveryResult providerResult) =>
            new NotificationDeliveryResult(NotificationDeliveryStatus.Sent, notification, providerResult);

        /// <summary>Creates a result for a notification that did not pass validation.</summary>
        public static NotificationDeliveryResult FailedValidation(Notification notification, string details) =>
            new NotificationDeliveryResult(NotificationDeliveryStatus.FailedValidation, notification, ProviderDeliveryResult.Failure(details));

        /// <summary>Creates a result for a notification that failed all delivery attempts.</summary>
        public static NotificationDeliveryResult Failed(Notification notification, ProviderDeliveryResult providerResult) =>
            new NotificationDeliveryResult(NotificationDeliveryStatus.Failed, notification, providerResult);
    }
}