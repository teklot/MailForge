using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;

namespace MailForge.Interfaces
{
    /// <summary>
    /// Validates an <see cref="EmailMessage"/> before it is delivered. Validators are invoked
    /// by the framework pipeline; a failed validation prevents the message from being sent.
    /// </summary>
    public interface IEmailValidator
    {
        /// <summary>
        /// Validates a message and returns zero or more problems. An empty result means the
        /// message is valid.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        /// <returns>A list of validation problems (empty when valid).</returns>
        Task<IReadOnlyList<ValidationProblem>> ValidateAsync(EmailMessage message, CancellationToken cancellationToken = default);
    }
}
