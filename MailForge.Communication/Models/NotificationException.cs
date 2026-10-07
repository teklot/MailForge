using System;

namespace MailForge.Communication.Models
{
    /// <summary>
    /// The exception thrown by notification providers and by the framework when a notification
    /// cannot be sent. <see cref="IsTransient"/> distinguishes retryable failures (timeouts,
    /// rate limits, temporary service errors) from permanent ones (bad credentials, invalid
    /// destinations).
    /// </summary>
    public class NotificationException : Exception
    {
        /// <summary>True when the failure is transient and a retry may succeed.</summary>
        public bool IsTransient { get; }

        /// <summary>Creates an exception.</summary>
        public NotificationException(string message, bool isTransient = false)
            : base(message)
        {
            IsTransient = isTransient;
        }

        /// <summary>Creates an exception wrapping an inner exception.</summary>
        public NotificationException(string message, Exception innerException, bool isTransient = false)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }
    }
}