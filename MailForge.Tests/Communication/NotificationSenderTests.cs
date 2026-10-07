using MailForge.Communication;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;
using MailForge.Models;

namespace MailForge.Tests
{
    public class NotificationSenderTests
    {
        private sealed class StubChannel : IChannel
        {
            public StubChannel(ChannelType channel) => Channel = channel;
            public ChannelType Channel { get; }
            public int InvocationCount { get; private set; }
            public NotificationDeliveryResult Result { get; set; } = null!;
            public bool ThrowOnSend { get; set; }

            public Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
            {
                InvocationCount++;
                if (ThrowOnSend)
                    throw new InvalidOperationException("boom");
                return Task.FromResult(Result);
            }
        }

        private sealed class CountingMiddleware : INotificationMiddleware
        {
            public List<NotificationDeliveryResult> Results { get; } = new List<NotificationDeliveryResult>();

            public Task<NotificationDeliveryResult> InvokeAsync(NotificationDeliveryContext context, NextNotificationHandler next)
            {
                Results.Add(NotificationDeliveryResult.Failed(context.Notification, ProviderDeliveryResult.Failure("pre")));
                return next();
            }
        }

        private static Notification Create(ChannelType channel, NotificationContent? content = null)
        {
            var message = EmailMessage.Create()
                .From("noreply@example.com")
                .To("user@example.com")
                .Subject("Hi")
                .Text("Hi")
                .Build();
            return new Notification(channel, content ?? new EmailContent(message));
        }

        [Fact]
        public async Task SendAsync_RoutesToRegisteredChannel()
        {
            var channel = new StubChannel(ChannelType.Email);
            var sender = new NotificationSender(new ChannelRegistry().Also(r => r.Register(channel)), Array.Empty<INotificationMiddleware>());
            var notification = Create(ChannelType.Email);

            var result = await sender.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.Equal(1, channel.InvocationCount);
            Assert.Same(channel.Result, result);
        }

        [Fact]
        public async Task SendAsync_UnknownChannel_ReturnsFailed()
        {
            var registry = new ChannelRegistry();
            registry.Register(new StubChannel(ChannelType.Email));
            var sender = new NotificationSender(registry, Array.Empty<INotificationMiddleware>());
            var notification = Create(ChannelType.Telegram);

            var result = await sender.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.False(result.Succeeded);
            Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
            Assert.Contains("telegram", result.Details);
        }

        [Fact]
        public async Task SendAsync_RunsMiddlewareInOrder()
        {
            var channel = new StubChannel(ChannelType.Email);
            var middleware = new CountingMiddleware();
            var sender = new NotificationSender(
                new ChannelRegistry().Also(r => r.Register(channel)),
                new INotificationMiddleware[] { middleware });
            var notification = Create(ChannelType.Email);

            await sender.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.Single(middleware.Results);
            Assert.Equal(1, channel.InvocationCount);
        }

        [Fact]
        public async Task SendAsync_MiddlewareCanShortCircuit()
        {
            var channel = new StubChannel(ChannelType.Email);
            var shortCircuit = new BlockingMiddleware();
            var sender = new NotificationSender(
                new ChannelRegistry().Also(r => r.Register(channel)),
                new INotificationMiddleware[] { shortCircuit });
            var notification = Create(ChannelType.Email);

            var result = await sender.SendAsync(notification, TestContext.Current.CancellationToken);

            Assert.Equal(0, channel.InvocationCount);
            Assert.Equal(NotificationDeliveryStatus.Failed, result.Status);
        }

        [Fact]
        public async Task SendAsync_ChannelException_WrappedInNotificationException()
        {
            var channel = new StubChannel(ChannelType.Email) { ThrowOnSend = true };
            var sender = new NotificationSender(
                new ChannelRegistry().Also(r => r.Register(channel)),
                Array.Empty<INotificationMiddleware>());
            var notification = Create(ChannelType.Email);

            var exception = await Assert.ThrowsAsync<NotificationException>(() => sender.SendAsync(notification, TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Equal("boom", exception.InnerException?.Message);
        }

        [Fact]
        public async Task SendAsync_NotificationExceptionPropagates()
        {
            var throwing = new ThrowingChannel(new NotificationException("transient", isTransient: true));
            var sender = new NotificationSender(
                new ChannelRegistry().Also(r => r.Register(throwing)),
                Array.Empty<INotificationMiddleware>());
            var notification = Create(ChannelType.Email);

            var exception = await Assert.ThrowsAsync<NotificationException>(() => sender.SendAsync(notification, TestContext.Current.CancellationToken));

            Assert.True(exception.IsTransient);
            Assert.Equal("transient", exception.Message);
        }

        [Fact]
        public async Task SendAsync_CancellationPropagates()
        {
            var channel = new StubChannel(ChannelType.Email);
            var sender = new NotificationSender(
                new ChannelRegistry().Also(r => r.Register(channel)),
                Array.Empty<INotificationMiddleware>());
            var notification = Create(ChannelType.Email);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(notification, cts.Token));
        }

        private sealed class BlockingMiddleware : INotificationMiddleware
        {
            public Task<NotificationDeliveryResult> InvokeAsync(NotificationDeliveryContext context, NextNotificationHandler next) =>
                Task.FromResult(NotificationDeliveryResult.Failed(context.Notification, ProviderDeliveryResult.Failure("blocked")));
        }

        private sealed class ThrowingChannel(NotificationException exception) : IChannel
        {
            public ChannelType Channel => ChannelType.Email;

            public Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default)
            {
                throw exception;
            }
        }
    }

    internal static class RegistryExtensions
    {
        public static T Also<T>(this T value, Action<T> configure)
        {
            configure(value);
            return value;
        }
    }
}