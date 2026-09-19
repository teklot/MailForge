using System;
using System.Threading.Tasks;

namespace MailForge.Console
{
    /// <summary>
    /// MailForge console entry point. With no arguments the interactive command menu is shown.
    /// Run live tests or the Studio demo directly with: live-smtp, live-resend, live-ses,
    /// live-postmark, live-mailgun, live-brevo, live-zeptomail, live-azurecs, studio-demo, studio-web.
    /// </summary>
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && LiveTests.IsLiveCommand(args[0]))
            {
                await LiveTests.RunAsync(args);
                return;
            }

            if (args.Length > 0 && string.Equals(args[0], StudioDemo.Command, StringComparison.OrdinalIgnoreCase))
            {
                await StudioDemo.RunAsync(args);
                return;
            }

            if (args.Length > 0 && string.Equals(args[0], StudioWebCommand.Command, StringComparison.OrdinalIgnoreCase))
            {
                await StudioWebCommand.RunAsync(args);
                return;
            }

            if (args.Length > 0)
                System.Console.WriteLine($"Unknown command '{args[0]}'. Supported commands: {string.Join(", ", LiveTests.Commands)}, {StudioDemo.Command}, {StudioWebCommand.Command}.\n");

            await ConsoleMenu.RunAsync();
        }
    }
}
