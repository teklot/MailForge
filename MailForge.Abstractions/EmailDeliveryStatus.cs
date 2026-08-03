namespace MailForge.Abstractions
{
    /// <summary>The possible outcomes of an email send.</summary>
    public enum EmailDeliveryStatus
    {
        /// <summary>The provider accepted the message for delivery.</summary>
        Sent = 0,

        /// <summary>The message did not pass validation and was not sent.</summary>
        FailedValidation = 1,

        /// <summary>The provider rejected the message or all retries failed.</summary>
        Failed = 2
    }
}
