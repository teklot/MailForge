using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MailForge.Studio.Capture;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailForge.Tests
{
    public class SmtpRelayServerTests
    {
        [Fact]
        public async Task Relay_CapturesMessageSentByMailKitClient()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var relay = new SmtpRelayServer(store, new StudioCaptureOptions { SmtpRelayPort = 0 });

                await relay.StartAsync(TestContext.Current.CancellationToken);
                try
                {
                    Assert.True(relay.IsRunning);

                    using var client = new SmtpClient();
                    await client.ConnectAsync("127.0.0.1", relay.Port, SecureSocketOptions.None, TestContext.Current.CancellationToken);
                    await client.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);
                    await client.DisconnectAsync(quit: true, TestContext.Current.CancellationToken);

                    var captured = await store.ListAsync(cancellationToken: TestContext.Current.CancellationToken);
                    var message = Assert.Single(captured);
                    Assert.Equal("Hello from MailKit", message.Subject);
                    Assert.Equal("sender@example.com", message.From);
                    Assert.Equal("sender@example.com", message.EnvelopeFrom);
                    Assert.Equal("Body text", message.TextBody);
                    Assert.NotNull(message.RawMime);
                    Assert.True(message.RawMime!.Length > 0);
                    Assert.Single(message.Recipients, r => r.Address == "a@example.com");
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

        [Fact]
        public async Task Relay_HandlesRawSmtpSession()
        {
            var factory = StudioTestSupport.CreateTempContextFactory(out var databasePath);
            try
            {
                var store = new StudioCaptureStore(factory);
                var relay = new SmtpRelayServer(store, new StudioCaptureOptions { SmtpRelayPort = 0 });

                await relay.StartAsync(TestContext.Current.CancellationToken);
                try
                {
                    using var client = new TcpClient();
                    await client.ConnectAsync("127.0.0.1", relay.Port, TestContext.Current.CancellationToken);
                    using var stream = client.GetStream();
                    using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
                    using var writer = new StreamWriter(stream, new ASCIIEncoding(), 1024, leaveOpen: true);
                    writer.AutoFlush = true;

                    Assert.StartsWith("220", await reader.ReadLineAsync(TestContext.Current.CancellationToken));

                    await SendLineAsync(writer, "EHLO localhost", TestContext.Current.CancellationToken);
                    Assert.StartsWith("250", await reader.ReadLineAsync(TestContext.Current.CancellationToken));
                    Assert.StartsWith("250", await reader.ReadLineAsync(TestContext.Current.CancellationToken));

                    await SendLineAsync(writer, "MAIL FROM:<sender@example.com>", TestContext.Current.CancellationToken);
                    Assert.Equal("250 Ok", await reader.ReadLineAsync(TestContext.Current.CancellationToken));

                    await SendLineAsync(writer, "RCPT TO:<a@example.com>", TestContext.Current.CancellationToken);
                    Assert.Equal("250 Ok", await reader.ReadLineAsync(TestContext.Current.CancellationToken));

                    await SendLineAsync(writer, "DATA", TestContext.Current.CancellationToken);
                    Assert.StartsWith("354", await reader.ReadLineAsync(TestContext.Current.CancellationToken));

                    await writer.WriteAsync("From: Sender <sender@example.com>\r\nTo: <a@example.com>\r\nSubject: Raw test\r\n\r\nBody line\r\n.\r\n");
                    await writer.FlushAsync(TestContext.Current.CancellationToken);

                    var queued = await reader.ReadLineAsync(TestContext.Current.CancellationToken);
                    Assert.StartsWith("250 Ok: queued as ", queued);

                    await SendLineAsync(writer, "QUIT", TestContext.Current.CancellationToken);
                    Assert.Equal("221 Bye", await reader.ReadLineAsync(TestContext.Current.CancellationToken));

                    var captured = await store.ListAsync(cancellationToken: TestContext.Current.CancellationToken);
                    var message = Assert.Single(captured);
                    Assert.Equal("Raw test", message.Subject);
                    Assert.Equal("Body line", message.TextBody);
                    Assert.Equal("sender@example.com", message.EnvelopeFrom);
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

        [Fact]
        public void Port_ReflectsEphemeralPortAssignment()
        {
            var store = new StudioCaptureStore(StudioTestSupport.CreateTempContextFactory(out var databasePath));
            try
            {
                var relay = new SmtpRelayServer(store, new StudioCaptureOptions { SmtpRelayPort = 0 });
                Assert.False(relay.IsRunning);
                Assert.Equal(0, relay.Port);
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }

        private static MimeMessage CreateMessage()
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress("Sender", "sender@example.com"));
            mime.To.Add(new MailboxAddress("A", "a@example.com"));
            mime.Subject = "Hello from MailKit";
            mime.Body = new BodyBuilder { TextBody = "Body text" }.ToMessageBody();
            return mime;
        }

        private static async Task SendLineAsync(StreamWriter writer, string line, System.Threading.CancellationToken cancellationToken)
        {
            await writer.WriteAsync(line + "\r\n");
            await writer.FlushAsync(cancellationToken);
        }
    }
}