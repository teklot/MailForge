using System.Collections.Generic;

namespace MailForge
{
    /// <summary>
    /// Immutable state shared by every <see cref="IEmailMiddleware"/> in the pipeline for a
    /// single send operation. The message can be mutated between stages.
    /// </summary>
    public sealed class EmailDeliveryContext
    {
        /// <summary>The message being sent.</summary>
        public EmailMessage Message { get; }

        /// <summary>Metadata written by middleware for other stages to consume.</summary>
        public IDictionary<string, object> Items { get; }

        /// <summary>Creates a delivery context.</summary>
        public EmailDeliveryContext(EmailMessage message)
        {
            Message = message;
            Items = new Dictionary<string, object>();
        }
    }
}
