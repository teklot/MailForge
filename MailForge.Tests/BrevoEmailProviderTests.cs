using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Brevo;

namespace MailForge.Tests
{
    public class BrevoEmailProviderTests
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
                .Tag("purpose", "welcome")
                .Attachment(new EmailAttachment("file.txt", new byte[] { 1, 2, 3 }))
                .Build();

        private static BrevoEmailProvider CreateProvider(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            new BrevoEmailProvider(
                new BrevoOptions { ApiKey = "test-key" },
                new HttpClient(new StubHttpMessageHandler(handler)));

        [Fact]
        public async Task SendAsync_Success_ParsesMessageIdAndPostsCorrectPayload()
        {
            HttpRequestMessage? captured = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                captured = request;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.EndsWith("v3/smtp/email", request.RequestUri!.ToString());
                Assert.Equal("test-key", request.Headers.GetValues("api-key").Single());

                var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                Assert.Equal("sender@example.com", json.RootElement.GetProperty("sender").GetProperty("email").GetString());
                Assert.Equal("Sender", json.RootElement.GetProperty("sender").GetProperty("name").GetString());
                Assert.Equal("user@example.com", json.RootElement.GetProperty("to")[0].GetProperty("email").GetString());
                Assert.Equal("Test", json.RootElement.GetProperty("subject").GetString());
                Assert.Equal("<p>Hi</p>", json.RootElement.GetProperty("htmlContent").GetString());
                Assert.NotNull(json.RootElement.GetProperty("attachment")[0].GetProperty("content").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"messageId\":\"<brevo-123>\"}")
                };
            });

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("<brevo-123>", result.ProviderMessageId);
            Assert.NotNull(captured);
        }

        [Fact]
        public async Task SendAsync_ApiError_ThrowsEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"message\":\"unauthorized\"}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.False(exception.IsTransient);
            Assert.Contains("unauthorized", exception.Message);
        }

        [Fact]
        public async Task SendAsync_RateLimit_ThrowsTransientEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("{\"message\":\"too many requests\"}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.True(exception.IsTransient);
        }

        [Fact]
        public void Constructor_RequiresApiKey()
        {
            Assert.Throws<ArgumentException>(() => new BrevoEmailProvider(new BrevoOptions()));
        }
    }
}
