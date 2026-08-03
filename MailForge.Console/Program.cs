using MailForge.Abstractions;
using MailForge.AmazonSES;
using MailForge.Core;
using MailForge.Resend;
using MailForge.Smtp;

namespace MailForge.Console;

public static class Program
{
    public static void Main()
    {
        var markers = new[]
        {
            typeof(MailForge.Abstractions.PackageInfo).Assembly.GetName(),
            typeof(MailForge.Core.PackageInfo).Assembly.GetName(),
            typeof(MailForge.Smtp.PackageInfo).Assembly.GetName(),
            typeof(MailForge.Resend.PackageInfo).Assembly.GetName(),
            typeof(MailForge.AmazonSES.PackageInfo).Assembly.GetName(),
        };

        System.Console.WriteLine("MailForge Phase 0 packages:");
        foreach (var name in markers)
            System.Console.WriteLine($"  {name.Name} {name.Version}");
    }
}
