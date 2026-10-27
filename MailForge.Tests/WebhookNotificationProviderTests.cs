using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using MailForge.Communication.Models;
using MailForge.Models;
using MailForge.Webhook;

namespace MailForge.Tests
{
    public class WebhookNotificationProviderTests
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

        private static WebhookNotificationProvider CreateProvider(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler,
            Action<WebhookOptions>? configure = null)
        {
            var options = new WebhookOptions { Endpoint = new Uri("https://hooks.example.com/notify") };
            configure?.Invoke(options);
            return new WebhookNotificationProvider(options, new HttpClient(new StubHttpMessageHandler(handler)));
        }

        private static Notification CreateNotification(WebhookContent content) =>
            new Notification(ChannelType.Webhook, content);

        private static HttpResponseMessage Ok() => new HttpResponseMessage(HttpStatusCode.OK);

        [Fact]
        public async Task SendAsync_PostsPayloadToEndpoint()
        {
            string? capturedUri = null;
            HttpMethod? capturedMethod = null;
            string? capturedBody = null;
            string? capturedContentType = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                capturedUri = request.RequestUri!.ToString();
                capturedMethod = request.Method;
                capturedBody = await request.Content!.ReadAsStringAsync(ct);
                capturedContentType = request.Content.Headers.ContentType!.MediaType;
                return Ok();
            });

            var result = await provider.SendAsync(
                CreateNotification(new WebhookContent("{\"event\":\"ping\"}")),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("https://hooks.example.com/notify", capturedUri);
            Assert.Equal(HttpMethod.Post, capturedMethod);
            Assert.Equal("{\"event\":\"ping\"}", capturedBody);
            Assert.Equal("application/json", capturedContentType);
        }

        [Fact]
        public async Task SendAsync_CustomContentType_IsApplied()
        {
            string? capturedContentType = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                capturedContentType = request.Content!.Headers.ContentType!.MediaType;
                return Ok();
            });

            await provider.SendAsync(
                CreateNotification(new WebhookContent("<ping/>", "application/xml")),
                TestContext.Current.CancellationToken);

            Assert.Equal("application/xml", capturedContentType);
        }

        [Fact]
        public async Task SendAsync_CustomHeaders_AreForwarded()
        {
            string? authHeader = null;

            var provider = CreateProvider(
                async (request, ct) =>
                {
                    request.Headers.TryGetValues("Authorization", out var values);
                    authHeader = values?.FirstOrDefault();
                    return Ok();
                },
                options => options.Headers["Authorization"] = "Bearer secret-token");

            await provider.SendAsync(
                CreateNotification(new WebhookContent("{}")),
                TestContext.Current.CancellationToken);

            Assert.Equal("Bearer secret-token", authHeader);
        }

        [Fact]
        public async Task SendAsync_WithSigningSecret_WritesHmacSha256Signature()
        {
            string? capturedSignature = null;
            const string payload = "{\"event\":\"ping\"}";
            const string secret = "top-secret";

            var provider = CreateProvider(
                async (request, ct) =>
                {
                    request.Headers.TryGetValues("X-MailForge-Signature", out var values);
                    capturedSignature = values?.FirstOrDefault();
                    return Ok();
                },
                options => options.SigningSecret = secret);

            await provider.SendAsync(
                CreateNotification(new WebhookContent(payload)),
                TestContext.Current.CancellationToken);

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

            Assert.Equal(expected, capturedSignature);
        }

        [Fact]
        public async Task SendAsync_SignaturePrefix_IsPrepended()
        {
            string? capturedSignature = null;

            var provider = CreateProvider(
                async (request, ct) =>
                {
                    request.Headers.TryGetValues("X-MailForge-Signature", out var values);
                    capturedSignature = values?.FirstOrDefault();
                    return Ok();
                },
                options =>
                {
                    options.SigningSecret = "top-secret";
                    options.SignaturePrefix = "sha256=";
                });

            await provider.SendAsync(
                CreateNotification(new WebhookContent("{}")),
                TestContext.Current.CancellationToken);

            Assert.NotNull(capturedSignature);
            Assert.StartsWith("sha256=", capturedSignature);
        }

        [Fact]
        public async Task SendAsync_WithoutSigningSecret_SendsNoSignature()
        {
            var hasSignature = true;

            var provider = CreateProvider((request, ct) =>
            {
                hasSignature = request.Headers.Contains("X-MailForge-Signature");
                return Task.FromResult(Ok());
            });

            await provider.SendAsync(
                CreateNotification(new WebhookContent("{}")),
                TestContext.Current.CancellationToken);

            Assert.False(hasSignature);
        }

        [Fact]
        public async Task SendAsync_ClientError_ReturnsFailureResult()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)));

            var result = await provider.SendAsync(
                CreateNotification(new WebhookContent("{}")),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("HTTP 400", result.Details);
        }

        [Fact]
        public async Task SendAsync_RateLimit_ThrowsTransientNotificationException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)));

            var exception = await Assert.ThrowsAsync<NotificationException>(
                () => provider.SendAsync(
                    CreateNotification(new WebhookContent("{}")),
                    TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Contains("HTTP 429", exception.Message);
        }

        [Fact]
        public async Task SendAsync_ServerError_ThrowsTransientNotificationException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

            var exception = await Assert.ThrowsAsync<NotificationException>(
                () => provider.SendAsync(
                    CreateNotification(new WebhookContent("{}")),
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
                    CreateNotification(new WebhookContent("{}")),
                    TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Contains("boom", exception.Message);
        }

        [Fact]
        public async Task SendAsync_WrongContentPayload_ReturnsFailureResult()
        {
            var provider = CreateProvider((request, ct) => Task.FromResult(Ok()));

            var message = EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Test")
                .Build();
            var result = await provider.SendAsync(
                new Notification(ChannelType.Webhook, new EmailContent(message)),
                TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("WebhookContent", result.Details);
        }

        [Fact]
        public void Constructor_RequiresEndpoint()
        {
            Assert.Throws<ArgumentException>(() => new WebhookNotificationProvider(new WebhookOptions()));
        }
    }
}
