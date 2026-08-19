using System;
using System.Text;
using System.Threading.Tasks;
using MailForge.AmazonSES;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Resend;
using MailForge.Smtp;

namespace MailForge.Console
{
    /// <summary>
    /// Live integration checks that send a real message through a provider. Useful for
    /// verifying a provider against smtp4dev, a real SMTP server, Resend, or Amazon SES.
    /// </summary>
    public static class LiveTests
    {
        /// <summary>The supported live test commands.</summary>
        public static readonly string[] Commands = { "live-smtp", "live-resend", "live-ses" };

        /// <summary>Returns true when the argument names a live test command.</summary>
        public static bool IsLiveCommand(string arg)
        {
            foreach (var command in Commands)
            {
                if (string.Equals(arg, command, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>Runs the live test named by the first argument.</summary>
        public static async Task RunAsync(string[] args)
        {
            switch (args[0].ToLowerInvariant())
            {
                case "live-smtp":
                    await SmtpAsync(args);
                    break;
                case "live-resend":
                    await ResendAsync(args);
                    break;
                case "live-ses":
                    await AmazonSesAsync(args);
                    break;
            }
        }

        /// <summary>
        /// Sends a message through SMTP. Usage:
        /// live-smtp [host] [port] [to]   (defaults: localhost 25 recipient@example.com)
        /// </summary>
        public static async Task SmtpAsync(string[] args)
        {
            var host = Arg(args, 1, "localhost");
            var port = int.TryParse(Arg(args, 2, "25"), out var parsedPort) ? parsedPort : 25;
            var to = ToAddress(args, 3);

            System.Console.WriteLine($"SMTP live test -> {host}:{port} to {to}");
            var provider = new SmtpEmailProvider(new SmtpOptions { Host = host, Port = port });
            await SendAsync(provider, to, "MailForge SMTP live test");
        }

        /// <summary>
        /// Sends a message through Resend. Usage:
        /// live-resend [apiKey] [to]   (API key also read from RESEND_API_KEY)
        /// </summary>
        public static async Task ResendAsync(string[] args)
        {
            var apiKey = Arg(args, 1, Environment.GetEnvironmentVariable("RESEND_API_KEY") ?? string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                System.Console.WriteLine("No Resend API key. Pass it as the first argument or set RESEND_API_KEY.");
                return;
            }

            var to = ToAddress(args, 2);
            System.Console.WriteLine($"Resend live test -> to {to}");
            var provider = new ResendEmailProvider(new ResendOptions { ApiKey = apiKey });
            await SendAsync(provider, to, "MailForge Resend live test");
        }

        /// <summary>
        /// Sends a message through Amazon SES. Usage:
        /// live-ses [region] [to]   (region also read from AWS_REGION; credentials from the
        /// default AWS credential chain or AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY)
        /// </summary>
        public static async Task AmazonSesAsync(string[] args)
        {
            var region = Arg(args, 1, Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1");
            var to = ToAddress(args, 2);

            var options = new AmazonSesOptions
            {
                Region = region,
                AccessKey = NullIfEmpty(Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID")),
                SecretKey = NullIfEmpty(Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY"))
            };

            System.Console.WriteLine($"Amazon SES live test -> region {region} to {to}");
            var provider = new AmazonSesEmailProvider(options);
            await SendAsync(provider, to, "MailForge SES live test");
        }

        private static async Task SendAsync(IEmailProvider provider, string to, string subject)
        {
            var from = Environment.GetEnvironmentVariable("LIVE_FROM") ?? "sender@example.com";
            var message = EmailMessage.Create()
                .From(from)
                .To(to)
                .Subject(subject)
                .Html("<h1>Hello from MailForge</h1><p>Live <b>test</b> message.</p>")
                .Text("Hello from MailForge! Live test message.")
                .Tag("purpose", "live-test")
                .Attachment(new EmailAttachment("note.txt", Encoding.UTF8.GetBytes("MailForge live test attachment.")))
                .Build();

            try
            {
                var result = await provider.SendAsync(message);
                System.Console.WriteLine($"  Succeeded: {result.Succeeded}");
                if (!string.IsNullOrEmpty(result.ProviderMessageId))
                    System.Console.WriteLine($"  ProviderMessageId: {result.ProviderMessageId}");
                if (!string.IsNullOrEmpty(result.Details))
                    System.Console.WriteLine($"  Details: {result.Details}");
            }
            catch (EmailException exception)
            {
                System.Console.WriteLine($"  Failed (transient={exception.IsTransient}): {exception.Message}");
            }
        }

        private static string Arg(string[] args, int index, string defaultValue) =>
            args.Length > index && !string.IsNullOrWhiteSpace(args[index]) ? args[index] : defaultValue;

        private static string ToAddress(string[] args, int index) =>
            Environment.GetEnvironmentVariable("LIVE_TO") ?? Arg(args, index, "recipient@example.com");

        private static string? NullIfEmpty(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
