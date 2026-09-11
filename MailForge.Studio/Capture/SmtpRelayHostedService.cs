using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// Hosted-service adapter that runs the <see cref="SmtpRelayServer"/> for the application
    /// lifetime. Registered only when the SMTP relay is enabled.
    /// </summary>
    internal sealed class SmtpRelayHostedService : IHostedService
    {
        private readonly SmtpRelayServer _relay;

        /// <summary>Creates the hosted service for the supplied relay.</summary>
        public SmtpRelayHostedService(SmtpRelayServer relay)
        {
            _relay = relay;
        }

        /// <summary>Starts the relay.</summary>
        public Task StartAsync(CancellationToken cancellationToken) => _relay.StartAsync(cancellationToken);

        /// <summary>Stops the relay.</summary>
        public Task StopAsync(CancellationToken cancellationToken) => _relay.StopAsync(cancellationToken);
    }
}