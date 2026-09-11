using System;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// An <see cref="IEmailProvider"/> that persists every message to the MailForge Studio
    /// local database instead of delivering it. Ideal for local development: point the
    /// MailForge pipeline at this provider to inspect everything that would have been sent.
    /// </summary>
    public sealed class StudioEmailProvider : IEmailProvider
    {
        private readonly IStudioCaptureStore _store;

        /// <summary>Creates a provider backed by the supplied capture store.</summary>
        public StudioEmailProvider(IStudioCaptureStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Studio Capture";

        /// <summary>The capabilities descriptor — the studio captures all message features.</summary>
        public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.All;

        /// <summary>Persists the message to the studio database and reports success.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var captured = await _store.CaptureAsync(message, cancellationToken: cancellationToken);
            return ProviderDeliveryResult.Success(
                providerMessageId: message.MessageId,
                details: $"Captured to Studio (id={captured.Id}).");
        }
    }
}