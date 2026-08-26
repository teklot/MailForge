using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.ZeptoMail;

namespace MailForge.Tests
{
    public class ZeptoMailEmailProviderTests
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

        private static ZeptoMailEmailProvider CreateProvider(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            new ZeptoMailEmailProvider(
                new ZeptoMailOptions { SendApiKey = "test-key" },
                new HttpClient(new StubHttpMessageHandler(handler)));

        [Fact]
        public async Task SendAsync_Success_ParsesMessageIdAndPostsCorrectPayload()
        {
            HttpRequestMessage? captured = null;

            var provider = CreateProvider(async (request, ct) =>
            {
                captured = request;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.EndsWith("v1.1/email", request.RequestUri!.ToString());
                Assert.Equal("Zoho-enczapikey test-key", request.Headers.Authorization!.ToString());

                var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                Assert.Equal("Sender <sender@example.com>", json.RootElement.GetProperty("from").GetProperty("address").GetString());
                Assert.Equal("Sender", json.RootElement.GetProperty("from").GetProperty("name").GetString());
                Assert.Equal("user@example.com", json.RootElement.GetProperty("to")[0].GetProperty("email_address").GetProperty("address").GetString());
                Assert.Equal("Test", json.RootElement.GetProperty("subject").GetString());
                Assert.Equal("<p>Hi</p>", json.RootElement.GetProperty("htmlbody").GetString());
                Assert.NotNull(json.RootElement.GetProperty("attachments")[0].GetProperty("file").GetString());

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"data\":[{\"message\":\"zepto-123\",\"status\":\"success\"}]}")
                };
            });

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("zepto-123", result.ProviderMessageId);
            Assert.NotNull(captured);
        }

        [Fact]
        public async Task SendAsync_ApiError_ThrowsEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent("{\"message\":\"invalid token\"}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.False(exception.IsTransient);
            Assert.Contains("invalid token", exception.Message);
        }

        [Fact]
        public async Task SendAsync_RateLimit_ThrowsTransientEmailException()
        {
            var provider = CreateProvider((request, ct) =>
                Task.FromResult(new HttpResponseMessage((HttpStatusCode)429)
                {
                    Content = new StringContent("{\"message\":\"rate limit exceeded\"}")
                }));

            var exception = await Assert.ThrowsAsync<EmailException>(
                () => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));
            Assert.True(exception.IsTransient);
        }

        [Fact]
        public void Constructor_RequiresSendApiKey()
        {
            Assert.Throws<ArgumentException>(() => new ZeptoMailEmailProvider(new ZeptoMailOptions()));
        }
    }
}
