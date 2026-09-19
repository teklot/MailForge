using System;
using System.IO;
using System.Threading.Tasks;
using MailForge.Studio.Web;

namespace MailForge.Console
{
    /// <summary>
    /// Starts the MailForge Studio web dashboard. An optional port and database path
    /// can be provided; defaults are port 5000 and a temporary SQLite database.
    /// </summary>
    public static class StudioWebCommand
    {
        /// <summary>The studio-web command name.</summary>
        public const string Command = "studio-web";

        /// <summary>
        /// Runs the web dashboard. Usage:
        /// studio-web [port] [dbPath] [relayPort]
        /// The SMTP capture relay listens on relayPort (default 2525); pass "off" to disable it.
        /// A shared temporary SQLite database (mailforge-studio.db) is used when dbPath is omitted,
        /// so studio-demo output is visible in the dashboard.
        /// </summary>
        public static async Task RunAsync(string[] args)
        {
            var port = int.TryParse(Arg(args, 1, "5000"), out var parsed) ? parsed : 5000;
            var dbPath = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
                ? args[2]
                : Path.Combine(Path.GetTempPath(), "mailforge-studio.db");

            var smtpRelay = !string.Equals(Arg(args, 3, "2525"), "off", StringComparison.OrdinalIgnoreCase);
            var smtpRelayPort = int.TryParse(Arg(args, 3, "2525"), out var relayPort) ? relayPort : 2525;

            var options = new StudioWebOptions
            {
                DatabasePath = dbPath,
                WebPort = port,
                EnableSmtpRelay = smtpRelay,
                SmtpRelayPort = smtpRelayPort,
            };

            System.Console.WriteLine("MailForge Studio — Web Dashboard");
            System.Console.WriteLine($"  database   : {options.DatabasePath}");
            System.Console.WriteLine($"  url        : http://localhost:{options.WebPort}");
            if (options.EnableSmtpRelay)
                System.Console.WriteLine($"  SMTP relay : 127.0.0.1:{options.SmtpRelayPort} (point any SMTP client here)");
            System.Console.WriteLine();

            await StudioWebHost.StartAsync(options);
        }

        private static string Arg(string[] args, int index, string defaultValue) =>
            args.Length > index && !string.IsNullOrWhiteSpace(args[index]) ? args[index] : defaultValue;
    }
}
