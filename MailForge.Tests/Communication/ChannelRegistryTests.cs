using MailForge.Communication;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;

namespace MailForge.Tests
{
    public class ChannelRegistryTests
    {
        private sealed class StubChannel(ChannelType channel) : IChannel
        {
            public ChannelType Channel { get; } = channel;

            public Task<NotificationDeliveryResult> SendAsync(Notification notification, CancellationToken cancellationToken = default) =>
                Task.FromResult(NotificationDeliveryResult.Sent(notification, MailForge.Models.ProviderDeliveryResult.Success()));
        }

        [Fact]
        public void RegisterAndGet_RoundTrips()
        {
            var registry = new ChannelRegistry();
            var channel = new StubChannel(ChannelType.Email);

            registry.Register(channel);

            Assert.Same(channel, registry.Get(ChannelType.Email));
        }

        [Fact]
        public void Get_UnknownChannel_ReturnsNull()
        {
            var registry = new ChannelRegistry();

            Assert.Null(registry.Get(ChannelType.Telegram));
        }

        [Fact]
        public void Register_DuplicateChannel_Throws()
        {
            var registry = new ChannelRegistry();
            registry.Register(new StubChannel(ChannelType.Email));

            Assert.Throws<ArgumentException>(() => registry.Register(new StubChannel(ChannelType.Email)));
        }

        [Fact]
        public void Register_NullChannel_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new ChannelRegistry().Register(null!));

        [Fact]
        public void Get_ComparesCaseInsensitive()
        {
            var registry = new ChannelRegistry();
            var channel = new StubChannel(ChannelType.Email);
            registry.Register(channel);

            Assert.Same(channel, registry.Get(new ChannelType("EMAIL")));
        }
    }
}