using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MailForge.Abstractions;

namespace MailForge.Smtp
{
    /// <summary>
    /// Delivers email through an SMTP server using the MailKit <see cref="SmtpClient"/>.
    /// A fresh connection is opened for every message.
    /// </summary>
    public sealed class SmtpEmailProvider : IEmailProvider
    {
        private readonly SmtpOptions _options;
        private readonly ISmtpClient _client;

        /// <summary>Creates the provider from SMTP options.</summary>
        public SmtpEmailProvider(SmtpOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.Host))
                throw new ArgumentException("An SMTP host is required.", nameof(options));
        }

        /// <summary>
        /// Creates the provider using a pre-configured client (primarily for testing).
        /// </summary>
        internal SmtpEmailProvider(SmtpOptions options, ISmtpClient client)
            : this(options)
        {
            _client = client;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "SMTP";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: true,
            supportsHeaders: true,
            supportsTags: true);

        /// <summary>Transmits a message through the SMTP server.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var mimeMessage = SmtpMessageMapper.ToMimeMessage(message);

            var client = _client ?? new SmtpClient();
            try
            {
                if (client is SmtpClient smtp)
                {
                    if (_options.TimeoutMilliseconds > 0)
                        smtp.Timeout = _options.TimeoutMilliseconds;
                    if (!string.IsNullOrEmpty(_options.LocalDomain))
                        smtp.LocalDomain = _options.LocalDomain;
                }

                await client.ConnectAsync(_options.Host, _options.Port, _options.SecureSocketOptions, cancellationToken);

                if (!string.IsNullOrEmpty(_options.Username))
                    await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);

                var messageId = await client.SendAsync(mimeMessage, cancellationToken);

                await client.DisconnectAsync(true, cancellationToken);

                return ProviderDeliveryResult.Success(messageId, $"Delivered via {_options.Host}:{_options.Port}.");
            }
            catch (EmailException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new EmailException(
                    $"SMTP delivery failed: {exception.Message}",
                    exception,
                    IsTransient(exception));
            }
            finally
            {
                if (_client == null)
                    client.Dispose();
            }
        }

        private static bool IsTransient(Exception exception)
        {
            if (exception is SmtpCommandException command)
            {
                var code = (int)command.StatusCode;
                return code >= 400 && code < 500;
            }

            if (exception is SocketException ||
                exception is ServiceNotConnectedException ||
                exception is ServiceNotAuthenticatedException ||
                exception is SmtpProtocolException ||
                exception is TimeoutException)
            {
                return true;
            }

            return false;
        }
    }
}
