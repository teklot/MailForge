using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MailForge.Middleware
{
    /// <summary>
    /// Logs the start and outcome of every delivery attempt through
    /// <see cref="Microsoft.Extensions.Logging.ILogger&lt;T&gt;"/>.
    /// </summary>
    public sealed class LoggingEmailMiddleware : IEmailMiddleware
    {
        private readonly ILogger<LoggingEmailMiddleware> _logger;

        /// <summary>Creates the middleware.</summary>
        public LoggingEmailMiddleware(ILogger<LoggingEmailMiddleware> logger = null)
        {
            _logger = logger;
        }

        /// <summary>Logs the delivery attempt and its outcome.</summary>
        public async Task<EmailDeliveryResult> InvokeAsync(EmailDeliveryContext context, NextEmailHandler next)
        {
            _logger?.LogInformation(
                "Sending email {MessageId} to {RecipientCount} recipients.",
                context.Message.MessageId,
                context.Message.Recipients.Count);

            try
            {
                var result = await next();
                _logger?.LogInformation(
                    "Email {MessageId} delivery finished with status {Status}.",
                    context.Message.MessageId,
                    result.Status);
                return result;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, "Email {MessageId} delivery failed.", context.Message.MessageId);
                throw;
            }
        }
    }
}
