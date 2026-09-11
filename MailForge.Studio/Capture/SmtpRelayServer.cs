using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MimeKit;
using Microsoft.Extensions.Hosting;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// A minimal local SMTP server that captures every message it receives and persists it
    /// to the MailForge Studio database. Any SMTP client — MailKit, .NET SmtpClient, telnet —
    /// can point at this relay with no code changes, making it a zero-integration capture path
    /// for applications that do not use the MailForge pipeline.
    /// </summary>
    public sealed class SmtpRelayServer : IHostedService, IAsyncDisposable
    {
        private readonly IStudioCaptureStore _store;
        private readonly StudioCaptureOptions _options;
        private TcpListener? _listener;
        private CancellationTokenSource? _shutdown;
        private Task? _acceptLoop;

        /// <summary>Creates a relay that captures messages to the supplied store.</summary>
        public SmtpRelayServer(IStudioCaptureStore store, StudioCaptureOptions options)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>The actual port the relay is listening on (useful when port 0 was configured).</summary>
        public int Port { get; private set; }

        /// <summary>True while the relay is accepting connections.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Starts the relay listener.</summary>
        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_listener != null)
                throw new InvalidOperationException("The SMTP relay is already running.");

            var address = IPAddress.TryParse(_options.SmtpRelayHost, out var parsed)
                ? parsed
                : ResolveHost(_options.SmtpRelayHost);

            _shutdown = new CancellationTokenSource();
            _listener = new TcpListener(address, _options.SmtpRelayPort);
            _listener.Start();

            var localEndpoint = (IPEndPoint)_listener.LocalEndpoint;
            Port = localEndpoint.Port;
            IsRunning = true;

            // Listen on the local loopback address too when bound to the wildcard address.
            _acceptLoop = Task.Run(() => AcceptLoopAsync(_shutdown.Token), CancellationToken.None);
            return Task.CompletedTask;
        }

        /// <summary>Stops the relay listener and waits for in-flight connections.</summary>
        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            _shutdown?.Cancel();
            _listener?.Stop();
            _listener = null;
            IsRunning = false;

            if (_acceptLoop != null)
            {
                try
                {
                    await _acceptLoop.WaitAsync(TimeSpan.FromSeconds(5));
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception)
                {
                }
                _acceptLoop = null;
            }

            _shutdown?.Dispose();
            _shutdown = null;
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            var listener = _listener;
            if (listener == null)
                return;

            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient? client = null;
                try
                {
                    client = await listener.AcceptTcpClientAsync(cancellationToken);
                    _ = Task.Run(() => HandleClientAsync(client, cancellationToken), CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    client?.Dispose();
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true))
            using (var writer = new StreamWriter(stream, new ASCIIEncoding(), 1024, leaveOpen: true))
            {
                writer.AutoFlush = true;
                await WriteAsync(writer, "220 MailForge Studio Relay ESMTP", cancellationToken);

                string? envelopeFrom = null;
                var envelopeTo = new List<string>();
                var dataLines = new List<string>();

                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line == null)
                        break;

                    var command = ParseCommand(line);

                    switch (command.Verb)
                    {
                        case "EHLO":
                            await WriteAsync(writer, $"250-MailForge Studio Relay", cancellationToken);
                            await WriteAsync(writer, "250 HELP", cancellationToken);
                            break;
                        case "HELO":
                            await WriteAsync(writer, "250 MailForge Studio Relay", cancellationToken);
                            break;
                        case "MAIL":
                            envelopeFrom = command.Argument;
                            envelopeTo.Clear();
                            await WriteAsync(writer, "250 Ok", cancellationToken);
                            break;
                        case "RCPT":
                            envelopeTo.Add(command.Argument);
                            await WriteAsync(writer, "250 Ok", cancellationToken);
                            break;
                        case "DATA":
                            if (envelopeFrom == null || envelopeTo.Count == 0)
                            {
                                await WriteAsync(writer, "503 Bad sequence of commands", cancellationToken);
                                break;
                            }
                            await WriteAsync(writer, "354 End data with <CR><LF>.<CR><LF>", cancellationToken);
                            dataLines.Clear();
                            while (true)
                            {
                                var dataLine = await reader.ReadLineAsync(cancellationToken);
                                if (dataLine == null)
                                    break;
                                if (dataLine == ".")
                                    break;
                                dataLines.Add(Unstuff(dataLine));
                            }
                            await CaptureAsync(writer, envelopeFrom, dataLines, cancellationToken);
                            envelopeFrom = null;
                            envelopeTo.Clear();
                            break;
                        case "RSET":
                            envelopeFrom = null;
                            envelopeTo.Clear();
                            dataLines.Clear();
                            await WriteAsync(writer, "250 Ok", cancellationToken);
                            break;
                        case "NOOP":
                            await WriteAsync(writer, "250 Ok", cancellationToken);
                            break;
                        case "QUIT":
                            await WriteAsync(writer, "221 Bye", cancellationToken);
                            return;
                        default:
                            await WriteAsync(writer, "500 Unrecognized command", cancellationToken);
                            break;
                    }
                }
            }
        }

        private async Task CaptureAsync(
            StreamWriter writer,
            string envelopeFrom,
            List<string> dataLines,
            CancellationToken cancellationToken)
        {
            try
            {
                var rawMime = BuildRawMime(dataLines);

                // Parse the raw message with MimeKit, then feed it through the studio store.
                using var stream = new MemoryStream(rawMime);
                var mime = await MimeMessage.LoadAsync(stream, cancellationToken);
                var message = MimeMessageConverter.ToEmailMessage(mime, envelopeFrom);
                await _store.CaptureAsync(message, envelopeFrom, rawMime, cancellationToken);

                await WriteAsync(writer, "250 Ok: queued as " + message.MessageId, cancellationToken);
            }
            catch (Exception)
            {
                await WriteAsync(writer, "451 Unable to process message", cancellationToken);
            }
        }

        private static byte[] BuildRawMime(List<string> dataLines) =>
            Encoding.UTF8.GetBytes(string.Join("\r\n", dataLines) + "\r\n");

        private static string Unstuff(string line) =>
            line.StartsWith(".", StringComparison.Ordinal) ? line.Substring(1) : line;

        private static (string Verb, string Argument) ParseCommand(string line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                return ("", "");

            var spaceIndex = trimmed.IndexOfAny(new[] { ' ', ':' });
            var verb = spaceIndex < 0 ? trimmed : trimmed.Substring(0, spaceIndex);
            var argument = spaceIndex < 0 ? "" : trimmed.Substring(spaceIndex + 1).Trim();

            // Normalize mail/rcpt argument to the address inside angle brackets.
            if (string.Equals(verb, "MAIL", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(verb, "RCPT", StringComparison.OrdinalIgnoreCase))
            {
                var start = argument.IndexOf('<');
                var end = argument.LastIndexOf('>');
                if (start >= 0 && end > start)
                    argument = argument.Substring(start + 1, end - start - 1);
            }

            return (verb.ToUpperInvariant(), argument);
        }

        private static IPAddress ResolveHost(string host)
        {
            var addresses = Dns.GetHostAddresses(host);
            var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return address ?? IPAddress.Loopback;
        }

        private static async Task WriteAsync(StreamWriter writer, string text, CancellationToken cancellationToken)
        {
            await writer.WriteAsync(text + "\r\n");
            await writer.FlushAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            _shutdown?.Dispose();
        }
    }
}