using System;

namespace MailForge.Models
{
    /// <summary>
    /// The exception thrown by providers and by the framework when an email cannot be sent.
    /// <see cref="IsTransient"/> distinguishes retryable failures (timeouts, rate limits,
    /// temporary service errors) from permanent ones (bad credentials, invalid addresses).
    /// </summary>
    public class EmailException : Exception
    {
        /// <summary>True when the failure is transient and a retry may succeed.</summary>
        public bool IsTransient { get; }

        /// <summary>Creates an exception.</summary>
        public EmailException(string message, bool isTransient = false)
            : base(message)
        {
            IsTransient = isTransient;
        }

        /// <summary>Creates an exception wrapping an inner exception.</summary>
        public EmailException(string message, Exception innerException, bool isTransient = false)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }
    }
}
