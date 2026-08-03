namespace MailForge
{
    /// <summary>
    /// Controls when a <see cref="FailoverEmailProvider"/> stops trying the current provider
    /// and fails over to the next route in the chain.
    /// </summary>
    public sealed class FailoverPolicy
    {
        /// <summary>
        /// Fails over only on transient <see cref="EmailException"/> failures (timeouts, rate
        /// limits, temporary service errors). Permanent failures and rejected messages are
        /// surfaced to the caller as-is so they are not retried through other providers.
        /// </summary>
        public static FailoverPolicy TransientOnly { get; } = new FailoverPolicy(failOverOnPermanentFailure: false);

        /// <summary>
        /// Fails over on any failure, including permanent exceptions and provider rejections.
        /// Prefer <see cref="TransientOnly"/> unless you have a strong reason to retry
        /// permanent failures through another provider.
        /// </summary>
        public static FailoverPolicy AnyFailure { get; } = new FailoverPolicy(failOverOnPermanentFailure: true);

        private FailoverPolicy(bool failOverOnPermanentFailure)
        {
            FailOverOnPermanentFailure = failOverOnPermanentFailure;
        }

        /// <summary>True when a permanent failure or rejection should trigger failover.</summary>
        public bool FailOverOnPermanentFailure { get; }
    }
}
