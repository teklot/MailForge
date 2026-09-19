using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailForge.Models;
using MailForge.Studio.Capture;
using MailForge.Studio.Capture.Entities;
using MailForge.Studio.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailForge.Tests
{
    public class StudioWebHostTests
    {
        [Fact]
        public async Task IndexPage_ReturnsHtmlWithCapturedMessages()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                await store.CaptureAsync(WelcomeMessage("alice@example.com"), cancellationToken: TestContext.Current.CancellationToken);

                await using var app = StudioWebHost.Build(Options(databasePath), b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);
                var client = app.GetTestClient();

var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
                var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("MailForge Studio", html);
                Assert.Contains("Welcome to Acme", html);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task MessagesApi_List_ReturnsPaginatedJson()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                await store.CaptureAsync(WelcomeMessage("alice@example.com"), cancellationToken: TestContext.Current.CancellationToken);
                await store.CaptureAsync(WelcomeMessage("bob@example.com"), cancellationToken: TestContext.Current.CancellationToken);

                await using var app = StudioWebHost.Build(Options(databasePath), b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);
                var client = app.GetTestClient();

                var response = await client.GetAsync("/api/messages/?page=1&pageSize=1", TestContext.Current.CancellationToken);
                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(1, json.RootElement.GetProperty("items").GetArrayLength());
                Assert.Equal(2, json.RootElement.GetProperty("totalCount").GetInt32());
                Assert.Equal(2, json.RootElement.GetProperty("totalPages").GetInt32());
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task MessagesApi_Get_ReturnsMessageDetail()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var captured = await store.CaptureAsync(WelcomeMessage("alice@example.com"), cancellationToken: TestContext.Current.CancellationToken);

                await using var app = StudioWebHost.Build(Options(databasePath), b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);
                var client = app.GetTestClient();

                var response = await client.GetAsync($"/api/messages/{captured.Id}", TestContext.Current.CancellationToken);
                var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("Welcome to Acme", json.RootElement.GetProperty("subject").GetString());
                Assert.Equal(1, json.RootElement.GetProperty("recipients").GetArrayLength());

                var missing = await client.GetAsync($"/api/messages/{System.Guid.NewGuid()}", TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task MessagesApi_Export_ReturnsEml()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var captured = await store.CaptureAsync(WelcomeMessage("alice@example.com"), cancellationToken: TestContext.Current.CancellationToken);

                await using var app = StudioWebHost.Build(Options(databasePath), b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);
                var client = app.GetTestClient();

                var response = await client.GetAsync($"/api/messages/{captured.Id}/export?format=eml", TestContext.Current.CancellationToken);
                var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("message/rfc822", response.Content.Headers.ContentType?.MediaType);
                Assert.Contains("Subject: Welcome to Acme", body);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task MessagesApi_Raw_ReturnsRegeneratedMime()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var captured = await store.CaptureAsync(
                    WelcomeMessage("alice@example.com"),
                    rawMime: Encoding.UTF8.GetBytes("From: sender@example.com\r\nSubject: Welcome to Acme\r\n\r\nHi"),
                    cancellationToken: TestContext.Current.CancellationToken);

                await using var app = StudioWebHost.Build(Options(databasePath), b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);
                var client = app.GetTestClient();

                var response = await client.GetAsync($"/api/messages/{captured.Id}/raw", TestContext.Current.CancellationToken);
                var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("message/rfc822", response.Content.Headers.ContentType?.MediaType);
                Assert.Contains("Welcome to Acme", body);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task MessagesApi_Attachment_ReturnsContentBytes()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var message = EmailMessage.Create()
                    .From("sender@example.com")
                    .To("alice@example.com")
                    .Subject("Files")
                    .Attachment(new EmailAttachment("a.txt", new byte[] { 1, 2, 3 }, "text/plain"))
                    .Build();
                var captured = await store.CaptureAsync(message, cancellationToken: TestContext.Current.CancellationToken);
                var attachmentId = captured.Attachments.First().Id;

                await using var app = StudioWebHost.Build(Options(databasePath), b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);
                var client = app.GetTestClient();

                var response = await client.GetAsync($"/api/messages/{captured.Id}/attachments/{attachmentId}", TestContext.Current.CancellationToken);
                var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);
                Assert.Equal(new byte[] { 1, 2, 3 }, bytes);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task MessagesApi_Replay_DeliversToRelay()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var captured = await store.CaptureAsync(WelcomeMessage("alice@example.com"), cancellationToken: TestContext.Current.CancellationToken);

                await using var relay = new SmtpRelayServer(
                    store,
                    new StudioCaptureOptions { SmtpRelayHost = "127.0.0.1", SmtpRelayPort = 0 });
                await relay.StartAsync(TestContext.Current.CancellationToken);

                var options = new StudioWebOptions
                {
                    DatabasePath = databasePath,
                    WebPort = 5000,
                    SmtpHost = "127.0.0.1",
                    SmtpPort = relay.Port,
                };

                await using var app = StudioWebHost.Build(options, b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);
                var client = app.GetTestClient();

                var response = await client.PostAsync($"/api/messages/{captured.Id}/replay", content: null, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
                using var json = JsonDocument.Parse(body);
                Assert.True(json.RootElement.GetProperty("succeeded").GetBoolean(), body);

                var after = await store.ListAsync(cancellationToken: TestContext.Current.CancellationToken);
                Assert.Equal(2, after.Count);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        [Fact]
        public async Task Host_Relay_CapturesMailVisibleViaApi()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                await using var app = StudioWebHost.Build(new StudioWebOptions
                {
                    DatabasePath = databasePath,
                    WebPort = 5000,
                    EnableSmtpRelay = true,
                    SmtpRelayPort = 0,
                    SmtpHost = "127.0.0.1",
                    SmtpPort = 25,
                }, b => b.WebHost.UseTestServer());
                await app.StartAsync(TestContext.Current.CancellationToken);

                var relay = app.Services.GetRequiredService<SmtpRelayServer>();
                Assert.False(relay.IsRunning); // Build/TestServer must not auto-start the relay
                await relay.StartAsync(TestContext.Current.CancellationToken);
                try
                {
                    using var client = new SmtpClient();
                    await client.ConnectAsync("127.0.0.1", relay.Port, SecureSocketOptions.None, TestContext.Current.CancellationToken);
                    var mime = new MimeMessage();
                    mime.From.Add(new MailboxAddress("Sender", "sender@example.com"));
                    mime.To.Add(new MailboxAddress("A", "a@example.com"));
                    mime.Subject = "Hello live";
                    mime.Body = new BodyBuilder { TextBody = "Body" }.ToMessageBody();
                    await client.SendAsync(mime, TestContext.Current.CancellationToken);
                    await client.DisconnectAsync(quit: true, TestContext.Current.CancellationToken);

                    var http = app.GetTestClient();
                    var response = await http.GetAsync("/api/messages/?page=1&pageSize=25", TestContext.Current.CancellationToken);
                    var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.True(json.RootElement.GetProperty("totalCount").GetInt32() >= 1);
                    Assert.Equal(
                        1,
                        json.RootElement.GetProperty("items").EnumerateArray().Count(i =>
                            i.GetProperty("subject").GetString() == "Hello live"));
                }
                finally
                {
                    await relay.StopAsync(TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        private static StudioWebOptions Options(string databasePath) => new()
        {
            DatabasePath = databasePath,
            WebPort = 5000,
        };

        private static EmailMessage WelcomeMessage(string to)
        {
            return EmailMessage.Create()
                .From("sender@example.com", "Sender")
                .To(to, "Alice")
                .Subject("Welcome to Acme")
                .Html("<h1>Welcome</h1>")
                .Text("Welcome to Acme!")
                .Build();
        }
    }
}