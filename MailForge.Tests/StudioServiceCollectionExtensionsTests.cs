using System.Linq;
using System.Threading.Tasks;
using MailForge.Extensions;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Studio.Capture;
using Microsoft.Extensions.DependencyInjection;

namespace MailForge.Tests
{
    public class StudioServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddMailForgeStudio_NullConfiguration_Throws()
        {
            var services = new ServiceCollection();
            Assert.Throws<System.ArgumentNullException>(() => services.AddMailForgeStudio(null!));
        }

        [Fact]
        public async Task AddMailForgeStudio_WiresStoreProviderAndRelay()
        {
            var databasePath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"mailforge-studio-di-test-{System.Guid.NewGuid():N}.db");
            try
            {
                var services = new ServiceCollection();
                services.AddMailForgeStudio(options =>
                {
                    options.DatabasePath = databasePath;
                    options.EnableSmtpRelay = true;
                    options.SmtpRelayPort = 0;
                });
                services.AddMailForge(builder => builder
                    .UseDefaultFrom("noreply@example.com")
                    .UseProvider(sp => sp.GetRequiredService<StudioEmailProvider>()));

                await using var provider = services.BuildServiceProvider();

                var emailProvider = provider.GetRequiredService<IEmailProvider>();
                Assert.IsType<StudioEmailProvider>(emailProvider);

                var relay = provider.GetRequiredService<SmtpRelayServer>();
                Assert.NotNull(provider.GetRequiredService<System.Collections.Generic.IEnumerable<Microsoft.Extensions.Hosting.IHostedService>>());

                var store = provider.GetRequiredService<IStudioCaptureStore>();
                var sender = provider.GetRequiredService<IEmailSender>();

                await relay.StartAsync(TestContext.Current.CancellationToken);
                try
                {
                    var result = await sender.SendAsync(
                        EmailMessage.Create().From("noreply@example.com").To("a@example.com").Subject("Studio DI").Text("Body").Build(),
                        TestContext.Current.CancellationToken);

                    Assert.True(result.Succeeded);
                    var captured = await store.ListAsync(cancellationToken: TestContext.Current.CancellationToken);
                    var message = Assert.Single(captured);
                    Assert.Equal("Studio DI", message.Subject);
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
        public async Task AddMailForgeStudio_RelayDisabled_StillResolvesStoreAndProvider()
        {
            var databasePath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"mailforge-studio-di-test-{System.Guid.NewGuid():N}.db");
            try
            {
                var services = new ServiceCollection();
                services.AddMailForgeStudio(options =>
                {
                    options.DatabasePath = databasePath;
                    options.EnableSmtpRelay = false;
                });
                services.AddMailForge(builder => builder
                    .UseProvider(sp => sp.GetRequiredService<StudioEmailProvider>()));

                await using var provider = services.BuildServiceProvider();

                Assert.IsType<StudioEmailProvider>(provider.GetRequiredService<IEmailProvider>());
                Assert.NotNull(provider.GetRequiredService<IStudioCaptureStore>());
                Assert.Empty(provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>());
            }
            finally
            {
                StudioTestSupport.CleanupDatabase(databasePath);
            }
        }
    }
}