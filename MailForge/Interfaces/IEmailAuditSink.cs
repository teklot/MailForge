using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;

namespace MailForge.Interfaces
{
    /// <summary>
    /// Receives a record of every send attempt. An audit sink is the recommended place to
    /// persist delivery history for retry and compliance purposes.
    /// </summary>
    public interface IEmailAuditSink
    {
        /// <summary>Records the outcome of a send attempt.</summary>
        /// <param name="record">The audit record.</param>
        /// <param name="cancellationToken">A token used to cancel the operation.</param>
        Task RecordAsync(EmailAuditRecord record, CancellationToken cancellationToken = default);
    }
}
