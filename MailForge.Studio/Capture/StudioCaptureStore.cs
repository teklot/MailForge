using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Studio.Capture.Entities;
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