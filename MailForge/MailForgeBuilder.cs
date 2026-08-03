using System;
using System.Collections.Generic;
using System.Linq;
using MailForge.Templates;

namespace MailForge
{
    /// <summary>
    /// Fluent configuration for the MailForge framework. Passed to
    /// <see cref="MailForge.ServiceCollectionExtensions.AddMailForge"/> to select a provider,
    /// register templates, validators, and middleware, and tune delivery behavior.
    /// </summary>
    public sealed class MailForgeBuilder
    {
        internal IEmailProvider Provider { get; private set; }
        internal EmailAddress DefaultFrom { get; private set; }
        internal bool AutoPlainText { get; private set; } = true;
        internal int MaxRetryAttempts { get; private set; } = 3;
        internal TimeSpan RetryBaseDelay { get; private set; } = TimeSpan.FromSeconds(1);
        internal IEmailAuditSink AuditSink { get; private set; }
        internal ITemplateRegistry Templates { get; } = new TemplateRegistry();
        internal IList<IEmailValidator> Validators { get; } = new List<IEmailValidator>();
        internal IList<IEmailMiddleware> Middleware { get; } = new List<IEmailMiddleware>();

        /// <summary>Selects the provider that delivers messages. Defaults to <see cref="FakeEmailProvider"/>.</summary>
        public MailForgeBuilder UseProvider(IEmailProvider provider)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            return this;
        }

        /// <summary>Sets a default sender used when a typed email does not specify one.</summary>
        public MailForgeBuilder UseDefaultFrom(EmailAddress from)
        {
            DefaultFrom = from ?? throw new ArgumentNullException(nameof(from));
            return this;
        }

        /// <summary>Sets a default sender from a raw address string.</summary>
        public MailForgeBuilder UseDefaultFrom(string address, string displayName = null) =>
            UseDefaultFrom(new EmailAddress(address, displayName));

        /// <summary>
        /// Enables or disables automatic plain-text generation from the HTML body
        /// (enabled by default).
        /// </summary>
        public MailForgeBuilder EnableAutoPlainText(bool enabled = true)
        {
            AutoPlainText = enabled;
            return this;
        }

        /// <summary>Configures transient-failure retries.</summary>
        /// <param name="maxAttempts">The maximum number of delivery attempts (1 disables retries).</param>
        /// <param name="baseDelay">The base delay before the first retry (default 1 second).</param>
        public MailForgeBuilder WithRetries(int maxAttempts, TimeSpan? baseDelay = null)
        {
            MaxRetryAttempts = Math.Max(1, maxAttempts);
            if (baseDelay.HasValue)
                RetryBaseDelay = baseDelay.Value;
            return this;
        }

        /// <summary>Registers an audit sink that records every delivery attempt.</summary>
        public MailForgeBuilder UseAuditSink(IEmailAuditSink auditSink)
        {
            AuditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
            return this;
        }

        /// <summary>Registers a named Razor template available to typed emails.</summary>
        public MailForgeBuilder RegisterTemplate(string name, string content)
        {
            Templates.Register(name, content);
            return this;
        }

        /// <summary>Adds a validator run before delivery.</summary>
        public MailForgeBuilder AddValidator(IEmailValidator validator)
        {
            Validators.Add(validator ?? throw new ArgumentNullException(nameof(validator)));
            return this;
        }

        /// <summary>Adds a middleware stage to the delivery pipeline.</summary>
        public MailForgeBuilder AddMiddleware(IEmailMiddleware middleware)
        {
            Middleware.Add(middleware ?? throw new ArgumentNullException(nameof(middleware)));
            return this;
        }

        /// <summary>
        /// Wraps providers in a failover precedence chain, each route carrying its own
        /// <see cref="ProviderFailoverRoute.Policy"/> and attempt budget. Routes are tried in
        /// order and delivery stops at the first success.
        /// </summary>
        public MailForgeBuilder UseFailover(IEnumerable<ProviderFailoverRoute> routes)
        {
            return UseProvider(new FailoverEmailProvider(routes));
        }

        /// <summary>Wraps providers in a failover precedence chain from routes listed in order.</summary>
        public MailForgeBuilder UseFailover(params ProviderFailoverRoute[] routes)
        {
            return UseProvider(new FailoverEmailProvider(routes));
        }

        /// <summary>
        /// Wraps providers in a failover precedence chain tried in order until one succeeds.
        /// Every route uses the default <see cref="FailoverPolicy.TransientOnly"/> policy with a
        /// single attempt.
        /// </summary>
        public MailForgeBuilder UseFailover(params IEmailProvider[] providers)
        {
            if (providers == null)
                throw new ArgumentNullException(nameof(providers));
            return UseProvider(new FailoverEmailProvider(providers));
        }

        /// <summary>Wraps providers in a failover chain that applies <paramref name="policy"/> to every route.</summary>
        public MailForgeBuilder UseFailover(FailoverPolicy policy, params IEmailProvider[] providers)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (providers == null)
                throw new ArgumentNullException(nameof(providers));

            var routes = providers.Select(provider => new ProviderFailoverRoute(provider, policy)).ToArray();
            return UseProvider(new FailoverEmailProvider(routes));
        }
    }
}
