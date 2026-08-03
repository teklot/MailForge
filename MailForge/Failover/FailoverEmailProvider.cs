using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailForge
{
    /// <summary>
    /// An <see cref="IEmailProvider"/> that tries a precedence-ordered chain of providers until
    /// one succeeds. Each provider is governed by a <see cref="ProviderFailoverRoute"/> that
    /// defines its failover <see cref="FailoverPolicy"/> and attempt budget. When every route is
    /// exhausted an aggregate <see cref="EmailException"/> is thrown whose
    /// <see cref="EmailException.IsTransient"/> reflects whether any failure was retryable.
    /// </summary>
    public sealed class FailoverEmailProvider : IEmailProvider
    {
        private readonly ProviderFailoverRoute[] _routes;

        /// <summary>Creates a failover provider from an ordered list of routes.</summary>
        public FailoverEmailProvider(IEnumerable<ProviderFailoverRoute> routes)
        {
            if (routes == null)
                throw new ArgumentNullException(nameof(routes));

            _routes = routes.ToArray();
            if (_routes.Length == 0)
                throw new ArgumentException("At least one provider route is required.", nameof(routes));
        }

        /// <summary>Creates a failover provider from routes listed in precedence order.</summary>
        public FailoverEmailProvider(params ProviderFailoverRoute[] routes)
            : this((IEnumerable<ProviderFailoverRoute>)routes)
        {
        }

        /// <summary>Creates a failover provider with one default route per provider, in precedence order.</summary>
        public FailoverEmailProvider(params IEmailProvider[] providers)
            : this(providers?.Select(provider => new ProviderFailoverRoute(provider)))
        {
        }

        /// <summary>The name of the provider chain.</summary>
        public string Name => "failover";

        /// <summary>
        /// The capabilities supported by every provider in the chain. A message that any single
        /// provider cannot carry (for example, an attachment) is not safe to route.
        /// </summary>
        public ProviderCapabilities Capabilities
        {
            get
            {
                var combined = ProviderCapabilities.All;
                foreach (var route in _routes)
                {
                    var capability = route.Provider.Capabilities;
                    combined = new ProviderCapabilities(
                        combined.SupportsAttachments && capability.SupportsAttachments,
                        combined.SupportsInlineImages && capability.SupportsInlineImages,
                        combined.SupportsHeaders && capability.SupportsHeaders,
                        combined.SupportsTags && capability.SupportsTags,
                        combined.RequiresBothBodies || capability.RequiresBothBodies);
                }
                return combined;
            }
        }

        /// <summary>
        /// Sends <paramref name="message"/> through the first route and fails over to the next
        /// while failures match each route's policy, stopping at the first success.
        /// </summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var failures = new List<string>();
            var anyTransient = false;

            foreach (var route in _routes)
            {
                for (var attempt = 0; attempt < route.MaxAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ProviderDeliveryResult result;
                    try
                    {
                        result = await route.Provider.SendAsync(message, cancellationToken);
                    }
                    catch (EmailException exception)
                    {
                        if (!exception.IsTransient)
                        {
                            // A permanent failure is surfaced unless the route opts in to failover.
                            if (!route.Policy.FailOverOnPermanentFailure)
                                throw;

                            failures.Add($"{route.Provider.Name}: {exception.Message}");
                            break;
                        }

                        anyTransient = true;
                        failures.Add($"{route.Provider.Name}: {exception.Message}");
                        continue;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        // An unexpected exception is treated as transient so the chain can recover.
                        anyTransient = true;
                        failures.Add($"{route.Provider.Name}: {exception.Message}");
                        continue;
                    }

                    if (result.Succeeded)
                    {
                        var details = string.IsNullOrEmpty(result.Details)
                            ? $"Delivered by provider '{route.Provider.Name}'."
                            : $"Delivered by provider '{route.Provider.Name}'. {result.Details}";
                        return new ProviderDeliveryResult(true, result.ProviderMessageId, details);
                    }

                    // The provider accepted the message but reported a permanent rejection.
                    if (!route.Policy.FailOverOnPermanentFailure)
                        return result;

                    failures.Add($"{route.Provider.Name}: {result.Details}");
                    break;
                }
            }

            throw new EmailException(
                "All failover providers failed to deliver the message: " + string.Join(" | ", failures),
                isTransient: anyTransient);
        }
    }
}
