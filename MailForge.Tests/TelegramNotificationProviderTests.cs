using System.Net;
using System.Net.Http;
using System.Text.Json;
using MailForge.Communication.Models;
using MailForge.Models;
using MailForge.Telegram;

namespace MailForge.Tests
{
    public class TelegramNotificationProviderTests
    {
        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

            public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            {
                _handler = handler;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
                _handler(request, cancellationToken);
        }

        private static TelegramNotificationProvider CreateProvider(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            new TelegramNotificationProvider(
                new TelegramOptions { BotToken = "123:abc" },
                new HttpClient(new StubHttpMessageHandler(handler)));

        private static HttpResponseMessage Ok(int messageId) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($"{{\"ok\":true,\"result\":{{\"message_id\":{messageId}}}}}")
            };

        private static Notification CreateNotification(TelegramContent content) =>
            new Notification(ChannelType.Telegram, content);

        [Fact]
        public async Task SendAsync_TextMessage_PostsSendMessageAndParsesMessageId()
        {
            string? capturedUri = null;
            string? capturedJson = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                capturedUri = request.RequestUri!.ToString();
                capturedJson = await request.Content!.ReadAsStringAsync(ct);
                return Ok(555);
            });

            var result = await provider.SendAsync(
                CreateNotification(new TelegramContent("42", "Hi")),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("555", result.ProviderMessageId);
            Assert.NotNull(capturedJson);
            Assert.EndsWith("bot123%3Aabc/sendMessage", capturedUri);

            var json = JsonDocument.Parse(capturedJson!);
            Assert.Equal("42", json.RootElement.GetProperty("chat_id").GetString());
            Assert.Equal("Hi", json.RootElement.GetProperty("text").GetString());
            Assert.False(json.RootElement.TryGetProperty("parse_mode", out _));
        }

