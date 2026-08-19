using MailForge.Interfaces;
using MailForge.Middleware;
using MailForge.Models;

namespace MailForge.Tests
{
    public class AuditEmailMiddlewareTests
    {
        private sealed class TestAuditSink : IEmailAuditSink
        {
            public List<EmailAuditRecord> Records { get; } = new();

            public Task RecordAsync(EmailAuditRecord record, CancellationToken cancellationToken = default)
            {
                Records.Add(record);
                return Task.CompletedTask;
            }
        }

        private sealed class FailingAuditSink : IEmailAuditSink
        {
            public Task RecordAsync(EmailAuditRecord record, CancellationToken cancellationToken = default)
                => throw new InvalidOperationException("sink failure");
        }

        private static EmailDeliveryContext CreateContext()
        {
            var message = EmailMessage.Create()
                .From("a@example.com")
                .To("b@example.com")
                .Subject("s")
                .Text("t")
                .Build();
            return new EmailDeliveryContext(message);
        }

        [Fact]
        public async Task RecordsAuditOnSuccess()
        {
            var sink = new TestAuditSink();
            var middleware = new AuditEmailMiddleware(sink, "Smtp");
            var context = CreateContext();

            var result = await middleware.InvokeAsync(context, () =>
                Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success("pm-123"))));

            Assert.True(result.Succeeded);
            Assert.Single(sink.Records);
            Assert.Equal(EmailDeliveryStatus.Sent, sink.Records[0].Status);
            Assert.Equal("Smtp", sink.Records[0].ProviderName);
            Assert.Equal("pm-123", sink.Records[0].ProviderMessageId);
            Assert.Equal(context.Message.MessageId, sink.Records[0].MessageId);
        }

        [Fact]
        public async Task RecordsAuditOnEmailException()
        {
            var sink = new TestAuditSink();
            var middleware = new AuditEmailMiddleware(sink, "Resend");
            var context = CreateContext();

            await Assert.ThrowsAsync<EmailException>(() =>
                middleware.InvokeAsync(context, () => throw new EmailException("perm", isTransient: false)));

            Assert.Single(sink.Records);
            Assert.Equal(EmailDeliveryStatus.Failed, sink.Records[0].Status);
            Assert.Equal("Resend", sink.Records[0].ProviderName);
        }

        [Fact]
        public async Task RecordsAuditOnGenericException()
        {
            var sink = new TestAuditSink();
            var middleware = new AuditEmailMiddleware(sink);
            var context = CreateContext();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                middleware.InvokeAsync(context, () => throw new InvalidOperationException("boom")));

            Assert.Single(sink.Records);
            Assert.Equal(EmailDeliveryStatus.Failed, sink.Records[0].Status);
            Assert.Equal("unknown", sink.Records[0].ProviderName);
        }

        [Fact]
        public async Task CopiesCustomItemsToRecordData()
        {
            var sink = new TestAuditSink();
            var middleware = new AuditEmailMiddleware(sink);
            var context = CreateContext();
            context.Items["custom-key"] = "custom-value";
            context.Items["MailForge.Attempt"] = 3;

            await middleware.InvokeAsync(context, () =>
                Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success())));

            Assert.Single(sink.Records);
            Assert.Equal("custom-value", sink.Records[0].Data["custom-key"]);
            Assert.False(sink.Records[0].Data.ContainsKey("MailForge.Attempt"));
            Assert.Equal(3, sink.Records[0].Attempt);
        }

        [Fact]
        public async Task SwallowsAuditSinkFailures()
        {
            var sink = new FailingAuditSink();
            var middleware = new AuditEmailMiddleware(sink);
            var context = CreateContext();

            var result = await middleware.InvokeAsync(context, () =>
                Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success())));

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task SwallowsSinkFailuresOnExceptionPath()
        {
            var sink = new FailingAuditSink();
            var middleware = new AuditEmailMiddleware(sink);
            var context = CreateContext();

            await Assert.ThrowsAsync<EmailException>(() =>
                middleware.InvokeAsync(context, () => throw new EmailException("fail", isTransient: true)));
        }

        [Fact]
        public void ThrowsWhenSinkIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AuditEmailMiddleware(null!));
        }

        [Fact]
        public async Task RecordsAttemptFromRetryMiddleware()
        {
            var sink = new TestAuditSink();
            var middleware = new AuditEmailMiddleware(sink);
            var context = CreateContext();
            context.Items["MailForge.Attempt"] = 2;

            await middleware.InvokeAsync(context, () =>
                Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success())));

            Assert.Equal(2, sink.Records[0].Attempt);
        }

        [Fact]
        public async Task DefaultsAttemptToOneWhenNotSet()
        {
            var sink = new TestAuditSink();
            var middleware = new AuditEmailMiddleware(sink);
            var context = CreateContext();

            await middleware.InvokeAsync(context, () =>
                Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success())));

            Assert.Equal(1, sink.Records[0].Attempt);
        }
    }
}
