using System;
using System.Text;
using System.Threading.Tasks;
using MailForge.AmazonSES;
using MailForge.AzureCS;
using MailForge.Brevo;
using MailForge.Interfaces;
using MailForge.Mailgun;
using MailForge.Models;
using MailForge.Postmark;
using MailForge.Resend;
using MailForge.Smtp;
using MailForge.ZeptoMail;

namespace MailForge.Console
{
    /// <summary>
    /// Live integration checks that send a real message through a provider. Useful for
    /// verifying a provider against smtp4dev, a real SMTP server, or a provider API.
    /// </summary>
    public static class LiveTests
    {
        /// <summary>The supported live test commands.</summary>
        public static readonly string[] Commands =
        {
            "live-smtp", "live-resend", "live-ses",
            "live-postmark", "live-mailgun", "live-brevo", "live-zeptomail", "live-azurecs"
        };

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
                case "live-postmark":
                    await PostmarkAsync(args);
                    break;
                case "live-mailgun":
                    await MailgunAsync(args);
                    break;
                case "live-brevo":
                    await BrevoAsync(args);
                    break;
                case "live-zeptomail":
                    await ZeptoMailAsync(args);
                    break;
                case "live-azurecs":
                    await AzureCSAsync(args);
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

        /// <summary>
        /// Sends a message through Postmark. Usage:
        /// live-postmark [serverToken] [to]   (token also read from POSTMARK_SERVER_TOKEN)
        /// </summary>
        public static async Task PostmarkAsync(string[] args)
        {
            var serverToken = Arg(args, 1, Environment.GetEnvironmentVariable("POSTMARK_SERVER_TOKEN") ?? string.Empty);
            if (string.IsNullOrWhiteSpace(serverToken))
            {
                System.Console.WriteLine("No Postmark server token. Pass it as the first argument or set POSTMARK_SERVER_TOKEN.");
                return;
            }

            var to = ToAddress(args, 2);
            System.Console.WriteLine($"Postmark live test -> to {to}");
            var provider = new PostmarkEmailProvider(new PostmarkOptions { ServerToken = serverToken });
            await SendAsync(provider, to, "MailForge Postmark live test");
        }

        /// <summary>
        /// Sends a message through Mailgun. Usage:
        /// live-mailgun [apiKey] [domain] [to]   (values also read from MAILGUN_API_KEY / MAILGUN_DOMAIN)
        /// </summary>
        public static async Task MailgunAsync(string[] args)
        {
            var apiKey = Arg(args, 1, Environment.GetEnvironmentVariable("MAILGUN_API_KEY") ?? string.Empty);
            var domain = Arg(args, 2, Environment.GetEnvironmentVariable("MAILGUN_DOMAIN") ?? string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(domain))
            {
                System.Console.WriteLine("Mailgun requires an API key and domain. Pass [apiKey] [domain] or set MAILGUN_API_KEY / MAILGUN_DOMAIN.");
                return;
            }

            var to = ToAddress(args, 3);
            System.Console.WriteLine($"Mailgun live test -> domain {domain} to {to}");
            var provider = new MailgunEmailProvider(new MailgunOptions { ApiKey = apiKey, Domain = domain });
            await SendAsync(provider, to, "MailForge Mailgun live test");
        }

        /// <summary>
        /// Sends a message through Brevo. Usage:
        /// live-brevo [apiKey] [to]   (API key also read from BREVO_API_KEY)
        /// </summary>
        public static async Task BrevoAsync(string[] args)
        {
            var apiKey = Arg(args, 1, Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? string.Empty);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                System.Console.WriteLine("No Brevo API key. Pass it as the first argument or set BREVO_API_KEY.");
                return;
            }

            var to = ToAddress(args, 2);
            System.Console.WriteLine($"Brevo live test -> to {to}");
            var provider = new BrevoEmailProvider(new BrevoOptions { ApiKey = apiKey });
            await SendAsync(provider, to, "MailForge Brevo live test");
        }

        /// <summary>
        /// Sends a message through Zoho ZeptoMail. Usage:
        /// live-zeptomail [sendApiKey] [to]   (API key also read from ZEPTOMAIL_SEND_API_KEY)
        /// </summary>
        public static async Task ZeptoMailAsync(string[] args)
        {
            var sendApiKey = Arg(args, 1, Environment.GetEnvironmentVariable("ZEPTOMAIL_SEND_API_KEY") ?? string.Empty);
            if (string.IsNullOrWhiteSpace(sendApiKey))
            {
                System.Console.WriteLine("No ZeptoMail API key. Pass it as the first argument or set ZEPTOMAIL_SEND_API_KEY.");
                return;
            }

            var to = ToAddress(args, 2);
            System.Console.WriteLine($"ZeptoMail live test -> to {to}");
            var provider = new ZeptoMailEmailProvider(new ZeptoMailOptions { SendApiKey = sendApiKey });
            await SendAsync(provider, to, "MailForge ZeptoMail live test");
        }

        /// <summary>
        /// Sends a message through Azure Communication Services. Usage:
        /// live-azurecs [endpoint] [accessKey] [sender] [to]   (values also read from
        /// AZURE_COMMUNICATION_ENDPOINT / AZURE_COMMUNICATION_ACCESS_KEY / AZURE_COMMUNICATION_SENDER)
        /// </summary>
        public static async Task AzureCSAsync(string[] args)
        {
            var endpoint = Arg(args, 1, Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_ENDPOINT") ?? string.Empty);
            var accessKey = Arg(args, 2, Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_ACCESS_KEY") ?? string.Empty);
            var sender = Arg(args, 3, Environment.GetEnvironmentVariable("AZURE_COMMUNICATION_SENDER") ?? string.Empty);
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(sender))
            {
                System.Console.WriteLine("Azure CS requires an endpoint, access key, and verified sender address. Pass [endpoint] [accessKey] [sender] or set the AZURE_COMMUNICATION_* env vars.");
                return;
            }

            var to = ToAddress(args, 4);
            System.Console.WriteLine($"Azure Communication Services live test -> sender {sender} to {to}");
            var provider = new AzureCSEmailProvider(new AzureCSOptions
            {
                Endpoint = endpoint,
                AccessKey = accessKey,
                SenderAddress = sender
            });
            var from = sender;
            await SendAsync(provider, to, "MailForge Azure Communication Services live test", from);
        }

        private static async Task SendAsync(IEmailProvider provider, string to, string subject, string? from = null)
        {
            from ??= Environment.GetEnvironmentVariable("LIVE_FROM") ?? "sender@example.com";
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
