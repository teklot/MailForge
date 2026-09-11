using System.Threading.Tasks;
using MailForge.Builders;
using MailForge.Extensions;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace MailForge.Tests
{
    public class MailForgeBuilderTests
    {
        [Fact]
        public void UseProvider_InstanceOverload_SetsProvider()
        {
            var provider = new FakeEmailProvider();
            var builder = new MailForgeBuilder();

            var result = builder.UseProvider(provider);

            Assert.Same(builder, result);
        }

        [Fact]
        public async Task AddMailForge_ProviderFactory_ResolvesProviderFromServiceProvider()
        {
            var services = new ServiceCollection();
            FakeEmailProvider? resolved = null;
            services.AddMailForge(builder => builder
                .UseDefaultFrom("noreply@example.com")
                .UseProvider(sp =>
                {
                    resolved = new FakeEmailProvider();
                    return resolved;
                }));

            await using var provider = services.BuildServiceProvider();

            var sender = provider.GetRequiredService<IEmailSender>();
            Assert.IsType<FakeEmailProvider>(provider.GetRequiredService<IEmailProvider>());

            var result = await sender.SendAsync(
                EmailMessage.Create().From("noreply@example.com").To("a@example.com").Subject("Hi").Text("Hi").Build(),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.NotNull(resolved);
            Assert.Single(resolved!.SentMessages);
        }

        [Fact]
        public async Task AddMailForge_ProviderFactory_IsInvokedOncePerMessagePipelineBuild()
        {
            var services = new ServiceCollection();
            var calls = 0;
            services.AddMailForge(builder => builder
                .UseDefaultFrom("noreply@example.com")
                .UseProvider(_ =>
                {
                    calls++;
                    return new FakeEmailProvider();
                }));

            await using var provider = services.BuildServiceProvider();
            var sender = provider.GetRequiredService<IEmailSender>();

            await sender.SendAsync(
                EmailMessage.Create().From("noreply@example.com").To("a@example.com").Subject("Hi").Text("Hi").Build(),
                TestContext.Current.CancellationToken);
            await sender.SendAsync(
                EmailMessage.Create().From("noreply@example.com").To("a@example.com").Subject("Hi2").Text("Hi2").Build(),
                TestContext.Current.CancellationToken);

            Assert.Equal(1, calls);
        }
    }
}