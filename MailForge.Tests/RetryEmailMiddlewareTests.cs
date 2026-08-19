using MailForge.Interfaces;
using MailForge.Middleware;
using MailForge.Models;

namespace MailForge.Tests
{
    public class RetryEmailMiddlewareTests
    {
        private static EmailDeliveryContext CreateContext() =>
            new EmailDeliveryContext(
                EmailMessage.Create().From("a@example.com").To("b@example.com").Subject("s").Text("t").Build());

        [Fact]
        public async Task RetriesTransientFailuresUntilSuccess()
        {
            var middleware = new RetryEmailMiddleware(maxAttempts: 3, TimeSpan.FromMilliseconds(1));
            var context = CreateContext();
            var calls = 0;

            var result = await middleware.InvokeAsync(context, () =>
            {
                calls++;
                if (calls < 3)
                    throw new EmailException("temporary", isTransient: true);
                return Task.FromResult(EmailDeliveryResult.Sent(context.Message, ProviderDeliveryResult.Success()));
            });

            Assert.True(result.Succeeded);
            Assert.Equal(3, calls);
        }

        [Fact]
        public async Task DoesNotRetryNonTransientFailures()
        {
            var middleware = new RetryEmailMiddleware(maxAttempts: 3, TimeSpan.FromMilliseconds(1));
            var context = CreateContext();
            var calls = 0;

            await Assert.ThrowsAsync<EmailException>(() =>
                middleware.InvokeAsync(context, () =>
                {
                    calls++;
                    throw new EmailException("permanent", isTransient: false);
                }));

            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task ThrowsAfterExhaustingAttempts()
        {
            var middleware = new RetryEmailMiddleware(maxAttempts: 2, TimeSpan.FromMilliseconds(1));
            var context = CreateContext();
            var calls = 0;

            await Assert.ThrowsAsync<EmailException>(() =>
                middleware.InvokeAsync(context, () =>
                {
                    calls++;
                    throw new EmailException("temporary", isTransient: true);
                }));

            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task TracksAttemptNumberInContext()
        {
            var middleware = new RetryEmailMiddleware(maxAttempts: 3, TimeSpan.FromMilliseconds(1));
            var context = CreateContext();
            var lastAttempt = 0;

            await Assert.ThrowsAsync<EmailException>(() =>
                middleware.InvokeAsync(context, () =>
                {
                    lastAttempt = (int)context.Items["MailForge.Attempt"];
                    throw new EmailException("temporary", isTransient: true);
                }));

            Assert.Equal(3, lastAttempt);
        }
    }
}
