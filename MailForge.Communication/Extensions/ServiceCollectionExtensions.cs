using System;
using Microsoft.Extensions.DependencyInjection;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Builders;

namespace MailForge.Communication.Extensions
{
    /// <summary>Dependency-injection extensions for the MailForge.Communication services.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the notification services into the service collection: the
        /// <see cref="IChannelRegistry"/>, configured channels, notification middleware, and a
        /// singleton <see cref="INotificationSender"/>.
        /// </summary>
        public static IServiceCollection AddCommunication(this IServiceCollection services, Action<CommunicationBuilder> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new CommunicationBuilder();
            configure(builder);

            services.AddSingleton<IChannelRegistry>(sp =>
            {
                var registry = new ChannelRegistry();
                foreach (var factory in builder.Channels)
                    registry.Register(factory(sp));
                return registry;
            });

            foreach (var middleware in builder.Middleware)
                services.AddSingleton(middleware);

            services.AddSingleton<INotificationSender>(sp =>
                new NotificationSender(
                    sp.GetRequiredService<IChannelRegistry>(),
                    sp.GetServices<INotificationMiddleware>()));

            return services;
        }
    }
}