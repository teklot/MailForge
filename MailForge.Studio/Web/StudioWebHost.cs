using MailForge.Studio.Capture;
using MailForge.Studio.Web.Components;
using MailForge.Studio.Web.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MailForge.Studio.Web;

/// <summary>Hosts the Studio web dashboard as a Minimal API application.</summary>
public static class StudioWebHost
{
    /// <summary>
    /// Builds a configured <see cref="WebApplication"/> for the Studio web dashboard without
    /// starting it, allowing tests to host it via the ASP.NET Core TestServer.
    /// </summary>
    public static WebApplication Build(StudioWebOptions webOptions, Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder();
        configureBuilder?.Invoke(builder);

        builder.WebHost.UseUrls($"http://127.0.0.1:{webOptions.WebPort}");

        var captureOptions = new StudioCaptureOptions
        {
            DatabasePath = webOptions.DatabasePath,
            EnableSmtpRelay = webOptions.EnableSmtpRelay,
            SmtpRelayHost = "127.0.0.1",
            SmtpRelayPort = webOptions.SmtpRelayPort,
        };

        builder.Services.AddSingleton(webOptions);
        builder.Services.AddSingleton(captureOptions);
        builder.Services.AddDbContextFactory<StudioDbContext>(db =>
            db.UseSqlite(captureOptions.BuildConnectionString()));
        builder.Services.AddSingleton<IStudioCaptureStore, StudioCaptureStore>();
        builder.Services.AddSingleton<MessageReplayer>();
        if (webOptions.EnableSmtpRelay)
            builder.Services.AddSingleton<SmtpRelayServer>();

        var app = builder.Build();

        var api = app.MapGroup("/api/messages");

        api.MapGet("/", MessagesApi.List);
        api.MapGet("/{id:guid}", MessagesApi.Get);
        api.MapGet("/{id:guid}/export", MessagesApi.Export);
        api.MapGet("/{id:guid}/raw", MessagesApi.Raw);
        api.MapGet("/{id:guid}/attachments/{attachmentId:guid}", MessagesApi.Attachment);
        api.MapPost("/{id:guid}/replay", MessagesApi.Replay);

        app.MapGet("/", IndexPage.Render);
        app.MapGet("/messages/{id:guid}", DetailPage.Render);

        return app;
    }

    /// <summary>Builds and starts a standalone <see cref="WebApplication"/> for the Studio web dashboard.</summary>
    public static async Task StartAsync(StudioWebOptions webOptions, CancellationToken ct = default)
    {
        var app = Build(webOptions);

        SmtpRelayServer? relay = null;
        if (webOptions.EnableSmtpRelay)
        {
            relay = app.Services.GetRequiredService<SmtpRelayServer>();
            await relay.StartAsync(ct);
        }

        try
        {
            Console.WriteLine($"Studio web dashboard: http://127.0.0.1:{webOptions.WebPort}");
            if (relay is not null)
                Console.WriteLine($"SMTP capture relay  : 127.0.0.1:{relay.Port} (point any SMTP client here)");
            Console.WriteLine("Press Ctrl+C to stop.");

            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            await app.RunAsync(cts.Token);
        }
        finally
        {
            if (relay is not null)
                await relay.StopAsync();
        }
    }
}