using System;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Studio.Capture.Entities;
using MailForge.Studio.Web;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// Persists and queries captured messages in the MailForge Studio local database.
    /// Implementations are thread-safe and can be shared by the studio provider and the
    /// SMTP relay server.
    /// </summary>
    public interface IStudioCaptureStore
    {
        /// <summary>Captures a MailForge message and returns the stored entity.</summary>
        Task<CapturedMessage> CaptureAsync(
            EmailMessage message,
            string? envelopeFrom = null,
            byte[]? rawMime = null,
            CancellationToken cancellationToken = default);

        /// <summary>Returns a captured message by id, including its children, or null.</summary>
        Task<CapturedMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Returns captured messages newest-first with paging.</summary>
        Task<IReadOnlyList<CapturedMessage>> ListAsync(
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries captured messages applying search, priority filter, sorting, and paging,
        /// including their recipients.
        /// </summary>
        Task<StudioMessagePage> QueryAsync(StudioMessageQuery query, CancellationToken cancellationToken = default);

        /// <summary>Returns a captured attachment by id, or null.</summary>
        Task<CapturedAttachment?> GetAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
    }
}