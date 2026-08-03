using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

namespace MailForge.AmazonSES
{
    /// <summary>
    /// Delivers email through the Amazon SES V2 API as a raw MIME message.
    /// </summary>
    public sealed class AmazonSesEmailProvider : IEmailProvider
    {
        private readonly AmazonSesOptions _options;
        private readonly IAmazonSimpleEmailServiceV2 _client;

        /// <summary>Creates the provider from Amazon SES options.</summary>
        public AmazonSesEmailProvider(AmazonSesOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _client = CreateClient(_options);
        }

        /// <summary>Creates the provider using a pre-configured SDK client (primarily for testing).</summary>
        internal AmazonSesEmailProvider(AmazonSesOptions options, IAmazonSimpleEmailServiceV2 client)
            : this(options)
        {
            _client = client;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "AmazonSES";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: true,
            supportsHeaders: true,
            supportsTags: true);

        /// <summary>Transmits a message through the Amazon SES API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var raw = SesMessageMapper.ToRawMessage(message);

            var request = new SendEmailRequest
            {
                FromEmailAddress = message.From?.Address,
                Content = new EmailContent
                {
                    Raw = new RawMessage { Data = new MemoryStream(raw) }
                },
                ConfigurationSetName = _options.ConfigurationSetName
            };

            try
            {
                var response = await _client.SendEmailAsync(request, cancellationToken);
                return ProviderDeliveryResult.Success(response.MessageId, "Accepted by Amazon SES.");
            }
            catch (AmazonSimpleEmailServiceV2Exception exception)
            {
                var isTransient = (int)exception.StatusCode >= 500 || exception.StatusCode == (System.Net.HttpStatusCode)429;
                throw new EmailException(
                    $"Amazon SES rejected the message: {exception.Message}",
                    exception,
                    isTransient);
            }
            catch (Exception exception)
            {
                throw new EmailException(
                    $"Amazon SES delivery failed: {exception.Message}",
                    exception,
                    isTransient: true);
            }
        }

        private static IAmazonSimpleEmailServiceV2 CreateClient(AmazonSesOptions options)
        {
            RegionEndpoint region = null;
            if (!string.IsNullOrWhiteSpace(options.Region))
                region = RegionEndpoint.GetBySystemName(options.Region);

            if (region == null)
                throw new ArgumentException("An AWS region is required (for example, \"us-east-1\").", nameof(options));

            var config = new AmazonSimpleEmailServiceV2Config
            {
                RegionEndpoint = region,
                MaxErrorRetry = Math.Max(0, options.MaxErrorRetry)
            };

            if (!string.IsNullOrWhiteSpace(options.AccessKey) && !string.IsNullOrWhiteSpace(options.SecretKey))
                return new AmazonSimpleEmailServiceV2Client(options.AccessKey, options.SecretKey, config);

            return new AmazonSimpleEmailServiceV2Client(config);
        }
    }
}
