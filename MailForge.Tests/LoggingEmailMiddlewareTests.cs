using MailForge.Interfaces;
using MailForge.Middleware;
using MailForge.Models;
using Microsoft.Extensions.Logging;

namespace MailForge.Tests
{
    public class LoggingEmailMiddlewareTests
    {
        private sealed class TestLogger : ILogger<LoggingEmailMiddleware>
        {
            public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                Entries.Add((logLevel, formatter(state, exception), exception));
            }
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
        public async Task LogsStartAndOutcomeOnSuccess()
        {
            var logger = new TestLogger();
            var middleware = new LoggingEmailMiddleware(logger);
            var context = CreateContext();

            var result = await middleware.InvokeAsync(context, () =>
                Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success())));

            Assert.True(result.Succeeded);
            Assert.Equal(2, logger.Entries.Count);
            Assert.Equal(LogLevel.Information, logger.Entries[0].Level);
            Assert.Contains("Sending email", logger.Entries[0].Message);
            Assert.Equal(LogLevel.Information, logger.Entries[1].Level);
            Assert.Contains("delivery finished", logger.Entries[1].Message);
        }

        [Fact]
        public async Task LogsErrorOnException()
        {
            var logger = new TestLogger();
            var middleware = new LoggingEmailMiddleware(logger);
            var context = CreateContext();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                middleware.InvokeAsync(context, () => throw new InvalidOperationException("boom")));

            Assert.Equal(2, logger.Entries.Count);
            Assert.Equal(LogLevel.Information, logger.Entries[0].Level);
            Assert.Equal(LogLevel.Error, logger.Entries[1].Level);
            Assert.Contains("delivery failed", logger.Entries[1].Message);
            Assert.NotNull(logger.Entries[1].Exception);
        }

        [Fact]
        public async Task PassesThroughResultFromNext()
        {
            var logger = new TestLogger();
            var middleware = new LoggingEmailMiddleware(logger);
            var context = CreateContext();
            var expected = EmailDeliveryResult.Failed(context.Message, ProviderDeliveryResult.Failure("nope"));

            var result = await middleware.InvokeAsync(context, () => Task.FromResult(expected));

            Assert.Same(expected, result);
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task ReThrowsExceptionFromNext()
        {
            var logger = new TestLogger();
            var middleware = new LoggingEmailMiddleware(logger);
            var context = CreateContext();

            await Assert.ThrowsAsync<EmailException>(() =>
                middleware.InvokeAsync(context, () => throw new EmailException("perm", isTransient: false)));

            Assert.Equal(2, logger.Entries.Count);
        }

        [Fact]
        public async Task WorksWhenLoggerIsNull()
        {
            var middleware = new LoggingEmailMiddleware(null);
            var context = CreateContext();

            var result = await middleware.InvokeAsync(context, () =>
                Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success())));

            Assert.True(result.Succeeded);
        }
    }
}
