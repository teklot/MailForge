using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MailForge;
using MailForge.Extensions;
using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Providers;

namespace MailForge.Console
{
    public sealed record WelcomeModel(string Name);

    public sealed class WelcomeEmail : Email<WelcomeModel>
    {
        public WelcomeEmail(WelcomeModel model) : base(model)
        {
            AddTo("user@example.com");
            SetSubject("Welcome {{Name}}!");
            SetHtml("<h1>Welcome {{Name}}!</h1><p>Thanks for joining MailForge.</p>");
        }
    }

    public sealed record ReceiptModel(decimal Amount, string Reference);

    public sealed class ReceiptEmail : Email<ReceiptModel>
    {
        public ReceiptEmail(ReceiptModel model) : base(model)
        {
            AddTo("customer@example.com");
            SetSubject("Your receipt");
            UseTemplate("receipt");
            AddTag("purpose", "receipt");
            Attach(new EmailAttachment("receipt.txt", System.Text.Encoding.UTF8.GetBytes("Receipt for " + model.Amount)));
        }
    }

    public static class Demo
    {
        public static async Task RunAsync()
        {
            var services = new ServiceCollection();
            services.AddMailForge(builder => builder
                .UseDefaultFrom("noreply@example.com")
                .RegisterTemplate("receipt", "<p>Receipt for @Model.Amount paid (ref @Model.Reference).</p>"));

            var serviceProvider = services.BuildServiceProvider();
            var sender = serviceProvider.GetRequiredService<IEmailSender>();

            System.Console.WriteLine("Sending typed email with inline template...");
            var welcomeResult = await sender.SendAsync(new WelcomeEmail(new WelcomeModel("Jane")), System.Threading.CancellationToken.None);
            System.Console.WriteLine($"  -> {welcomeResult.Status}");

            System.Console.WriteLine("Sending typed email with named Razor template and attachment...");
            var receiptResult = await sender.SendAsync(new ReceiptEmail(new ReceiptModel(49.99m, "INV-1234")), System.Threading.CancellationToken.None);
            System.Console.WriteLine($"  -> {receiptResult.Status}");

            var fake = (FakeEmailProvider)serviceProvider.GetRequiredService<IEmailProvider>();
            System.Console.WriteLine($"\nFake provider captured {fake.SentMessages.Count} message(s):");
            foreach (var message in fake.SentMessages)
            {
                System.Console.WriteLine($"  - {message.Subject}");
                System.Console.WriteLine($"      To:       {string.Join(", ", message.ToRecipients)}");
                System.Console.WriteLine($"      Html:     {message.HtmlBody}");
                System.Console.WriteLine($"      Text:     {message.TextBody}");
                System.Console.WriteLine($"      Attachments: {message.Attachments.Count}");
            }
        }
    }
}
