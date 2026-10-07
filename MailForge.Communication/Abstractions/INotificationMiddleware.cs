using System.Threading.Tasks;
using MailForge.Communication.Models;

namespace MailForge.Communication.Abstractions
{
    /// <summary>
    /// A unit of the notification pipeline that wraps delivery of a
    /// <see cref="Notification"/>. Middleware can inspect the notification, measure and log
    /// the attempt, apply retries, and short-circuit delivery by not invoking the next handler.
    /// </summary>
    public interface INotificationMiddleware
    {
        /// <summary>Invoked for every notification passing through the pipeline.</summary>
        /// <param name="context">The current delivery context.</param>
        /// <param name="next">The next middleware (or the channel) in the pipeline.</param>
        /// <returns>The delivery outcome.</returns>
        Task<NotificationDeliveryResult> InvokeAsync(NotificationDeliveryContext context, NextNotificationHandler next);
    }

    /// <summary>Delegates to the next step of the notification pipeline.</summary>
    public delegate Task<NotificationDeliveryResult> NextNotificationHandler();
}