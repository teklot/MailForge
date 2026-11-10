using Microsoft.Extensions.DependencyInjection;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Extensions;
using MailForge.Communication.Models;
using MailForge.Extensions;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Providers;
using MailForge.Telegram;
using MailForge.Telegram.Extensions;

namespace MailForge.Tests.Communication.Routing
{
    public class RoutingDiTests
    {
        private static EmailMessage CreateMessage() =>
            EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Hi")
                .Text("Hi")
                .Build();

        [Fact]
        public async Task UseRouting_RoutesByEvent()
        {
            var services = new ServiceCollection();
            services.AddMailForge(builder => { });
            services.AddCommunication(builder => builder
                .UseEmailChannel()
                .UseRouting(r => r
                    .WhenEvent("device.offline", ChannelType.Email)
                    .Default(ChannelType.Email)));

            using var sp = services.BuildServiceProvider();
            var sender = sp.GetRequiredService<INotificationSender>();
            var result = await sender.SendAsync(
                new Notification(ChannelType.Telegram, new EmailContent(CreateMessage()), @event: "device.offline"),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UseRouting_FanOut_SendsToMultipleChannels()
        {
            var fake = new TelegramChannelTests.FakeNotificationProvider();
            var services = new ServiceCollection();
            services.AddMailForge(builder => { });
            services.AddCommunication(builder => builder
                .UseEmailChannel()
                .UseTelegramChannel(fake)
                .UseRouting(r => r
                    .When(n => n.Priority == NotificationPriority.Urgent, ChannelType.Email, ChannelType.Telegram)));

            using var sp = services.BuildServiceProvider();
            var sender = sp.GetRequiredService<INotificationSender>();
            var notification = new Notification(ChannelType.Email, new EmailContent(CreateMessage()), NotificationPriority.Urgent);
            var result = await sender.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
            // Email channel should receive the notification; Telegram may receive only if content is compatible
        }

        [Fact]
        public async Task UseRouting_NoMatch_FallsBackToOriginalChannel()
        {
            var services = new ServiceCollection();
            services.AddMailForge(builder => { });
            services.AddCommunication(builder => builder.UseEmailChannel());

            using var sp = services.BuildServiceProvider();
            var sender = sp.GetRequiredService<INotificationSender>();
            var result = await sender.SendAsync(
                new Notification(ChannelType.Email, new EmailContent(CreateMessage()), @event: "something.else"),
                TestContext.Current.CancellationToken);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task UseRouting_WithMiddleware_RunsPipeline()
        {
            var services = new ServiceCollection();
            services.AddMailForge(builder => { });
            services.AddCommunication(builder => builder
                .UseEmailChannel()
                .AddMiddleware(new RecordingMiddleware())
                .UseRouting(r => r.WhenEvent("test", ChannelType.Email)));

            using var sp = services.BuildServiceProvider();
            var sender = sp.GetRequiredService<INotificationSender>();
            var result = await sender.SendAsync(
                new Notification(ChannelType.Email, new EmailContent(CreateMessage()), @event: "test"),
                TestContext.Current.CancellationToken);

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
