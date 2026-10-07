using System;
using System.Collections.Generic;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;

namespace MailForge.Communication
{
    /// <summary>
    /// The default <see cref="IChannelRegistry"/> implementation. At most one channel is
    /// registered per channel type; duplicate registrations throw.
    /// </summary>
    public sealed class ChannelRegistry : IChannelRegistry
    {
        private readonly Dictionary<ChannelType, IChannel> _channels = new Dictionary<ChannelType, IChannel>();

        /// <summary>Registers a channel. Throws when a channel for the same type already exists.</summary>
        public void Register(IChannel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (_channels.ContainsKey(channel.Channel))
                throw new ArgumentException($"A channel is already registered for '{channel.Channel.Name}'.", nameof(channel));
            _channels[channel.Channel] = channel;
        }

        /// <summary>Returns the channel registered for <paramref name="channel"/>, or null.</summary>
        public IChannel? Get(ChannelType channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            return _channels.TryGetValue(channel, out var value) ? value : null;
        }
    }
}