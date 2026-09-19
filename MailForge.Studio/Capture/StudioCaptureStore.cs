using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Studio.Capture.Entities;
using MailForge.Studio.Web;
using Microsoft.EntityFrameworkCore;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// Default <see cref="IStudioCaptureStore"/> implementation backed by EF Core and SQLite.
    /// Each operation uses a fresh context from an <see cref="IDbContextFactory{TContext}"/>,
    /// making the store safe to share across the studio provider and the SMTP relay server.
    /// The schema is migrated automatically on first use.
    /// </summary>
    public sealed class StudioCaptureStore : IStudioCaptureStore
    {
        private readonly IDbContextFactory<StudioDbContext> _contextFactory;
        private readonly SemaphoreSlim _schemaLock = new SemaphoreSlim(1, 1);
        private bool _schemaReady;

        /// <summary>Creates a store backed by the supplied context factory.</summary>
        public StudioCaptureStore(IDbContextFactory<StudioDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        /// <inheritdoc />
        public async Task<CapturedMessage> CaptureAsync(
            EmailMessage message,
            string? envelopeFrom = null,
            byte[]? rawMime = null,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var captured = Map(message, envelopeFrom, rawMime);

            await EnsureSchemaAsync(cancellationToken);
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            context.Messages.Add(captured);
            await context.SaveChangesAsync(cancellationToken);

            return captured;
        }

        /// <inheritdoc />
        public async Task<CapturedMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await EnsureSchemaAsync(cancellationToken);
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Messages
                .Include(m => m.Recipients)
                .Include(m => m.Attachments)
                .Include(m => m.Headers)
                .SingleOrDefaultAsync(m => m.Id == id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<CapturedMessage>> ListAsync(
            int skip = 0,
            int take = 50,
            CancellationToken cancellationToken = default)
        {
            if (skip < 0)
                throw new ArgumentOutOfRangeException(nameof(skip));
            if (take < 0)
                throw new ArgumentOutOfRangeException(nameof(take));

            await EnsureSchemaAsync(cancellationToken);
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Messages
                .Include(m => m.Recipients)
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<StudioMessagePage> QueryAsync(
            StudioMessageQuery query,
            CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            var page = Math.Max(1, query.Page ?? 1);
            var pageSize = Math.Max(1, query.PageSize ?? 25);

            await EnsureSchemaAsync(cancellationToken);
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            IQueryable<CapturedMessage> filtered = context.Messages.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim().ToLower();
                filtered = filtered.Where(m =>
                    (m.Subject != null && m.Subject.ToLower().Contains(search)) ||
                    (m.From != null && m.From.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(query.Priority) &&
                !string.Equals(query.Priority, "All", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<EmailPriority>(query.Priority, ignoreCase: true, out var priority))
            {
                filtered = filtered.Where(m => m.Priority == priority);
            }

            var totalCount = await filtered.CountAsync(cancellationToken);

            IQueryable<CapturedMessage> sorted = filtered.OrderByDescending(m => m.CreatedAt);
            if (string.Equals(query.Sort, "From", StringComparison.OrdinalIgnoreCase))
                sorted = filtered.OrderBy(m => m.From ?? "");
            else if (string.Equals(query.Sort, "Subject", StringComparison.OrdinalIgnoreCase))
                sorted = filtered.OrderBy(m => m.Subject ?? "");

            var items = await sorted
                .Include(m => m.Recipients)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new StudioMessagePage
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        /// <inheritdoc />
        public async Task<CapturedAttachment?> GetAttachmentAsync(
            Guid attachmentId,
            CancellationToken cancellationToken = default)
        {
            await EnsureSchemaAsync(cancellationToken);
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await context.Attachments
                .AsNoTracking()
                .SingleOrDefaultAsync(a => a.Id == attachmentId, cancellationToken);
        }

        private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
        {
            if (_schemaReady)
                return;

            await _schemaLock.WaitAsync(cancellationToken);
            try
            {
                if (_schemaReady)
                    return;

                await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
                await context.Database.MigrateAsync(cancellationToken);
                _schemaReady = true;
            }
            finally
            {
                _schemaLock.Release();
            }
        }

        private static CapturedMessage Map(EmailMessage message, string? envelopeFrom, byte[]? rawMime)
        {
            var captured = new CapturedMessage
            {
                MessageId = message.MessageId,
                EnvelopeFrom = envelopeFrom,
                From = message.From?.Address,
                Subject = message.Subject,
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody,
                Priority = message.Priority,
                RawMime = rawMime,
                CreatedAt = message.CreatedAt,
                Recipients = message.Recipients
                    .Select(r => new CapturedRecipient
                    {
                        Type = r.Type,
                        Address = r.Address.Address,
                        DisplayName = r.Address.DisplayName
                    })
                    .ToList(),
                Attachments = message.Attachments
                    .Select(a => new CapturedAttachment
                    {
                        FileName = a.FileName,
                        MediaType = a.MediaType,
                        IsInline = a.IsInline,
                        ContentId = a.ContentId,
                        Content = a.Content
                    })
                    .ToList(),
                Headers = message.Headers
                    .Select(h => new CapturedHeader { Name = h.Key, Value = h.Value })
                    .ToList()
            };

            return captured;
        }
    }
}