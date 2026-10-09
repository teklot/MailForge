using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Telegram
{
    /// <summary>
    /// Delivers notifications through the Telegram Bot API (https://telegram.org) using
    /// HttpClient. Text messages go to sendMessage, photos and URL documents to
    /// sendPhoto/sendDocument, and inline documents to multipart sendDocument uploads.
    /// Transient failures (429, 5xx, transport errors) are thrown as
    /// <see cref="NotificationException"/> with <c>IsTransient</c> set; API rejections are
    /// returned as failed results.
    /// </summary>
    public sealed class TelegramNotificationProvider : INotificationProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly TelegramOptions _options;
        private readonly HttpClient _httpClient;

        /// <summary>Creates the provider from Telegram options.</summary>
        public TelegramNotificationProvider(TelegramOptions options)
            : this(options, CreateHttpClient(options ?? throw new ArgumentNullException(nameof(options))))
        {
        }

        /// <summary>Creates the provider using a pre-configured HttpClient (primarily for testing).</summary>
        internal TelegramNotificationProvider(TelegramOptions options, HttpClient httpClient)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(_options.BotToken))
                throw new ArgumentException("A Telegram bot token is required.", nameof(options));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>The provider display name.</summary>
        public string Name => "Telegram";

        /// <summary>The channel this provider serves.</summary>
        public ChannelType Channel => ChannelType.Telegram;

        /// <summary>Transmits a notification through the Telegram Bot API.</summary>
        public async Task<ProviderDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            if (!(notification.Content is TelegramContent content))
                return ProviderDeliveryResult.Failure(
                    $"The Telegram provider requires a {nameof(TelegramContent)} payload but received '{notification.Content.GetType().Name}'.");

            var messageIds = new List<string>(content.ChatIds.Count);
            for (var i = 0; i < content.ChatIds.Count; i++)
            {
                var result = await SendToChatAsync(content, content.ChatIds[i], cancellationToken);
                if (!result.Succeeded)
                {
                    var details = result.Details ?? "Telegram rejected the request.";
                    if (messageIds.Count > 0)
                        details += $" {messageIds.Count} of {content.ChatIds.Count} chat(s) accepted this message before the failure.";
                    return ProviderDeliveryResult.Failure(details);
                }

                if (!string.IsNullOrEmpty(result.ProviderMessageId))
                    messageIds.Add(result.ProviderMessageId!);
            }

            return ProviderDeliveryResult.Success(
                messageIds.Count > 0 ? string.Join(",", messageIds) : null,
                $"Accepted by Telegram for {content.ChatIds.Count} chat(s).");
        }

        private async Task<ProviderDeliveryResult> SendToChatAsync(TelegramContent content, string chatId, CancellationToken cancellationToken)
        {
            string method;
            HttpRequestMessage request;

            if (content.Media == null || content.Media.Url != null)
            {
                method = content.Media == null ? "sendMessage"
                    : content.Media.Kind == TelegramMediaKind.Photo ? "sendPhoto" : "sendDocument";
                var json = BuildJsonBody(content, chatId);
                request = new HttpRequestMessage(HttpMethod.Post, BuildUri(method))
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            }
            else
            {
                method = "sendDocument";
                request = new HttpRequestMessage(HttpMethod.Post, BuildUri(method))
                {
                    Content = BuildMultipart(content, chatId)
                };
            }

            using (request)
            {
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
                        $"Telegram request failed: {exception.Message}",
                        exception,
                        isTransient: true);
                }

                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var parsed = TryParseResponse(body);
                    var status = response.StatusCode;

                    if (response.IsSuccessStatusCode && parsed != null && parsed.Ok)
                    {
                        var messageId = parsed.Result == null
                            ? null
                            : parsed.Result.MessageId.ToString(CultureInfo.InvariantCulture);
                        return ProviderDeliveryResult.Success(messageId, $"Accepted by Telegram (HTTP {(int)status}).");
                    }

                    var transient = status == (HttpStatusCode)429 || (int)status >= 500;
                    var description = BuildDescription(status, parsed);

                    if (transient)
                        throw new NotificationException(description, isTransient: true);

                    if (response.IsSuccessStatusCode && parsed == null)
                        throw new NotificationException(
                            "Telegram returned an unexpected response.",
                            isTransient: true);

                    return ProviderDeliveryResult.Failure(description);
                }
            }
        }

        private Uri BuildUri(string method) =>
            new Uri(_options.BaseUri.AbsoluteUri.TrimEnd('/') + "/bot" +
                    Uri.EscapeDataString(_options.BotToken!) + "/" + method);

        private static string BuildJsonBody(TelegramContent content, string chatId)
        {
            if (content.Media == null)
            {
                var request = new TelegramTextRequest
                {
                    ChatId = chatId,
                    Text = content.Text,
                    ParseMode = ToWireParseMode(content.ParseMode),
                    ReplyMarkup = BuildKeyboard(content.Keyboard)
                };
                return JsonSerializer.Serialize(request, JsonOptions);
            }

            var mediaRequest = new TelegramMediaRequest
            {
                ChatId = chatId,
                Photo = content.Media.Kind == TelegramMediaKind.Photo ? content.Media.Url : null,
                Document = content.Media.Kind == TelegramMediaKind.Document ? content.Media.Url : null,
                Caption = content.Text,
                ParseMode = ToWireParseMode(content.ParseMode),
                ReplyMarkup = BuildKeyboard(content.Keyboard)
            };
            return JsonSerializer.Serialize(mediaRequest, JsonOptions);
        }

        private static MultipartFormDataContent BuildMultipart(TelegramContent content, string chatId)
        {
            var media = content.Media!;
            var multipart = new MultipartFormDataContent();
            multipart.Add(new StringContent(chatId), "chat_id");
            multipart.Add(new StringContent(content.Text), "caption");

            var parseMode = ToWireParseMode(content.ParseMode);
            if (parseMode != null)
                multipart.Add(new StringContent(parseMode), "parse_mode");

            if (content.Keyboard != null)
            {
                var keyboardJson = JsonSerializer.Serialize(BuildKeyboard(content.Keyboard), JsonOptions);
                multipart.Add(new StringContent(keyboardJson), "reply_markup");
            }

            var file = new ByteArrayContent(media.Content!);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            multipart.Add(file, "document", media.FileName!);
            return multipart;
        }

        private static TelegramKeyboardDto? BuildKeyboard(TelegramKeyboard? keyboard)
        {
            if (keyboard == null)
                return null;

            var dto = new TelegramKeyboardDto();
            foreach (var row in keyboard.Rows)
            {
                var rowDto = new List<TelegramKeyboardButtonDto>(row.Count);
                foreach (var button in row)
                {
                    rowDto.Add(new TelegramKeyboardButtonDto
                    {
                        Text = button.Text,
                        Url = button.Url,
                        CallbackData = button.CallbackData
                    });
                }
                dto.InlineKeyboard.Add(rowDto);
            }
            return dto;
        }

        private static string? ToWireParseMode(TelegramParseMode parseMode)
        {
            switch (parseMode)
            {
                case TelegramParseMode.Html:
                    return "HTML";
                case TelegramParseMode.MarkdownV2:
                    return "MarkdownV2";
                default:
                    return null;
            }
        }

        private static TelegramApiResponse? TryParseResponse(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;
            try
            {
                return JsonSerializer.Deserialize<TelegramApiResponse>(body, JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string BuildDescription(HttpStatusCode status, TelegramApiResponse? parsed)
        {
            if (parsed != null && !string.IsNullOrWhiteSpace(parsed.Description))
            {
                var description = $"Telegram rejected the request: {parsed.Description}";
                if (parsed.Parameters != null && parsed.Parameters.RetryAfter > 0)
                    description += $" (retry after {parsed.Parameters.RetryAfter}s)";
                return description;
            }

            return $"Telegram rejected the request (HTTP {(int)status}).";
        }

        private static HttpClient CreateHttpClient(TelegramOptions options)
        {
            var client = new HttpClient { Timeout = options.Timeout };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}
