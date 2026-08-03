using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MailForge.Abstractions;
using MailForge.Core;
using MailForge.Core.Middleware;
using MailForge.Core.Templates;
using MailForge.Core.Validation;

namespace MailForge
{
    /// <summary>Dependency-injection extensions for the MailForge framework.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the MailForge pipeline into the service collection. The resolved
        /// <see cref="IEmailSender"/> renders, validates, and delivers messages through the
        /// configured provider and middleware.
        /// </summary>
        public static IServiceCollection AddMailForge(this IServiceCollection services, Action<MailForgeBuilder> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = new MailForgeBuilder();
            configure(builder);

            var provider = builder.Provider ?? new FakeEmailProvider();

            services.AddSingleton<IEmailProvider>(provider);
            services.AddSingleton<ITemplateRegistry>(builder.Templates);
            services.AddSingleton<IInlineTemplateRenderer>(new InlineTemplateRenderer());
            services.AddSingleton<IEmailTemplateRenderer>(new RazorEmailTemplateRenderer());

            foreach (var validator in builder.Validators)
                services.AddSingleton(validator);
            foreach (var middleware in builder.Middleware)
                services.AddSingleton(middleware);

            services.AddSingleton<IEmailSender>(sp =>
            {
                var pipeline = new List<IEmailMiddleware>
                {
                    new RetryEmailMiddleware(builder.MaxRetryAttempts, builder.RetryBaseDelay),
                    new LoggingEmailMiddleware(sp.GetService<ILogger<LoggingEmailMiddleware>>())
                };
                if (builder.AuditSink != null)
                    pipeline.Add(new AuditEmailMiddleware(builder.AuditSink, provider.Name));
                pipeline.AddRange(builder.Middleware);

                var validators = new List<IEmailValidator> { new DefaultEmailValidator() };
                validators.AddRange(builder.Validators);

                return new EmailSender(
                    sp.GetRequiredService<IEmailProvider>(),
                    pipeline,
                    validators,
                    sp.GetRequiredService<IEmailTemplateRenderer>(),
                    sp.GetRequiredService<IInlineTemplateRenderer>(),
                    sp.GetRequiredService<ITemplateRegistry>(),
                    builder.DefaultFrom,
                    builder.AutoPlainText);
            });

            return services;
        }
    }
}
