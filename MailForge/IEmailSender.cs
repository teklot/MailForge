using System.Threading;
using System.Threading.Tasks;

namespace MailForge
{
    /// <summary>
    /// The high-level entry point used by application code to send emails. The default
    /// implementation orchestrates rendering, validation, the middleware pipeline, and the
    /// configured <see cref="IEmailProvider"/>.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>Sends an email defined inline.</summary>
        /// <param name="email">The message definition.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The delivery outcome.</returns>
        Task<EmailDeliveryResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default);

        /// <summary>Sends a strongly typed email.</summary>
        /// <param name="email">The typed email definition.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>The delivery outcome.</returns>
        Task<EmailDeliveryResult> SendAsync<TModel>(Email<TModel> email, CancellationToken cancellationToken = default);
    }
}
