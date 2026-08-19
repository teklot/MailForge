using System;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.Middleware
{
    /// <summary>
    /// Retries the downstream pipeline when it throws a transient <see cref="EmailException"/>.
    /// Each attempt runs the whole remaining pipeline (logging, auditing, provider) so every
    /// attempt is observed. Backoff is exponential with a small random jitter.
    /// </summary>
    public sealed class RetryEmailMiddleware : IEmailMiddleware
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;
        private readonly Random _random;

        /// <summary>Creates the middleware.</summary>
        /// <param name="maxAttempts">The maximum number of delivery attempts (at least 1).</param>
        /// <param name="baseDelay">The base delay applied after the first failure.</param>
        public RetryEmailMiddleware(int maxAttempts = 3, TimeSpan? baseDelay = null)
        {
            _maxAttempts = Math.Max(1, maxAttempts);
            _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
            _random = new Random();
        }

        /// <summary>Invokes the pipeline, retrying transient failures.</summary>
        public async Task<EmailDeliveryResult> InvokeAsync(EmailDeliveryContext context, NextEmailHandler next)
        {
            for (var attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                context.Items["MailForge.Attempt"] = attempt;

                try
                {
                    return await next();
                }
                catch (EmailException exception) when (attempt < _maxAttempts)
                {
                    if (!exception.IsTransient)
                        throw;

                    await Task.Delay(ComputeDelay(attempt), CancellationToken.None);
                }
            }

            throw new EmailException("The email could not be delivered.", isTransient: true);
        }

        private TimeSpan ComputeDelay(int attempt)
        {
            var exponent = Math.Min(attempt, 6);
            var baseMilliseconds = _baseDelay.TotalMilliseconds * Math.Pow(2, exponent - 1);
            var jitter = _random.NextDouble() * baseMilliseconds * 0.2;
            return TimeSpan.FromMilliseconds(baseMilliseconds + jitter);
        }
    }
}
