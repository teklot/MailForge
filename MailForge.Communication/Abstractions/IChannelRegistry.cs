using MailForge.Communication.Models;

namespace MailForge.Communication.Abstractions
{
    /// <summary>
    /// A registry that resolves the <see cref="IChannel"/> responsible for a
    /// <see cref="ChannelType"/>. At most one channel is registered per channel type.
    /// </summary>
    public interface IChannelRegistry
    {
        /// <summary>Registers a channel. Throws when a channel for the same type already exists.</summary>
        /// <param name="channel">The channel to register.</param>
        void Register(IChannel channel);

        /// <summary>Returns the channel registered for <paramref name="channel"/>, or null.</summary>
        /// <param name="channel">The channel type to resolve.</param>
        IChannel? Get(ChannelType channel);
    }
}