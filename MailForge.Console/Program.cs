using System;
using System.Threading.Tasks;

namespace MailForge.Console
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            if (args.Length > 0 && LiveTests.IsLiveCommand(args[0]))
            {
                await LiveTests.RunAsync(args);
                return;
            }

            if (args.Length > 0)
                System.Console.WriteLine($"Unknown command '{args[0]}'. Supported live commands: {string.Join(", ", LiveTests.Commands)}.\n");

            await Demo.RunAsync();
        }
    }
}
