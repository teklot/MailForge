using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailForge.Extensions;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Studio.Capture;
using Microsoft.Extensions.DependencyInjection;

namespace MailForge.Console
{
    /// <summary>
    /// Demonstrates MailForge Studio locally: wires the Studio capture package into the
    /// MailForge pipeline, sends sample messages, starts the SMTP relay, and prints the
    /// captured inbox. No network or provider credentials are required.
    /// </summary>
    public static class StudioDemo
    {
        /// <summary>The studio demo command name.</summary>
        public const string Command = "studio-demo";

        /// <summary>
        /// Runs the demo. Usage:
        /// studio-demo [relayPort]   (port 0 picks an ephemeral port; defaults to 2525)
        /// </summary>
        public static async Task RunAsync(string[] args)
        {
            var relayPort = int.TryParse(Arg(args, 1, "2525"), out var parsed) ? parsed : 2525;
            var databasePath = Path.Combine(Path.GetTempPath(), "mailforge-studio-demo.db");
            if (File.Exists(databasePath))
                File.Delete(databasePath);

            var services = new ServiceCollection();
            services.AddMailForgeStudio(options =>
            {
                options.DatabasePath = databasePath;
                options.EnableSmtpRelay = true;
                options.SmtpRelayPort = relayPort;
            });
            services.AddMailForge(builder => builder
                .UseDefaultFrom("noreply@mailforge.dev")
                .UseProvider(sp => sp.GetRequiredService<StudioEmailProvider>()));

            await using var provider = services.BuildServiceProvider();
            var sender = provider.GetRequiredService<IEmailSender>();
            var store = provider.GetRequiredService<IStudioCaptureStore>();
            var relay = provider.GetRequiredService<SmtpRelayServer>();

            System.Console.WriteLine("MailForge Studio — local capture demo");
            System.Console.WriteLine($"  database : {databasePath}");
            System.Console.WriteLine();

            await relay.StartAsync();
            try
            {
                System.Console.WriteLine($"  SMTP relay running on 127.0.0.1:{relay.Port} (point any SMTP client here)");
                System.Console.WriteLine();

                await sender.SendAsync(BuildWelcome(), System.Threading.CancellationToken.None);
                await sender.SendAsync(BuildOrderConfirmation(), System.Threading.CancellationToken.None);
                await sender.SendAsync(BuildPriorityAlert(), System.Threading.CancellationToken.None);

                System.Console.WriteLine("  sent 3 sample messages through the MailForge pipeline");
                System.Console.WriteLine();

                var inbox = await store.ListAsync(cancellationToken: System.Threading.CancellationToken.None);
                System.Console.WriteLine($"Captured inbox ({inbox.Count} message(s)):");
                System.Console.WriteLine(new string('-', 100));
                foreach (var message in inbox)
                {
                    var recipients = string.Join(", ", message.Recipients.Select(r => r.DisplayName ?? r.Address));
                    var body = (message.HtmlBody ?? message.TextBody ?? string.Empty).Trim();
                    if (body.Length > 60)
                        body = body.Substring(0, 57) + "...";

                    System.Console.WriteLine($"  {message.CreatedAt:yyyy-MM-dd HH:mm:ss}  {message.From,-28} -> {recipients}");
                    System.Console.WriteLine($"    {message.Subject}");
                    System.Console.WriteLine($"    {body}");
                    System.Console.WriteLine();
                }
            }
            finally
            {
                await relay.StopAsync();
            }
        }

        private static EmailMessage BuildWelcome()
        {
            return EmailMessage.Create()
                .From("noreply@mailforge.dev", "MailForge")
                .To("alice@example.com", "Alice")
                .Subject("Welcome to Acme")
                .Html("<h1>Welcome</h1><p>Thanks for joining <b>Acme</b>!</p>")
                .Text("Welcome to Acme! Thanks for joining.")
                .Tag("kind", "welcome")
                .Build();
        }

        private static EmailMessage BuildOrderConfirmation()
        {
            return EmailMessage.Create()
                .From("noreply@mailforge.dev", "MailForge")
                .To("bob@example.com", "Bob")
                .Cc("billing@example.com")
                .Subject("Order #1042 confirmed")
                .Html("<h1>Order confirmed</h1><p>Your order is on its way.</p>")
                .Text("Order #1042 confirmed. Your order is on its way.")
                .Attachment(new EmailAttachment(
                    "invoice.pdf",
                    Encoding.UTF8.GetBytes("Demo invoice PDF body."),
                    "application/pdf"))
                .Tag("kind", "order")
                .Build();
        }

        private static EmailMessage BuildPriorityAlert()
        {
            return EmailMessage.Create()
                .From("alerts@mailforge.dev", "Acme Alerts")
                .To("ops@example.com", "Ops")
                .Subject("Disk usage above 85%")
                .Html("<p>Disk usage reached <b>88%</b> on web-01.</p>")
                .Text("Disk usage reached 88% on web-01.")
                .Priority(EmailPriority.High)
                .Header("X-Sev", "2")
                .Tag("kind", "alert")
                .Build();
        }

        private static string Arg(string[] args, int index, string defaultValue) =>
            args.Length > index && !string.IsNullOrWhiteSpace(args[index]) ? args[index] : defaultValue;
    }
}