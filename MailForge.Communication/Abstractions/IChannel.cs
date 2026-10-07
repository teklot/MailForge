using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Communication.Abstractions
{
    /// <summary>
    /// A delivery channel that can transmit a <see cref="Notification"/> for its
    /// <see cref="ChannelType"/>. A channel owns the channel-specific pipeline and hands the
    /// notification to one or more providers. Implementations must throw
    /// <see cref="NotificationException"/> on transport or service failures and return a
    /// <see cref="NotificationDeliveryResult"/> for every handled notification.
    /// </summary>
    public interface IChannel
    {
        /// <summary>The channel this implementation delivers.</summary>
        ChannelType Channel { get; }

        /// <summary>
        /// Transmits a notification. Implementations should validate the payload shape and
        /// map provider outcomes (including validation failures) to a delivery result.
        /// </summary>
        /// <param name="notification">The notification to send.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The delivery outcome.</returns>
        Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default);
    }
}