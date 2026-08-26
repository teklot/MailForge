using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.ZeptoMail
{
    /// <summary>
    /// Delivers email through the Zoho ZeptoMail API (https://zeptomail.zoho.com) using HttpClient.
    /// </summary>
    public sealed class ZeptoMailEmailProvider : IEmailProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ZeptoMailOptions _options;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from ZeptoMail options.</summary>
        public ZeptoMailEmailProvider(ZeptoMailOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.SendApiKey))
                throw new ArgumentException("A ZeptoMail API key is required.", nameof(options));
            _httpClient = CreateHttpClient(_options);
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        public ZeptoMailEmailProvider(ZeptoMailOptions options, HttpClient httpClient)
            : this(options)
        {
            _httpClient = httpClient;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "ZeptoMail";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: true,
            supportsHeaders: false,
            supportsTags: false);

        /// <summary>Transmits a message through the ZeptoMail API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var request = BuildRequest(message);
            var json = JsonSerializer.Serialize(request, JsonOptions);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, "v1.1/email"))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            })
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Zoho-enczapikey", _options.SendApiKey!);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new EmailException(
                        $"ZeptoMail request failed: {exception.Message}",
                        exception,
                        isTransient: true);
                }

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var messageId = TryParseMessageId(body);
                        return ProviderDeliveryResult.Success(messageId, $"Accepted by ZeptoMail (HTTP {(int)response.StatusCode}).");
                    }

                    var isTransient = response.StatusCode == (HttpStatusCode)429 ||
                                      (int)response.StatusCode >= 500;

                    var messageText = TryParseError(body) ?? $"ZeptoMail rejected the request (HTTP {(int)response.StatusCode}).";
                    throw new EmailException(messageText, isTransient);
                }
            }
        }

        private ZeptoMailSendRequest BuildRequest(EmailMessage message)
        {
            var request = new ZeptoMailSendRequest
            {
                From = new ZeptoMailAddressDto
                {
                    Address = message.From?.ToString(),
                    Name = message.From?.DisplayName
                },
                To = ToRecipientArray(message.ToRecipients),
                Cc = ToRecipientArray(message.CcRecipients),
                Bcc = ToRecipientArray(message.BccRecipients),
                Subject = message.Subject,
                HtmlBody = message.HtmlBody,
                TextBody = message.TextBody
            };

            if (message.Attachments.Count > 0)
            {
                var attachments = new List<ZeptoMailAttachmentDto>(message.Attachments.Count);
                foreach (var attachment in message.Attachments)
                {
                    attachments.Add(new ZeptoMailAttachmentDto
                    {
                        FileName = attachment.FileName,
                        FileContent = Convert.ToBase64String(attachment.Content),
                        MimeType = attachment.MediaType
                    });
                }
                request.Attachments = attachments.ToArray();
            }

            return request;
        }

        private static ZeptoMailRecipientDto[]? ToRecipientArray(IReadOnlyList<EmailRecipient> recipients)
        {
            if (recipients.Count == 0)
                return null;
            var result = new ZeptoMailRecipientDto[recipients.Count];
            for (var i = 0; i < recipients.Count; i++)
            {
                result[i] = new ZeptoMailRecipientDto
                {
                    EmailAddress = new ZeptoMailAddressDto
                    {
                        Address = recipients[i].Address.ToString(),
                        Name = recipients[i].Address.DisplayName
                    }
                };
            }
            return result;
        }

        private static string? TryParseMessageId(string body)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ZeptoMailSendResponse>(body, JsonOptions);
                return response?.Data is { Length: > 0 } data ? data[0].Message : null;
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
                var error = JsonSerializer.Deserialize<ZeptoMailErrorResponse>(body, JsonOptions);
                return string.IsNullOrWhiteSpace(error?.Message)
                    ? null
                    : $"ZeptoMail rejected the request: {error?.Message}";
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HttpClient CreateHttpClient(ZeptoMailOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
