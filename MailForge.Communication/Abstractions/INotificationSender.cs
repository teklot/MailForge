using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Models;

namespace MailForge.Communication.Abstractions
{
    /// <summary>
    /// The high-level entry point used by application code to send notifications. The default
    /// implementation resolves the channel for the notification and runs the configured
    /// middleware pipeline around it.
    /// </summary>
    public interface INotificationSender
    {
        /// <summary>Sends a notification through its configured channel.</summary>
        /// <param name="notification">The notification to send.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The delivery outcome.</returns>
        Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default);
    }
}