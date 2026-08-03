using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Abstractions;

namespace MailForge.Core
{
    /// <summary>
    /// A provider that never delivers anything. It records every message sent through it in
    /// memory, which makes it useful for development, demos, and tests.
    /// </summary>
    public sealed class FakeEmailProvider : IEmailProvider
    {
        private readonly List<EmailMessage> _sent = new List<EmailMessage>();

        /// <summary>The provider display name.</summary>
        public string Name => "Fake";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = ProviderCapabilities.All;

        /// <summary>The messages that have been sent through this provider.</summary>
        public IReadOnlyList<EmailMessage> SentMessages => _sent;

        /// <summary>Records the message and returns a successful result.</summary>
        public Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            lock (_sent)
            {
                _sent.Add(message);
            }
            return Task.FromResult(ProviderDeliveryResult.Success(providerMessageId: $"fake-{message.MessageId}"));
        }

        /// <summary>Clears the recorded messages.</summary>
        public void Clear() => _sent.Clear();
    }
}
