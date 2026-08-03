using System;
using System.Threading.Tasks;
using MailForge.Abstractions;

namespace MailForge.Core.Middleware
{
    /// <summary>
    /// Writes an <see cref="EmailAuditRecord"/> for every delivery attempt to the configured
    /// <see cref="IEmailAuditSink"/>. Failures (including exceptions thrown downstream) are
    /// recorded and re-thrown so later stages can still act on them.
    /// </summary>
    public sealed class AuditEmailMiddleware : IEmailMiddleware
    {
        private readonly IEmailAuditSink _auditSink;
        private readonly string _providerName;

        /// <summary>Creates the middleware.</summary>
        public AuditEmailMiddleware(IEmailAuditSink auditSink, string providerName = null)
        {
            _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
            _providerName = providerName;
        }

        /// <summary>Records the delivery attempt.</summary>
        public async Task<EmailDeliveryResult> InvokeAsync(EmailDeliveryContext context, NextEmailHandler next)
        {
            try
            {
                var result = await next();
                await RecordAsync(context, result.Status, result.ProviderResult);
                return result;
            }
            catch (EmailException exception)
            {
                await RecordAsync(context, EmailDeliveryStatus.Failed, ProviderDeliveryResult.Failure(exception.Message));
                throw;
            }
            catch (Exception exception)
            {
                await RecordAsync(context, EmailDeliveryStatus.Failed, ProviderDeliveryResult.Failure(exception.Message));
                throw;
            }
        }

        private async Task RecordAsync(EmailDeliveryContext context, EmailDeliveryStatus status, ProviderDeliveryResult providerResult)
        {
            var record = new EmailAuditRecord(
                context.Message.MessageId,
                _providerName ?? "unknown",
                status,
                providerResult?.ProviderMessageId,
                providerResult?.Details,
                attempt: context.Items.TryGetValue("MailForge.Attempt", out var attempt) ? (int)attempt : 1,
                DateTimeOffset.UtcNow,
                context.Message);

            foreach (var item in context.Items)
            {
                if (item.Key.StartsWith("MailForge.", StringComparison.Ordinal))
                    continue;
                record.Data[item.Key] = item.Value;
            }

            try
            {
                await _auditSink.RecordAsync(record);
            }
            catch
            {
                // Auditing must never break delivery.
            }
        }
    }
}
