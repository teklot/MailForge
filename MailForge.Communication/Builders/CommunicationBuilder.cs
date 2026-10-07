using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using MailForge.Communication.Abstractions;
using MailForge.Interfaces;

namespace MailForge.Communication.Builders
{
    /// <summary>
    /// Fluent configuration for the MailForge.Communication services. Passed to
    /// <see cref="Extensions.ServiceCollectionExtensions.AddCommunication"/> to register
    /// channels and notification middleware.
    /// </summary>
    public sealed class CommunicationBuilder
    {
        /// <summary>Initializes a new instance of the <see cref="CommunicationBuilder"/> class.</summary>
        public CommunicationBuilder() { }
        internal IList<Func<IServiceProvider, IChannel>> Channels { get; } = new List<Func<IServiceProvider, IChannel>>();
        internal IList<INotificationMiddleware> Middleware { get; } = new List<INotificationMiddleware>();

        /// <summary>Registers a channel instance.</summary>
        public CommunicationBuilder UseChannel(IChannel channel)
        {
            Channels.Add(_ => channel ?? throw new ArgumentNullException(nameof(channel)));
            return this;
        }

        /// <summary>Registers a channel that is resolved from the service provider when the registry is built.</summary>
        public CommunicationBuilder UseChannel(Func<IServiceProvider, IChannel> factory)
        {
            Channels.Add(factory ?? throw new ArgumentNullException(nameof(factory)));
            return this;
        }

        /// <summary>
        /// Registers the email channel over the configured <see cref="IEmailSender"/>. Requires
        /// <c>AddMailForge</c> to have been registered first.
        /// </summary>
        public CommunicationBuilder UseEmailChannel()
        {
            Channels.Add(sp => new EmailChannel(sp.GetRequiredService<IEmailSender>()));
            return this;
        }

        /// <summary>Adds a middleware stage to the notification pipeline.</summary>
        public CommunicationBuilder AddMiddleware(INotificationMiddleware middleware)
        {
            Middleware.Add(middleware ?? throw new ArgumentNullException(nameof(middleware)));
            return this;
        }
    }
}