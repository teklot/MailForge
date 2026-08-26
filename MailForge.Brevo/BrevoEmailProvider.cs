using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.Brevo
{
    /// <summary>
    /// Delivers email through the Brevo API (https://brevo.com) using HttpClient.
    /// </summary>
    public sealed class BrevoEmailProvider : IEmailProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly BrevoOptions _options;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from Brevo options.</summary>
        public BrevoEmailProvider(BrevoOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new ArgumentException("A Brevo API key is required.", nameof(options));
            _httpClient = CreateHttpClient(_options);
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        public BrevoEmailProvider(BrevoOptions options, HttpClient httpClient)
            : this(options)
        {
            _httpClient = httpClient;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Brevo";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: true,
            supportsHeaders: true,
            supportsTags: true);

        /// <summary>Transmits a message through the Brevo API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var request = BuildRequest(message);
            var json = JsonSerializer.Serialize(request, JsonOptions);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, "v3/smtp/email"))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            })
            {
                httpRequest.Headers.Add("api-key", _options.ApiKey!);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new EmailException(
                        $"Brevo request failed: {exception.Message}",
                        exception,
                        isTransient: true);
                }

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var id = TryParseMessageId(body);
                        return ProviderDeliveryResult.Success(id, $"Accepted by Brevo (HTTP {(int)response.StatusCode}).");
                    }

                    var isTransient = response.StatusCode == (HttpStatusCode)429 ||
                                      (int)response.StatusCode >= 500;
                    if (response.StatusCode == (HttpStatusCode)401)
                        isTransient = false;
                    var messageText = TryParseError(body) ?? $"Brevo rejected the request (HTTP {(int)response.StatusCode}).";
                    throw new EmailException(messageText, isTransient);
                }
            }
        }

        private BrevoSendRequest BuildRequest(EmailMessage message)
        {
            var request = new BrevoSendRequest
            {
                Sender = message.From != null
                    ? new BrevoSenderDto { Name = message.From.DisplayName, Email = message.From.Address }
                    : null,
                To = ToArray(message.ToRecipients),
                Cc = ToArray(message.CcRecipients),
                Bcc = ToArray(message.BccRecipients),
                Subject = message.Subject,
                HtmlContent = message.HtmlBody,
                TextContent = message.TextBody,
                Headers = message.Headers.Count > 0 ? message.Headers.ToDictionary(pair => pair.Key, pair => pair.Value) : null,
                Tags = message.Tags.Count > 0 ? message.Tags.Keys.ToArray() : null
            };

            if (message.Attachments.Count > 0)
            {
                var attachments = new List<BrevoAttachmentDto>(message.Attachments.Count);
                foreach (var attachment in message.Attachments)
                {
                    attachments.Add(new BrevoAttachmentDto
                    {
                        Name = attachment.FileName,
                        Content = Convert.ToBase64String(attachment.Content)
                    });
                }
                request.Attachment = attachments.ToArray();
            }

            return request;
        }

        private static BrevoRecipientDto[]? ToArray(IReadOnlyList<EmailRecipient> recipients)
        {
            if (recipients.Count == 0)
                return null;
            var result = new BrevoRecipientDto[recipients.Count];
            for (var i = 0; i < recipients.Count; i++)
            {
                result[i] = new BrevoRecipientDto
                {
                    Email = recipients[i].Address.Address,
                    Name = recipients[i].Address.DisplayName
                };
            }
            return result;
        }

        private static string? TryParseMessageId(string body)
        {
            try
            {
                var response = JsonSerializer.Deserialize<BrevoSendResponse>(body, JsonOptions);
                return response?.MessageId;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? TryParseError(string body)
        {
            try
            {
                var error = JsonSerializer.Deserialize<BrevoErrorResponse>(body, JsonOptions);
                return string.IsNullOrWhiteSpace(error?.Message)
                    ? null
                    : $"Brevo rejected the request: {error?.Message}";
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HttpClient CreateHttpClient(BrevoOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
