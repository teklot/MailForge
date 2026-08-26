using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Postmark;

namespace MailForge.Tests
{
    public class PostmarkEmailProviderTests
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

        private static PostmarkEmailProvider CreateProvider(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            new PostmarkEmailProvider(
                new PostmarkOptions { ServerToken = "test-key" },
                new HttpClient(new StubHttpMessageHandler(handler)));

        [Fact]
        public async Task SendAsync_Success_ParsesMessageIdAndPostsCorrectPayload()
        {
            HttpRequestMessage? captured = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                captured = request;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.EndsWith("email", request.RequestUri!.ToString());
                Assert.Equal("test-key", request.Headers.GetValues("X-Postmark-Server-Token").Single());

                var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                Assert.Equal("Sender <sender@example.com>", json.RootElement.GetProperty("From").GetString());
                Assert.Equal("user@example.com", json.RootElement.GetProperty("To").GetString());
                Assert.Equal("Test", json.RootElement.GetProperty("Subject").GetString());
                Assert.Equal("<p>Hi</p>", json.RootElement.GetProperty("HtmlBody").GetString());
                Assert.Equal("welcome", json.RootElement.GetProperty("Metadata").GetProperty("purpose").GetString());
                Assert.NotNull(json.RootElement.GetProperty("Attachments")[0].GetProperty("Content").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"MessageID\":\"pm-123\",\"ErrorCode\":0}")
                };
            });

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("pm-123", result.ProviderMessageId);
            Assert.NotNull(captured);
        }

        [Fact]
        public async Task SendAsync_ApiError_ThrowsEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"ErrorCode\":10,\"Message\":\"Invalid token\"}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.False(exception.IsTransient);
            Assert.Contains("Invalid token", exception.Message);
        }

        [Fact]
        public async Task SendAsync_RateLimit_ThrowsTransientEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("{\"Message\":\"rate limit exceeded\"}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.True(exception.IsTransient);
        }

        [Fact]
        public void Constructor_RequiresServerToken()
        {
            Assert.Throws<ArgumentException>(() => new PostmarkEmailProvider(new PostmarkOptions()));
        }
    }
}
