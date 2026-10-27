using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Webhook
{
    /// <summary>
    /// Delivers notifications as HTTP POST requests to an arbitrary endpoint using HttpClient.
    /// The <see cref="WebhookContent.Payload"/> is posted verbatim with the configured headers;
    /// when <see cref="WebhookOptions.SigningSecret"/> is set, the body is signed with
    /// HMAC-SHA256 and the lowercase hex digest is written to the signature header. Transient
    /// failures (429, 5xx, transport errors) are thrown as <see cref="NotificationException"/>
    /// with <c>IsTransient</c> set; endpoint rejections are returned as failed results.
    /// </summary>
    public sealed class WebhookNotificationProvider : INotificationProvider
    {
        private readonly WebhookOptions _options;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from Webhook options.</summary>
        public WebhookNotificationProvider(WebhookOptions options)
            : this(options, CreateHttpClient(options ?? throw new ArgumentNullException(nameof(options))))
        {
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        internal WebhookNotificationProvider(WebhookOptions options, HttpClient httpClient)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (_options.Endpoint == null)
                throw new ArgumentException("A webhook endpoint is required.", nameof(options));
            if (!_options.Endpoint.IsAbsoluteUri)
                throw new ArgumentException("The webhook endpoint must be an absolute URI.", nameof(options));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Webhook";

        /// <summary>The channel this provider serves.</summary>
        public ChannelType Channel => ChannelType.Webhook;

        /// <summary>Posts a notification to the configured endpoint.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (!(notification.Content is WebhookContent content))
                return ProviderDeliveryResult.Failure(
                    $"The Webhook provider requires a {nameof(WebhookContent)} payload but received '{notification.Content.GetType().Name}'.");

            var body = Encoding.UTF8.GetBytes(content.Payload);
            var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
            {
                Content = new ByteArrayContent(body)
            };
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(content.ContentType);

            foreach (var header in _options.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

            var signature = TryBuildSignature(body);
            if (signature != null)
                request.Headers.TryAddWithoutValidation(_options.SignatureHeader, signature);

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new NotificationException(
                    $"Webhook request failed: {exception.Message}",
                    exception,
                    isTransient: true);
            }

            using (request)
            using (response)
            {
                var status = response.StatusCode;

                if (response.IsSuccessStatusCode)
                    return ProviderDeliveryResult.Success(null, $"Accepted by webhook endpoint (HTTP {(int)status}).");

                var transient = status == (HttpStatusCode)429 || (int)status >= 500;
                var description = $"The webhook endpoint rejected the request (HTTP {(int)status}).";

                if (transient)
                    throw new NotificationException(description, isTransient: true);

                return ProviderDeliveryResult.Failure(description);
            }
        }

        private string? TryBuildSignature(byte[] body)
        {
            if (string.IsNullOrEmpty(_options.SigningSecret))
                return null;

#pragma warning disable CA5350 // HMAC-SHA256 is the intended, non-deprecated algorithm.
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SigningSecret!)))
            {
                var hash = hmac.ComputeHash(body);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    builder.Append(b.ToString("x2"));
                return (_options.SignaturePrefix ?? string.Empty) + builder.ToString();
            }
#pragma warning restore CA5350
        }

        private static HttpClient CreateHttpClient(WebhookOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
