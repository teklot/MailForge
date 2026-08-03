using System;

namespace MailForge
{
    /// <summary>
    /// Configures one provider in a <see cref="FailoverEmailProvider"/> precedence chain: how
    /// many attempts the provider gets and under which <see cref="FailoverPolicy"/> it fails over.
    /// </summary>
    public sealed class ProviderFailoverRoute
    {
        /// <summary>
        /// Creates a route using the default <see cref="FailoverPolicy.TransientOnly"/> policy
        /// with a single attempt.
        /// </summary>
        public ProviderFailoverRoute(IEmailProvider provider)
            : this(provider, FailoverPolicy.TransientOnly, maxAttempts: 1)
        {
        }

        /// <summary>
        /// Creates a route that may attempt the same provider up to
        /// <paramref name="maxAttempts"/> times before failing over.
        /// </summary>
        public ProviderFailoverRoute(IEmailProvider provider, FailoverPolicy policy, int maxAttempts = 1)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
            MaxAttempts = Math.Max(1, maxAttempts);
        }

        /// <summary>The provider that delivers messages for this route.</summary>
        public IEmailProvider Provider { get; }

        /// <summary>Controls which failures trigger failover.</summary>
        public FailoverPolicy Policy { get; }

        /// <summary>Attempts allowed on this provider before failing over (minimum 1).</summary>
        public int MaxAttempts { get; }
    }
}
