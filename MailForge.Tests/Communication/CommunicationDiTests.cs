using Microsoft.Extensions.DependencyInjection;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Extensions;
using MailForge.Communication.Models;
using MailForge.Extensions;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Providers;

namespace MailForge.Tests
{
    public class CommunicationDiTests
    {
        private static ServiceProvider CreateProvider()
        {
            var services = new ServiceCollection();
            services.AddMailForge(builder => { });
            services.AddCommunication(builder => builder.UseEmailChannel());
            return services.BuildServiceProvider();
        }

        private static EmailMessage CreateMessage() =>
            EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Parity")
                .Text("Parity")
                .Build();

        [Fact]
        public async Task AddCommunication_RegistersSenderAndEmailChannel()
        {
            using var sp = CreateProvider();

            var sender = sp.GetRequiredService<INotificationSender>();
            var registry = sp.GetRequiredService<IChannelRegistry>();

            Assert.NotNull(sender);
            Assert.NotNull(registry.Get(ChannelType.Email));

            var result = await sender.SendAsync(new Notification(ChannelType.Email, new EmailContent(CreateMessage())), TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task SendingThroughEmailChannel_ReachesTheSamePipeline()
        {
            using var sp = CreateProvider();
            var emailSender = sp.GetRequiredService<IEmailSender>();
            var notificationSender = sp.GetRequiredService<INotificationSender>();
            var message = CreateMessage();

            var emailResult = await emailSender.SendAsync(message, TestContext.Current.CancellationToken);
            var notificationResult = await notificationSender.SendAsync(new Notification(ChannelType.Email, new EmailContent(message)), TestContext.Current.CancellationToken);

            Assert.Equal(emailResult.ProviderResult.ProviderMessageId, notificationResult.ProviderResult.ProviderMessageId);
            Assert.Equal(emailResult.Details, notificationResult.Details);
            var provider = sp.GetRequiredService<IEmailProvider>() as FakeEmailProvider;
            Assert.Equal(2, provider!.SentMessages.Count);
        }

        [Fact]
        public async Task ValidationFailure_ParityMapping()
        {
            using var sp = CreateProvider();
            var emailSender = sp.GetRequiredService<IEmailSender>();
            var notificationSender = sp.GetRequiredService<INotificationSender>();
            var message = EmailMessage.Create().From("noreply@example.com").To("user@example.com").Build();

            var emailResult = await emailSender.SendAsync(message, TestContext.Current.CancellationToken);
            var notificationResult = await notificationSender.SendAsync(new Notification(ChannelType.Email, new EmailContent(message)), TestContext.Current.CancellationToken);

            Assert.False(emailResult.Succeeded);
            Assert.False(notificationResult.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.FailedValidation, notificationResult.Status);
            Assert.Equal(emailResult.Details, notificationResult.Details);
        }

        [Fact]
        public async Task AddCommunication_WithoutChannels_UnknownChannelFails()
        {
            var services = new ServiceCollection();
            services.AddCommunication(builder => { });
            using var sp = services.BuildServiceProvider();

            var sender = sp.GetRequiredService<INotificationSender>();
            var result = await sender.SendAsync(new Notification(ChannelType.Telegram, new EmailContent(CreateMessage())), TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Contains("telegram", result.Details);
        }

        [Fact]
        public async Task Middleware_RunsAroundChannelWhenRegistered()
        {
            var services = new ServiceCollection();
            services.AddMailForge(builder => { });
            services.AddCommunication(builder => builder
                .UseEmailChannel()
                .AddMiddleware(new RecordingMiddleware()));
            using var sp = services.BuildServiceProvider();

            var sender = sp.GetRequiredService<INotificationSender>();
            var result = await sender.SendAsync(new Notification(ChannelType.Email, new EmailContent(CreateMessage())), TestContext.Current.CancellationToken);
            var middleware = sp.GetServices<INotificationMiddleware>().OfType<RecordingMiddleware>().Single();

            Assert.True(result.Succeeded);
            Assert.Equal(1, middleware.InvocationCount);
        }

        private sealed class RecordingMiddleware : INotificationMiddleware
        {
            public int InvocationCount { get; private set; }

            public Task<NotificationDeliveryResult> InvokeAsync(NotificationDeliveryContext context, NextNotificationHandler next)
            {
                InvocationCount++;
                return next();
            }
        }
    }
}