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

namespace MailForge.Resend
{
    /// <summary>
    /// Delivers email through the Resend API (https://resend.com) using HttpClient.
    /// </summary>
    public sealed class ResendEmailProvider : IEmailProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ResendOptions _options;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from Resend options.</summary>
        public ResendEmailProvider(ResendOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new ArgumentException("A Resend API key is required.", nameof(options));
            _httpClient = CreateHttpClient(_options);
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        public ResendEmailProvider(ResendOptions options, HttpClient httpClient)
            : this(options)
        {
            _httpClient = httpClient;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Resend";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: false,
            supportsHeaders: true,
            supportsTags: true);

        /// <summary>Transmits a message through the Resend API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var request = BuildRequest(message);
            var json = JsonSerializer.Serialize(request, JsonOptions);

            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, "emails"))
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            })
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey!);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new EmailException(
                        $"Resend request failed: {exception.Message}",
                        exception,
                        isTransient: true);
                }

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var id = TryParseMessageId(body);
                        return ProviderDeliveryResult.Success(id, $"Accepted by Resend (HTTP {(int)response.StatusCode}).");
                    }

                    var isTransient = response.StatusCode == (HttpStatusCode)429 ||
                                      (int)response.StatusCode >= 500;
                    var messageText = TryParseError(body) ?? $"Resend rejected the request (HTTP {(int)response.StatusCode}).";
                    throw new EmailException(messageText, isTransient);
                }
            }
        }

        private ResendSendRequest BuildRequest(EmailMessage message)
        {
            var request = new ResendSendRequest
            {
                From = message.From?.ToString(),
                To = ToArray(message.ToRecipients),
                Cc = ToArray(message.CcRecipients),
                Bcc = ToArray(message.BccRecipients),
                Subject = message.Subject,
                Html = message.HtmlBody,
                Text = message.TextBody,
                Headers = message.Headers.Count > 0 ? message.Headers.ToDictionary(pair => pair.Key, pair => pair.Value) : null,
                Tags = message.Tags.Count > 0
                    ? message.Tags.Select(pair => new ResendTagDto { Name = pair.Key, Value = pair.Value }).ToArray()
                    : null
            };

            if (message.Attachments.Count > 0)
            {
                var attachments = new List<ResendAttachmentDto>(message.Attachments.Count);
                foreach (var attachment in message.Attachments)
                {
                    attachments.Add(new ResendAttachmentDto
                    {
                        FileName = attachment.FileName,
                        Content = Convert.ToBase64String(attachment.Content),
                        ContentType = attachment.MediaType
                    });
                }
                request.Attachments = attachments.ToArray();
            }

            return request;
        }

        private static string[]? ToArray(IReadOnlyList<EmailRecipient> recipients)
        {
            if (recipients.Count == 0)
                return null;
            var result = new string[recipients.Count];
            for (var i = 0; i < recipients.Count; i++)
                result[i] = recipients[i].Address.ToString();
            return result;
        }

        private static string? TryParseMessageId(string body)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ResendSendResponse>(body, JsonOptions);
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
                var error = JsonSerializer.Deserialize<ResendErrorResponse>(body, JsonOptions);
                return string.IsNullOrWhiteSpace(error?.Message)
                    ? null
                    : $"Resend rejected the request: {error?.Message}";
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HttpClient CreateHttpClient(ResendOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
