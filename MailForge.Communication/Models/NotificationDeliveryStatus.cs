namespace MailForge.Communication.Models
{
    /// <summary>The possible outcomes of sending a notification.</summary>
    public enum NotificationDeliveryStatus
    {
        /// <summary>The provider accepted the notification for delivery.</summary>
        Sent = 0,

        /// <summary>The notification did not pass validation and was not sent.</summary>
        FailedValidation = 1,

        /// <summary>The provider rejected the notification or all retries failed.</summary>
        Failed = 2
    }
}