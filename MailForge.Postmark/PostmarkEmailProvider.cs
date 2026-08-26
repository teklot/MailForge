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

namespace MailForge.Postmark
{
    /// <summary>
    /// Delivers email through the Postmark API (https://postmarkapp.com) using HttpClient.
    /// </summary>
    public sealed class PostmarkEmailProvider : IEmailProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly PostmarkOptions _options;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from Postmark options.</summary>
        public PostmarkEmailProvider(PostmarkOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.ServerToken))
                throw new ArgumentException("A Postmark server token is required.", nameof(options));
            _httpClient = CreateHttpClient(_options);
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        public PostmarkEmailProvider(PostmarkOptions options, HttpClient httpClient)
            : this(options)
        {
            _httpClient = httpClient;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Postmark";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: true,
            supportsHeaders: true,
            supportsTags: true);

        /// <summary>Transmits a message through the Postmark API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var request = BuildRequest(message);
            var json = JsonSerializer.Serialize(request, JsonOptions);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, "email"))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            })
            {
                httpRequest.Headers.Add("X-Postmark-Server-Token", _options.ServerToken!);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new EmailException(
                        $"Postmark request failed: {exception.Message}",
                        exception,
                        isTransient: true);
                }

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var id = TryParseMessageId(body);
                        return ProviderDeliveryResult.Success(id, $"Accepted by Postmark (HTTP {(int)response.StatusCode}).");
                    }

                    var isTransient = response.StatusCode == (HttpStatusCode)429 ||
                                      (int)response.StatusCode >= 500;
                    if (response.StatusCode == (HttpStatusCode)401)
                        isTransient = false;
                    var messageText = TryParseError(body) ?? $"Postmark rejected the request (HTTP {(int)response.StatusCode}).";
                    throw new EmailException(messageText, isTransient);
                }
            }
        }

        private PostmarkSendRequest BuildRequest(EmailMessage message)
        {
            var request = new PostmarkSendRequest
            {
                From = message.From?.ToString(),
                To = ToSingle(message.ToRecipients),
                Cc = ToSingle(message.CcRecipients),
                Bcc = ToSingle(message.BccRecipients),
                Subject = message.Subject,
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody,
                MessageStream = _options.MessageStream
            };

            if (message.Headers.Count > 0)
            {
                request.Headers = message.Headers
                    .Select(pair => new PostmarkHeaderDto { Name = pair.Key, Value = pair.Value })
                    .ToArray();
            }

            if (message.Tags.Count > 0)
            {
                request.Metadata = message.Tags.ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            if (message.Attachments.Count > 0)
            {
                var attachments = new List<PostmarkAttachmentDto>(message.Attachments.Count);
                foreach (var attachment in message.Attachments)
                {
                    var dto = new PostmarkAttachmentDto
                    {
                        Name = attachment.FileName,
                        Content = Convert.ToBase64String(attachment.Content),
                        ContentType = attachment.MediaType
                    };

                    if (attachment.IsInline && !string.IsNullOrWhiteSpace(attachment.ContentId))
                        dto.ContentID = attachment.ContentId;

                    attachments.Add(dto);
                }
                request.Attachments = attachments.ToArray();
            }

            return request;
        }

        private static string? ToSingle(IReadOnlyList<EmailRecipient> recipients)
        {
            if (recipients.Count == 0)
                return null;
            return string.Join(", ", recipients.Select(r => r.Address.ToString()));
        }

        private static string? TryParseMessageId(string body)
        {
            try
            {
                var response = JsonSerializer.Deserialize<PostmarkSendResponse>(body, JsonOptions);
                return response?.MessageID;
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
                var error = JsonSerializer.Deserialize<PostmarkErrorResponse>(body, JsonOptions);
                return string.IsNullOrWhiteSpace(error?.Message)
                    ? null
                    : $"Postmark rejected the request: {error?.Message}";
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HttpClient CreateHttpClient(PostmarkOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
