using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Models;

namespace MailForge.Communication.Abstractions
{
    /// <summary>
    /// Determines the target channels and optional transformed notifications for delivery.
    /// When registered, the <see cref="INotificationSender"/> uses the router to resolve
    /// targets from routing rules instead of sending directly to <see cref="Notification.Channel"/>.
    /// </summary>
    public interface INotificationRouter
    {
        /// <summary>
        /// Resolves delivery targets for the given notification. Implementations must return
        /// at least one target when routing is applied; a target's <see cref="NotificationTarget.Notification"/>
        /// may be a transformed clone used for that route.
        /// </summary>
        /// <param name="notification">The notification to route.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The targets to deliver through.</returns>
        Task<IReadOnlyList<NotificationTarget>> RouteAsync(Notification notification, CancellationToken cancellationToken = default);
    }
}
