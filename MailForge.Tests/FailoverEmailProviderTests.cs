using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Builders;
using MailForge.Interfaces;
using MailForge.Models;

namespace MailForge.Tests
{
    public class FailoverEmailProviderTests
    {
        private static EmailMessage CreateMessage() =>
            EmailMessage.Create().From("a@example.com").To("b@example.com").Subject("s").Text("t").Build();

        [Fact]
        public async Task SendsThroughFirstProviderWhenItSucceeds()
        {
            var primary = new StubProvider("Primary").ThenSucceed();
            var backup = new StubProvider("Backup").ThenSucceed();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary),
                new ProviderFailoverRoute(backup));

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(1, primary.Calls);
            Assert.Equal(0, backup.Calls);
        }

        [Fact]
        public async Task FallsBackToNextProviderOnTransientFailure()
        {
            var primary = new StubProvider("Primary").ThenFailTransient();
            var backup = new StubProvider("Backup").ThenSucceed("backup-id");
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary),
                new ProviderFailoverRoute(backup));

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal("backup-id", result.ProviderMessageId);
            Assert.Equal(1, primary.Calls);
            Assert.Equal(1, backup.Calls);
        }

        [Fact]
        public async Task TransientOnlyPolicyDoesNotFailOverOnPermanentFailure()
        {
            var primary = new StubProvider("Primary").ThenFailPermanent();
            var backup = new StubProvider("Backup").ThenSucceed();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary),
                new ProviderFailoverRoute(backup));

            await Assert.ThrowsAsync<EmailException>(() => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));

            Assert.Equal(1, primary.Calls);
            Assert.Equal(0, backup.Calls);
        }

        [Fact]
        public async Task AnyFailurePolicyFailsOverOnPermanentFailure()
        {
            var primary = new StubProvider("Primary").ThenFailPermanent();
            var backup = new StubProvider("Backup").ThenSucceed();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary, FailoverPolicy.AnyFailure),
                new ProviderFailoverRoute(backup));

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(1, primary.Calls);
            Assert.Equal(1, backup.Calls);
        }

        [Fact]
        public async Task TransientOnlyPolicyDoesNotFailOverOnRejectedResult()
        {
            var primary = new StubProvider("Primary").ThenReject();
            var backup = new StubProvider("Backup").ThenSucceed();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary),
                new ProviderFailoverRoute(backup));

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(1, primary.Calls);
            Assert.Equal(0, backup.Calls);
        }

        [Fact]
        public async Task AnyFailurePolicyFailsOverOnRejectedResult()
        {
            var primary = new StubProvider("Primary").ThenReject();
            var backup = new StubProvider("Backup").ThenSucceed();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary, FailoverPolicy.AnyFailure),
                new ProviderFailoverRoute(backup));

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(1, primary.Calls);
            Assert.Equal(1, backup.Calls);
        }

        [Fact]
        public async Task RetriesTransientFailuresOnSameProviderUpToMaxAttemptsBeforeFailingOver()
        {
            var primary = new StubProvider("Primary").ThenFailTransient();
            var backup = new StubProvider("Backup").ThenSucceed();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary, FailoverPolicy.TransientOnly, maxAttempts: 2),
                new ProviderFailoverRoute(backup));

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(2, primary.Calls);
            Assert.Equal(1, backup.Calls);
        }

        [Fact]
        public async Task StopsAtFirstSuccessfulProvider()
        {
            var first = new StubProvider("First").ThenFailTransient();
            var second = new StubProvider("Second").ThenSucceed();
            var third = new StubProvider("Third").ThenSucceed();
            var provider = new FailoverEmailProvider(first, second, third);

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Equal(new[] { "First", "Second" }, first.AttemptOrder.Concat(second.AttemptOrder).Concat(third.AttemptOrder));
            Assert.Equal(0, third.Calls);
        }

        [Fact]
        public async Task SuccessfulResultIdentifiesTheDeliveringProvider()
        {
            var primary = new StubProvider("Primary").ThenFailTransient();
            var backup = new StubProvider("Backup").ThenSucceed();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary),
                new ProviderFailoverRoute(backup));

            var result = await provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken);

            Assert.Contains("Backup", result.Details);
        }

        [Fact]
        public async Task ThrowsTransientExceptionWhenAllProvidersFailTransiently()
        {
            var primary = new StubProvider("Primary").ThenFailTransient();
            var backup = new StubProvider("Backup").ThenFailTransient();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary),
                new ProviderFailoverRoute(backup));

            var exception = await Assert.ThrowsAsync<EmailException>(() => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Contains("Primary", exception.Message);
            Assert.Contains("Backup", exception.Message);
        }

        [Fact]
        public async Task ThrowsNonTransientExceptionWhenAllProvidersReject()
        {
            var primary = new StubProvider("Primary").ThenReject();
            var backup = new StubProvider("Backup").ThenReject();
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(primary, FailoverPolicy.AnyFailure),
                new ProviderFailoverRoute(backup, FailoverPolicy.AnyFailure));

            var exception = await Assert.ThrowsAsync<EmailException>(() => provider.SendAsync(CreateMessage(), TestContext.Current.CancellationToken));

            Assert.False(exception.IsTransient);
        }

        [Fact]
        public async Task CancellationIsPropagatedBeforeInvokingProviders()
        {
            var primary = new StubProvider("Primary").ThenSucceed();
            var provider = new FailoverEmailProvider(primary);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => provider.SendAsync(CreateMessage(), cts.Token));

            Assert.Equal(0, primary.Calls);
        }

        [Fact]
        public void CapabilitiesAreTheIntersectionOfInnerProviders()
        {
            var limited = new StubProvider("Limited").WithCapabilities(ProviderCapabilities.BodyOnly);
            var full = new StubProvider("Full");
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(limited),
                new ProviderFailoverRoute(full));

            Assert.False(provider.Capabilities.SupportsAttachments);
            Assert.False(provider.Capabilities.SupportsInlineImages);
            Assert.False(provider.Capabilities.SupportsHeaders);
            Assert.False(provider.Capabilities.SupportsTags);
            Assert.False(provider.Capabilities.RequiresBothBodies);
        }

        [Fact]
        public void RequiresBothBodiesIsMostRestrictiveAcrossProviders()
        {
            var dual = new StubProvider("Dual").WithCapabilities(new ProviderCapabilities(requiresBothBodies: true));
            var full = new StubProvider("Full");
            var provider = new FailoverEmailProvider(
                new ProviderFailoverRoute(dual),
                new ProviderFailoverRoute(full));

            Assert.True(provider.Capabilities.RequiresBothBodies);
            Assert.True(provider.Capabilities.SupportsAttachments);
        }

        [Fact]
        public void ConstructorRequiresAtLeastOneRoute()
        {
            Assert.Throws<ArgumentException>(() => new FailoverEmailProvider(Array.Empty<ProviderFailoverRoute>()));
            Assert.Throws<ArgumentException>(() => new FailoverEmailProvider(Array.Empty<IEmailProvider>()));
        }

        [Fact]
        public void ConstructorThrowsOnNullProviders()
        {
            Assert.Throws<ArgumentNullException>(() => new FailoverEmailProvider((IEmailProvider[])null!));
            Assert.Throws<ArgumentNullException>(() => new FailoverEmailProvider((IEnumerable<ProviderFailoverRoute>)null!));
        }

        [Fact]
        public void RouteNormalizesAttemptsToAtLeastOne()
        {
            var route = new ProviderFailoverRoute(new StubProvider("A"), FailoverPolicy.TransientOnly, maxAttempts: 0);
            Assert.Equal(1, route.MaxAttempts);
        }

        private sealed class StubProvider : IEmailProvider
        {
            private readonly List<Func<EmailMessage, ProviderDeliveryResult>> _behaviors =
                new List<Func<EmailMessage, ProviderDeliveryResult>>();

            public StubProvider(string name)
            {
                Name = name;
            }

            public string Name { get; }
            public ProviderCapabilities Capabilities { get; private set; } = ProviderCapabilities.All;
            public int Calls { get; private set; }
            public List<string> AttemptOrder { get; } = new List<string>();

            public StubProvider WithCapabilities(ProviderCapabilities capabilities)
            {
                Capabilities = capabilities;
                return this;
            }

            public StubProvider ThenSucceed(string messageId = "ok")
            {
                _behaviors.Add(_ => ProviderDeliveryResult.Success(messageId, "delivered"));
                return this;
            }

            public StubProvider ThenFailTransient()
            {
                _behaviors.Add(_ => throw new EmailException("temporary outage", isTransient: true));
                return this;
            }

            public StubProvider ThenFailPermanent()
            {
                _behaviors.Add(_ => throw new EmailException("invalid credentials", isTransient: false));
                return this;
            }

            public StubProvider ThenReject()
            {
                _behaviors.Add(_ => ProviderDeliveryResult.Failure("message rejected"));
                return this;
            }

            public Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
            {
                var behavior = _behaviors.Count == 1 ? _behaviors[0] : _behaviors[Calls];
                Calls++;
                AttemptOrder.Add(Name);
                return Task.FromResult(behavior(message));
            }
        }
    }
}
