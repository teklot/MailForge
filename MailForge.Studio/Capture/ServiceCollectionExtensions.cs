using System;
using MailForge.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MailForge.Studio.Capture
{
    /// <summary>Dependency-injection extensions for the MailForge Studio capture subsystem.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the MailForge Studio capture subsystem: the SQLite-backed capture store,
        /// the <see cref="StudioEmailProvider"/>, and (optionally) the SMTP relay server.
        /// Point the MailForge pipeline at the studio provider with
        /// <c>UseProvider(sp =&gt; sp.GetRequiredService&lt;StudioEmailProvider&gt;())</c>.
        /// </summary>
        public static IServiceCollection AddMailForgeStudio(
            this IServiceCollection services,
            Action<StudioCaptureOptions> configure)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new StudioCaptureOptions();
            configure(options);

            services.AddSingleton(options);
            services.AddSingleton<IStudioCaptureStore, StudioCaptureStore>();
            services.AddSingleton<StudioEmailProvider>();

            services.AddDbContextFactory<StudioDbContext>(builder =>
                builder.UseSqlite(options.BuildConnectionString()));

            if (options.EnableSmtpRelay)
            {
                services.AddSingleton<SmtpRelayServer>();
                services.TryAddEnumerable(ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, SmtpRelayHostedService>());
            }

            return services;
        }
    }
}