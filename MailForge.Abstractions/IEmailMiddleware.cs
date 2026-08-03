using System.Threading.Tasks;

namespace MailForge.Abstractions
{
    /// <summary>
    /// A unit of the framework pipeline that wraps the delivery of an <see cref="EmailMessage"/>.
    /// Middleware can inspect and mutate the message, measure and log the attempt, apply retries,
    /// and short-circuit delivery by not invoking the next handler.
    /// </summary>
    public interface IEmailMiddleware
    {
        /// <summary>
        /// Invoked for every message passing through the pipeline.
        /// </summary>
        /// <param name="context">The current delivery context.</param>
        /// <param name="next">The next middleware (or the provider) in the pipeline.</param>
        /// <returns>The delivery outcome.</returns>
        Task<EmailDeliveryResult> InvokeAsync(EmailDeliveryContext context, NextEmailHandler next);    }

    /// <summary>Delegates to the next step of the pipeline.</summary>
    public delegate Task<EmailDeliveryResult> NextEmailHandler();
}
