using System;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Builders;

namespace MailForge.Telegram.Extensions
{
    /// <summary>DI and builder extensions for registering the Telegram channel.</summary>
    public static class TelegramBuilderExtensions
    {
        /// <summary>
        /// Registers the Telegram channel with the given options. The channel constructs its
        /// own <see cref="TelegramNotificationProvider"/> when the channel registry is built.
        /// </summary>
        /// <param name="builder">The communication builder.</param>
        /// <param name="options">The Telegram configuration.</param>
        public static CommunicationBuilder UseTelegramChannel(this CommunicationBuilder builder, TelegramOptions options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return builder.UseChannel(_ => new TelegramChannel(new TelegramNotificationProvider(options)));
        }

        /// <summary>
        /// Registers the Telegram channel over an existing provider (for example, a custom
        /// provider or a failover chain).
        /// </summary>
        /// <param name="builder">The communication builder.</param>
        /// <param name="provider">The notification provider the channel delivers through.</param>
        public static CommunicationBuilder UseTelegramChannel(this CommunicationBuilder builder, INotificationProvider provider)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return builder.UseChannel(_ => new TelegramChannel(provider));
        }

        /// <summary>
        /// Registers the Telegram channel with options configured by the given callback.
        /// The channel constructs its own <see cref="TelegramNotificationProvider"/> when the
        /// channel registry is built.
        /// </summary>
        /// <param name="builder">The communication builder.</param>
        /// <param name="configure">A callback that sets the Telegram options.</param>
        public static CommunicationBuilder UseTelegramChannel(this CommunicationBuilder builder, Action<TelegramOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new TelegramOptions();
            configure(options);
            return UseTelegramChannel(builder, options);
        }
    }
}