        [Fact]
        public async Task SendAsync_ColonInToken_KeepsHttpScheme()
        {
            string? capturedUri = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                capturedUri = request.RequestUri!.ToString();
                return Ok(1);
            });

            await provider.SendAsync(
                CreateNotification(new TelegramContent("42", "Hi")),
                TestContext.Current.CancellationToken);

            Assert.NotNull(capturedUri);
            Assert.StartsWith("https://", capturedUri);
            Assert.EndsWith("bot123%3Aabc/sendMessage", capturedUri);
        }

        [Fact]
        public async Task SendAsync_HtmlWithKeyboard_SerializesSnakeCasePayload()
        {
            string? capturedJson = null;
            var keyboard = new TelegramKeyboard(
                TelegramKeyboardButton.WithUrl("Docs", "https://example.com/docs"),
                TelegramKeyboardButton.WithCallbackData("Ack", "ack-1"));

            var provider = CreateProvider(async (request, ct) =>
            {
                capturedJson = await request.Content!.ReadAsStringAsync(ct);
                return Ok(1);
            });

            await provider.SendAsync(
                CreateNotification(new TelegramContent("42", "<b>Hi</b>", TelegramParseMode.Html, keyboard: keyboard)),
                TestContext.Current.CancellationToken);

            Assert.NotNull(capturedJson);
            var json = JsonDocument.Parse(capturedJson!);
            Assert.Equal("HTML", json.RootElement.GetProperty("parse_mode").GetString());

            var button0 = json.RootElement.GetProperty("reply_markup").GetProperty("inline_keyboard")[0][0];
            Assert.Equal("Docs", button0.GetProperty("text").GetString());
            Assert.Equal("https://example.com/docs", button0.GetProperty("url").GetString());

            var button1 = json.RootElement.GetProperty("reply_markup").GetProperty("inline_keyboard")[0][1];
            Assert.Equal("ack-1", button1.GetProperty("callback_data").GetString());
        }

        [Fact]
        public async Task SendAsync_PhotoMedia_UsesSendPhotoWithCaption()
        {
            string? capturedUri = null;
            string? capturedJson = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                capturedUri = request.RequestUri!.ToString();
                capturedJson = await request.Content!.ReadAsStringAsync(ct);
                return Ok(7);
            });

            await provider.SendAsync(
                CreateNotification(new TelegramContent(
                    "42",
                    "Look",
                    media: TelegramMedia.FromPhoto("https://example.com/pic.png"))),
                TestContext.Current.CancellationToken);

            Assert.NotNull(capturedJson);
            Assert.EndsWith("bot123%3Aabc/sendPhoto", capturedUri);

            var json = JsonDocument.Parse(capturedJson!);
            Assert.Equal("https://example.com/pic.png", json.RootElement.GetProperty("photo").GetString());
            Assert.Equal("Look", json.RootElement.GetProperty("caption").GetString());
        }

        [Fact]
        public async Task SendAsync_UrlDocument_UsesSendDocument()
        {
            string? capturedUri = null;
            string? capturedJson = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                capturedUri = request.RequestUri!.ToString();
                capturedJson = await request.Content!.ReadAsStringAsync(ct);
                return Ok(8);
            });

            await provider.SendAsync(
                CreateNotification(new TelegramContent(
                    "42",
                    "Report",
                    media: TelegramMedia.FromDocument("https://example.com/report.pdf"))),
                TestContext.Current.CancellationToken);

            Assert.NotNull(capturedJson);
            Assert.EndsWith("bot123%3Aabc/sendDocument", capturedUri);

            var json = JsonDocument.Parse(capturedJson!);
            Assert.Equal("https://example.com/report.pdf", json.RootElement.GetProperty("document").GetString());
        }

        [Fact]
        public async Task SendAsync_InlineDocument_UploadsMultipart()
        {
            string? capturedUri = null;
            string? capturedMediaType = null;
            string? capturedBody = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                capturedUri = request.RequestUri!.ToString();
                capturedMediaType = request.Content!.Headers.ContentType!.MediaType;
                capturedBody = await request.Content.ReadAsStringAsync(ct);
                return Ok(9);
            });

            await provider.SendAsync(
                CreateNotification(new TelegramContent(
                    "42",
                    "Report",
                    media: TelegramMedia.FromDocument(new byte[] { 1, 2, 3 }, "report.pdf"))),
                TestContext.Current.CancellationToken);

            Assert.NotNull(capturedBody);
            Assert.EndsWith("bot123%3Aabc/sendDocument", capturedUri);
            Assert.StartsWith("multipart/form-data", capturedMediaType);

            Assert.Contains("name=chat_id", capturedBody);
            Assert.Contains("Report", capturedBody);
            Assert.Contains("name=caption", capturedBody);
            Assert.Contains("name=document; filename=report.pdf", capturedBody);
        }

        [Fact]
        public async Task SendAsync_ApiRejection_ReturnsFailureResult()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"ok\":false,\"error_code\":400,\"description\":\"Bad Request: chat not found\"}")
                }));

            var result = await provider.SendAsync(
                CreateNotification(new TelegramContent("42", "Hi")),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("chat not found", result.Details);
        }

        [Fact]
        public async Task SendAsync_RateLimit_ThrowsTransientNotificationException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("{\"ok\":false,\"error_code\":429,\"description\":\"Too Many Requests\",\"parameters\":{\"retry_after\":5}}")
                }));

            var exception = await Assert.ThrowsAsync<NotificationException>(
                () => provider.SendAsync(
                    CreateNotification(new TelegramContent("42", "Hi")),
                    TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Contains("retry after 5s", exception.Message);
        }

        [Fact]
        public async Task SendAsync_ServerError_ThrowsTransientNotificationException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("oops")
                }));

            var exception = await Assert.ThrowsAsync<NotificationException>(
                () => provider.SendAsync(
                    CreateNotification(new TelegramContent("42", "Hi")),
                    TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Contains("HTTP 500", exception.Message);
        }

        [Fact]
        public async Task SendAsync_TransportFailure_ThrowsTransientNotificationException()
        {
            var provider = CreateProvider((request, ct) => throw new HttpRequestException("boom"));

            var exception = await Assert.ThrowsAsync<NotificationException>(
                () => provider.SendAsync(
                    CreateNotification(new TelegramContent("42", "Hi")),
                    TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Contains("boom", exception.Message);
        }

        [Fact]
        public async Task SendAsync_MultipleChatIds_SendsToEachAndAggregatesMessageIds()
        {
            var calls = 0;
            var provider = CreateProvider((request, ct) =>
            {
                calls++;
                return Task.FromResult(Ok(calls == 1 ? 11 : 22));
            });

            var result = await provider.SendAsync(
                CreateNotification(new TelegramContent(new[] { "1", "2" }, "Hi")),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("11,22", result.ProviderMessageId);
            Assert.Equal(2, calls);
            Assert.Contains("2 chat(s)", result.Details);
        }

        [Fact]
        public async Task SendAsync_MultipleChatIds_SecondChatRejected_ReturnsFailureWithPartialDetails()
        {
            var calls = 0;
            var provider = CreateProvider((request, ct) =>
            {
                calls++;
                if (calls == 1)
                    return Task.FromResult(Ok(11));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"ok\":false,\"error_code\":400,\"description\":\"Bad Request: chat not found\"}")
                });
            });

            var result = await provider.SendAsync(
                CreateNotification(new TelegramContent(new[] { "1", "2" }, "Hi")),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("chat not found", result.Details);
            Assert.Contains("1 of 2", result.Details);
        }

        [Fact]
        public async Task SendAsync_WrongContentPayload_ReturnsFailureResult()
        {
            var provider = CreateProvider((request, ct) => Task.FromResult(Ok(1)));

            var message = EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Test")
                .Build();
            var result = await provider.SendAsync(
                new Notification(ChannelType.Telegram, new EmailContent(message)),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("TelegramContent", result.Details);
        }

        [Fact]
        public void Constructor_RequiresBotToken()
        {
            Assert.Throws<ArgumentException>(() => new TelegramNotificationProvider(new TelegramOptions()));
        }
    }
}
