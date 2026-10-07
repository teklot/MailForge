using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Communication.Abstractions
{
    /// <summary>
    /// A provider that transmits a <see cref="Notification"/> through its channel's wire
    /// format. Channels compose providers (including failover chains) and translate
    /// <see cref="ProviderDeliveryResult"/> into a <see cref="NotificationDeliveryResult"/>.
    /// </summary>
    public interface INotificationProvider
    {
        /// <summary>The display name of the provider (for example, "Telegram" or "Webhook").</summary>
        string Name { get; }

        /// <summary>The channel this provider serves.</summary>
        ChannelType Channel { get; }

        /// <summary>
        /// Transmits a notification. Implementations must throw <see cref="NotificationException"/>
        /// on transient transport or service failures so channels can apply retries.
        /// </summary>
        /// <param name="notification">The notification to send.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>A provider-specific delivery result.</returns>
        Task<ProviderDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default);
    }
}