using System.Threading;
using System.Threading.Tasks;

namespace MailForge.Abstractions
{
    /// <summary>
    /// A delivery channel that can transmit an <see cref="EmailMessage"/> to its recipients.
    /// Implementations wrap a concrete email service such as an SMTP server, an HTTP API
    /// (Resend, SendGrid), or a cloud SDK (Amazon SES).
    /// </summary>
    public interface IEmailProvider
    {
        /// <summary>The display name of the provider (for example, "SMTP" or "Resend").</summary>
        string Name { get; }

        /// <summary>Describes the capabilities supported by this provider.</summary>
        ProviderCapabilities Capabilities { get; }

        /// <summary>
        /// Transmits a message. Implementations must throw <see cref="EmailException"/> on
        /// transport or service failures so the framework pipeline can apply retries.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>A provider-specific delivery result.</returns>
        Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
