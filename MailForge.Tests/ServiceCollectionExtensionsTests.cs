using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace MailForge.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        private sealed class InviteEmail : Email<InviteModel>
        {
            public InviteEmail(InviteModel model) : base(model)
            {
                AddTo("guest@example.com");
                SetSubject("Invite: {{Name}}");
                SetText("Hello {{Name}}!");
            }
        }

        private sealed class InviteModel
        {
            public string Name { get; set; } = "Guest";
        }

        [Fact]
        public async Task AddMailForge_ResolvesSenderAndSendsThroughFakeProvider()
        {
            var services = new ServiceCollection();
            services.AddMailForge(builder => builder.UseDefaultFrom("noreply@example.com"));

            await using var provider = services.BuildServiceProvider();
            var sender = provider.GetRequiredService<IEmailSender>();
            var fake = Assert.IsType<FakeEmailProvider>(provider.GetRequiredService<IEmailProvider>());

            var result = await sender.SendAsync(new InviteEmail(new InviteModel()), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.Single(fake.SentMessages);
            Assert.Equal("Invite: Guest", fake.SentMessages[0].Subject);
            Assert.Equal("noreply@example.com", fake.SentMessages[0].From.Address);
        }

        [Fact]
        public async Task AddMailForge_UseFailover_WiresFailoverProviderChain()
        {
            var primary = new FakeEmailProvider();
            var backup = new FakeEmailProvider();
            var services = new ServiceCollection();
            services.AddMailForge(builder => builder
                .UseDefaultFrom("noreply@example.com")
                .UseFailover(primary, backup));

            await using var provider = services.BuildServiceProvider();
            var sender = provider.GetRequiredService<IEmailSender>();
            var resolved = Assert.IsType<FailoverEmailProvider>(provider.GetRequiredService<IEmailProvider>());

            var result = await sender.SendAsync(new InviteEmail(new InviteModel()), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            Assert.IsType<FailoverEmailProvider>(resolved);
            Assert.Single(primary.SentMessages);
            Assert.Empty(backup.SentMessages);
        }

        [Fact]
        public void AddMailForge_NullConfiguration_Throws()
        {
            var services = new ServiceCollection();
            Assert.Throws<System.ArgumentNullException>(() => services.AddMailForge(null));
        }
    }
}
