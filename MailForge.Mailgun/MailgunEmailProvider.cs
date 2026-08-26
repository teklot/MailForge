using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.Mailgun
{
    /// <summary>
    /// Delivers email through the Mailgun API (https://www.mailgun.com) using HttpClient.
    /// </summary>
    public sealed class MailgunEmailProvider : IEmailProvider
    {
        private readonly MailgunOptions _options;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from Mailgun options.</summary>
        public MailgunEmailProvider(MailgunOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new ArgumentException("A Mailgun API key is required.", nameof(options));
            if (string.IsNullOrWhiteSpace(_options.Domain))
                throw new ArgumentException("A Mailgun domain is required.", nameof(options));
            _httpClient = CreateHttpClient(_options);
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        public MailgunEmailProvider(MailgunOptions options, HttpClient httpClient)
            : this(options)
        {
            _httpClient = httpClient;
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Mailgun";

        /// <summary>The capabilities descriptor.</summary>
        public ProviderCapabilities Capabilities { get; } = new ProviderCapabilities(
            supportsAttachments: true,
            supportsInlineImages: true,
            supportsHeaders: true,
            supportsTags: true);

        /// <summary>Transmits a message through the Mailgun API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            using (var content = BuildFormData(message))
            using (var httpRequest = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUri, $"v3/{_options.Domain}/messages"))
            {
                Content = content
            })
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{_options.ApiKey}"));
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                HttpResponseMessage response;
                try
                {
                    response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                }
                catch (Exception exception)
                {
                    throw new EmailException(
                        $"Mailgun request failed: {exception.Message}",
                        exception,
                        isTransient: true);
                }

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var id = TryParseMessageId(body);
                        return ProviderDeliveryResult.Success(id, $"Accepted by Mailgun (HTTP {(int)response.StatusCode}).");
                    }

                    var isTransient = response.StatusCode == (HttpStatusCode)429 ||
                                      (int)response.StatusCode >= 500;
                    var messageText = TryParseError(body) ?? $"Mailgun rejected the request (HTTP {(int)response.StatusCode}).";
                    throw new EmailException(messageText, isTransient);
                }
            }
        }

        private MultipartFormDataContent BuildFormData(EmailMessage message)
        {
            var form = new MultipartFormDataContent();

            if (message.From != null)
                form.Add(new StringContent(message.From.ToString()), "from");

            AddRecipients(form, "to", message.ToRecipients);
            AddRecipients(form, "cc", message.CcRecipients);
            AddRecipients(form, "bcc", message.BccRecipients);

            if (!string.IsNullOrWhiteSpace(message.Subject))
                form.Add(new StringContent(message.Subject), "subject");

            if (!string.IsNullOrWhiteSpace(message.TextBody))
                form.Add(new StringContent(message.TextBody), "text");

            if (!string.IsNullOrWhiteSpace(message.HtmlBody))
                form.Add(new StringContent(message.HtmlBody), "html");

            foreach (var header in message.Headers)
                form.Add(new StringContent(header.Value), $"h:{header.Key}");

            foreach (var tag in message.Tags)
                form.Add(new StringContent(tag.Value), "o:tag");

            foreach (var attachment in message.Attachments)
            {
                if (attachment.IsInline)
                {
                    var imageContent = new ByteArrayContent(attachment.Content);
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.MediaType);
                    form.Add(imageContent, "inline", attachment.FileName);
                    if (!string.IsNullOrWhiteSpace(attachment.ContentId))
                        imageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline")
                        {
                            Name = attachment.FileName,
                            FileName = attachment.FileName
                        };
                }
                else
                {
                    var fileContent = new ByteArrayContent(attachment.Content);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(attachment.MediaType);
                    form.Add(fileContent, "attachment", attachment.FileName);
                }
            }

            return form;
        }

        private static void AddRecipients(MultipartFormDataContent form, string fieldName, IReadOnlyList<EmailRecipient> recipients)
        {
            foreach (var recipient in recipients)
                form.Add(new StringContent(recipient.Address.ToString()), fieldName);
        }

        private static string? TryParseMessageId(string body)
        {
            try
            {
                var response = JsonSerializer.Deserialize<MailgunSendResponse>(body, JsonOptions);
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
                var error = JsonSerializer.Deserialize<MailgunErrorResponse>(body, JsonOptions);
                return string.IsNullOrWhiteSpace(error?.Message)
                    ? null
                    : $"Mailgun rejected the request: {error?.Message}";
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static HttpClient CreateHttpClient(MailgunOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            return client;
        }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>The response returned by the Mailgun API on success.</summary>
        internal sealed class MailgunSendResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }
        }

        /// <summary>The error payload returned by the Mailgun API on failure.</summary>
        internal sealed class MailgunErrorResponse
        {
            [JsonPropertyName("message")]
            public string? Message { get; set; }
        }
    }
}
