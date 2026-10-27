using System;
using MailForge.Communication.Abstractions;
using MailForge.Communication.Builders;

namespace MailForge.Webhook.Extensions
{
    /// <summary>DI and builder extensions for registering the Webhook channel.</summary>
    public static class WebhookBuilderExtensions
    {
        /// <summary>
        /// Registers the Webhook channel with the given options. The channel constructs its
        /// own <see cref="WebhookNotificationProvider"/> when the channel registry is built.
        /// </summary>
        /// <param name="builder">The communication builder.</param>
        /// <param name="options">The Webhook configuration.</param>
        public static CommunicationBuilder UseWebhookChannel(this CommunicationBuilder builder, WebhookOptions options)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return builder.UseChannel(_ => new WebhookChannel(new WebhookNotificationProvider(options)));
        }

        /// <summary>
        /// Registers the Webhook channel over an existing provider (for example, a custom
        /// provider or a failover chain).
        /// </summary>
        /// <param name="builder">The communication builder.</param>
        /// <param name="provider">The notification provider the channel delivers through.</param>
        public static CommunicationBuilder UseWebhookChannel(this CommunicationBuilder builder, INotificationProvider provider)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return builder.UseChannel(_ => new WebhookChannel(provider));
        }

        /// <summary>
        /// Registers the Webhook channel with options configured by the given callback.
        /// The channel constructs its own <see cref="WebhookNotificationProvider"/> when the
        /// channel registry is built.
        /// </summary>
        /// <param name="builder">The communication builder.</param>
        /// <param name="configure">A callback that sets the Webhook options.</param>
        public static CommunicationBuilder UseWebhookChannel(this CommunicationBuilder builder, Action<WebhookOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new WebhookOptions();
            configure(options);
            return UseWebhookChannel(builder, options);
        }
    }
}
