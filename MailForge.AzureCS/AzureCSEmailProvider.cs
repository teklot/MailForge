using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.AzureCS
{
    /// <summary>
    /// Delivers email through the Azure Communication Services Email REST API
    /// (see https://learn.microsoft.com/azure/communication-services/quickstarts/email) using HttpClient
    /// with HMAC-SHA256 signed headers (no Azure SDK dependency).
    /// </summary>
    public sealed class AzureCSEmailProvider : IEmailProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly Regex CidReferencePattern = new Regex(
            @"cid:([\w\-\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly AzureCSOptions _options;
        private readonly string _senderAddress;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from Azure communication services options.</summary>
        public AzureCSEmailProvider(AzureCSOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.Endpoint))
                throw new ArgumentException("An Azure Communication Services endpoint is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(_options.AccessKey))
                throw new ArgumentException("An Azure Communication Services access key is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(_options.SenderAddress))
                throw new ArgumentException("A verified sender address is required.", nameof(options));
            _senderAddress = _options.SenderAddress!;
            _httpClient = CreateHttpClient(_options);
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        public AzureCSEmailProvider(AzureCSOptions options, HttpClient httpClient)
            : this(options)
        {
            _httpClient = httpClient;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Azure Communication Services";

        /// <summary>
        /// The capabilities descriptor.
        /// SupportsInlineImages is true: inline images (cid: references) are rewritten as
        /// data: URIs in the HTML body since ACS has no cid: attachment mechanism.
        /// </summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: true,
            supportsHeaders: true,
            supportsTags: false);

        /// <summary>Transmits a message through the Azure Communication Services Email API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var request = BuildRequest(message);
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            var requestUri = BuildRequestUri();
            var headers = AzureCSHmacSigner.Sign(
                "POST",
                requestUri.PathAndQuery,
                requestUri.Authority,
                bodyBytes,
                _options.AccessKey!);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new ByteArrayContent(bodyBytes)
            })
            {
                httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                httpRequest.Headers.TryAddWithoutValidation("x-ms-date", headers.Date);
                httpRequest.Headers.TryAddWithoutValidation("x-ms-content-sha256", headers.ContentHash);
                httpRequest.Headers.TryAddWithoutValidation("Authorization", headers.Authorization);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new EmailException(
                        $"Azure Communication Services request failed: {exception.Message}",
                        exception,
                        isTransient: true);
                }

                using (response)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var id = TryParseMessageId(responseBody);
                        return ProviderDeliveryResult.Success(id, $"Accepted by Azure Communication Services (HTTP {(int)response.StatusCode}).");
                    }

                    var isTransient = response.StatusCode == (HttpStatusCode)429 ||
                                      (int)response.StatusCode >= 500;
                    var messageText = TryParseError(responseBody)
                                      ?? $"Azure Communication Services rejected the request (HTTP {(int)response.StatusCode}).";
                    throw new EmailException(messageText, isTransient);
                }
            }
        }

        private Uri BuildRequestUri()
        {
            var endpoint = _options.Endpoint!.TrimEnd('/');
            return new Uri($"{endpoint}/emails:send?api-version={_options.ApiVersion}", UriKind.Absolute);
        }

        private AzureCSSendRequest BuildRequest(EmailMessage message)
        {
            var recipients = new AzureCSRecipientsDto
            {
                To = ToArray(message.ToRecipients),
                Cc = ToArray(message.CcRecipients),
                Bcc = ToArray(message.BccRecipients)
            };

            var html = EmbedInlineImagesAsDataUris(message);

            var request = new AzureCSSendRequest
            {
                SenderAddress = _senderAddress,
                Content = new AzureCSContentDto
                {
                    Subject = message.Subject,
                    PlainText = message.TextBody,
                    Html = html
                },
                Recipients = recipients,
                Headers = message.Headers.Count > 0 ? message.Headers.ToDictionary(pair => pair.Key, pair => pair.Value) : null,
                UserEngagementTrackingDisabled = true
            };

            if (message.Attachments.Count > 0)
            {
                var attachments = new List<AzureCSAttachmentDto>(message.Attachments.Count);
                foreach (var attachment in message.Attachments)
                {
                    if (attachment.IsInline)
                        continue;

                    attachments.Add(new AzureCSAttachmentDto
                    {
                        Name = attachment.FileName,
                        ContentType = attachment.MediaType,
                        ContentInBase64 = Convert.ToBase64String(attachment.Content)
                    });
                }
                request.Attachments = attachments.Count > 0 ? attachments.ToArray() : null;
            }

            return request;
        }

        /// <summary>
        /// Replaces <c>cid:&lt;contentId&gt;</c> references in the HTML body with base64 <c>data:</c> URIs.
        /// Azure Communication Services has no <c>cid:</c> attachment mechanism, so inline images
        /// must be embedded directly in the HTML content.
        /// </summary>
        private static string? EmbedInlineImagesAsDataUris(EmailMessage message)
        {
            if (string.IsNullOrEmpty(message.HtmlBody))
                return message.HtmlBody;

            var inlineById = new Dictionary<string, EmailAttachment>(StringComparer.OrdinalIgnoreCase);
            foreach (var attachment in message.Attachments)
            {
                if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
                    inlineById[attachment.ContentId!] = attachment;
            }

            if (inlineById.Count == 0)
                return message.HtmlBody;

            return CidReferencePattern.Replace(message.HtmlBody, match =>
            {
                var contentId = match.Groups[1].Value;
                if (!inlineById.TryGetValue(contentId, out var attachment))
                    return match.Value;

                return "data:" + attachment.MediaType + ";base64," + Convert.ToBase64String(attachment.Content);
            });
        }

        private static AzureCSAddressDto[]? ToArray(IReadOnlyList<EmailRecipient> recipients)
        {
            if (recipients.Count == 0)
                return null;
            var result = new AzureCSAddressDto[recipients.Count];
            for (var i = 0; i < recipients.Count; i++)
            {
                result[i] = new AzureCSAddressDto
                {
                    Address = recipients[i].Address.Address,
                    DisplayName = recipients[i].Address.DisplayName
                };
            }
            return result;
        }

        private static string? TryParseMessageId(string body)
        {
            try
            {
                var response = JsonSerializer.Deserialize<AzureCSSendResponse>(body, JsonOptions);
                return response?.Id;
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
                var error = JsonSerializer.Deserialize<AzureCSErrorResponse>(body, JsonOptions);
                var message = error?.Error?.Message;
                return string.IsNullOrWhiteSpace(message)
                    ? null
                    : $"Azure Communication Services rejected the request: {message}";
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HttpClient CreateHttpClient(AzureCSOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}