using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Resend;

namespace MailForge.Tests
{
    public class ResendEmailProviderTests
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

        private static ResendEmailProvider CreateProvider(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            new ResendEmailProvider(
                new ResendOptions { ApiKey = "test-key" },
                new HttpClient(new StubHttpMessageHandler(handler)));

        [Fact]
        public async Task SendAsync_Success_ParsesMessageIdAndPostsCorrectPayload()
        {
            HttpRequestMessage? captured = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                captured = request;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.EndsWith("emails", request.RequestUri!.ToString());
                Assert.Equal("Bearer test-key", request.Headers.Authorization!.ToString());

                var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                Assert.Equal("Sender <sender@example.com>", json.RootElement.GetProperty("from").GetString());
                Assert.Equal("Test", json.RootElement.GetProperty("subject").GetString());
                Assert.Equal("welcome", json.RootElement.GetProperty("tags")[0].GetProperty("value").GetString());
                Assert.NotNull(json.RootElement.GetProperty("attachments")[0].GetProperty("content").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"resend-123\"}")
                };
            });

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("resend-123", result.ProviderMessageId);
            Assert.NotNull(captured);
        }

        [Fact]
        public async Task SendAsync_ApiError_ThrowsEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"statusCode\":401,\"name\":\"authentication_error\",\"message\":\"invalid key\"}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.False(exception.IsTransient);
            Assert.Contains("invalid key", exception.Message);
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
            Assert.Throws<ArgumentException>(() => new ResendEmailProvider(new ResendOptions()));
        }
    }
}
