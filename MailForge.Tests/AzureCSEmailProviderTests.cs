using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailForge.AzureCS;
using MailForge.Models;

namespace MailForge.Tests
{
    public class AzureCSEmailProviderTests
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

        private static EmailMessage CreateMessage() =>
            EmailMessage.Create()
                .From("sender@example.com", "Sender")
                .To("user@example.com")
                .Subject("Test")
                .Html("<p>Hi</p>")
                .Text("Hi")
                .Header("X-Custom", "value")
                .Attachment(new EmailAttachment("file.txt", new byte[] { 1, 2, 3 }))
                .Build();

        private static AzureCSEmailProvider CreateProvider(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            var accessKey = "YWNjZXNza2V5"; // base64 for "accesskey"
            return new AzureCSEmailProvider(
                new AzureCSOptions
                {
                    Endpoint = "https://resource.communication.azure.com",
                    AccessKey = accessKey,
                    SenderAddress = "noreply@contoso.com"
                },
                new HttpClient(new StubHttpMessageHandler(handler)));
        }

        [Fact]
        public async Task SendAsync_Success_ParsesMessageIdAndBuildsSignedRequest()
        {
            HttpRequestMessage? captured = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                captured = request;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.EndsWith("/emails:send?api-version=2023-03-31", request.RequestUri!.ToString());

                Assert.NotNull(request.Headers.GetValues("x-ms-date"));
                Assert.NotNull(request.Headers.GetValues("x-ms-content-sha256"));
                var auth = Assert.Single(request.Headers.GetValues("Authorization"));
                Assert.StartsWith("HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature=", auth);
                Assert.NotNull(request.Content!.Headers.ContentType);
                Assert.Equal("application/json", request.Content.Headers.ContentType.ToString());

                var json = JsonDocument.Parse(await request.Content.ReadAsStringAsync());
                Assert.Equal("noreply@contoso.com", json.RootElement.GetProperty("senderAddress").GetString());
                Assert.Equal("Test", json.RootElement.GetProperty("content").GetProperty("subject").GetString());
                Assert.Equal("user@example.com", json.RootElement.GetProperty("recipients").GetProperty("to")[0].GetProperty("address").GetString());
                Assert.NotNull(json.RootElement.GetProperty("attachments")[0].GetProperty("contentInBase64").GetString());

                return new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent("{\"id\":\"operation-123\",\"status\":\"Running\"}")
                };
            });

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("operation-123", result.ProviderMessageId);
            Assert.NotNull(captured);
        }

        [Fact]
        public async Task SendAsync_ApiError_ThrowsEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"error\":{\"code\":\"AuthenticationFailed\",\"message\":\"invalid access key\"}}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.False(exception.IsTransient);
            Assert.Contains("invalid access key", exception.Message);
        }

        [Fact]
        public async Task SendAsync_RateLimit_ThrowsTransientEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("{\"error\":{\"code\":\"TooManyRequests\",\"message\":\"throttled\"}}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.True(exception.IsTransient);
        }

        [Fact]
        public void Constructor_RequiresEndpointKeyAndSender()
        {
            Assert.Throws<ArgumentException>(() => new AzureCSEmailProvider(new AzureCSOptions()));
            Assert.Throws<ArgumentException>(() => new AzureCSEmailProvider(new AzureCSOptions { Endpoint = "https://x", AccessKey = "YQ==" }));
            Assert.Throws<ArgumentException>(() => new AzureCSEmailProvider(new AzureCSOptions
            {
                Endpoint = "https://x",
                AccessKey = "YQ=="
            }));
            Assert.Throws<ArgumentException>(() => new AzureCSEmailProvider(new AzureCSOptions
            {
                Endpoint = "https://x",
                SenderAddress = "noreply@contoso.com"
            }));
        }
    }
}