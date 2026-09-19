namespace MailForge.Console;

/// <summary>
/// Interactive command menu for the MailForge console. Launched when the console runs with
/// no arguments; each entry maps to a command that can also be run directly (live-*, studio-demo).
/// </summary>
internal static class ConsoleMenu
{
    public static async Task RunAsync()
    {
        while (true)
        {
            TryClearScreen();
            System.Console.WriteLine("=== MailForge Console Menu ===");
            System.Console.WriteLine();
            System.Console.WriteLine("  1. Run the demo walkthrough");
            System.Console.WriteLine("  2. Studio — local capture demo");
            System.Console.WriteLine("  3. Studio — web dashboard (+ SMTP relay on 2525)");
            System.Console.WriteLine("  4. Live test — SMTP");
            System.Console.WriteLine("  5. Live test — Resend");
            System.Console.WriteLine("  6. Live test — Amazon SES");
            System.Console.WriteLine("  7. Live test — Postmark");
            System.Console.WriteLine("  8. Live test — Mailgun");
            System.Console.WriteLine("  9. Live test — Brevo");
            System.Console.WriteLine(" 10. Live test — ZeptoMail");
            System.Console.WriteLine(" 11. Live test — Azure Communication Services");
            System.Console.WriteLine("  0. Exit");
            System.Console.WriteLine();
            System.Console.Write("Choose an option: ");

            string? input = System.Console.ReadLine();
            if (input is null)
                return; // no attached stdin (e.g. piped input); exit instead of looping

            if (!int.TryParse(input.Trim(), out int choice))
            {
                System.Console.WriteLine("Please enter a valid number.");
                WaitToContinue();
                continue;
            }

            if (choice == 0)
                return;

            if (choice is >= 1 and <= 11)
            {
                await RunMenuChoice(choice);
                System.Console.WriteLine();
                System.Console.WriteLine("Press Enter to return to the menu...");
                System.Console.ReadLine();
            }
            else
            {
                System.Console.WriteLine("Unknown option.");
                WaitToContinue();
            }
        }
    }

    private static async Task RunMenuChoice(int choice)
    {
        switch (choice)
        {
            case 1:
                await Demo.RunAsync();
                break;
            case 2:
                await StudioDemo.RunAsync(new[] { StudioDemo.Command, "0" });
                break;
            case 3:
                await StudioWebCommand.RunAsync(new[] { StudioWebCommand.Command });
                break;
            case 4:
                await LiveTests.SmtpAsync(new[] { LiveTests.Commands[0] });
                break;
            case 5:
                await LiveTests.ResendAsync(new[] { LiveTests.Commands[1] });
                break;
            case 6:
                await LiveTests.AmazonSesAsync(new[] { LiveTests.Commands[2] });
                break;
            case 7:
                await LiveTests.PostmarkAsync(new[] { LiveTests.Commands[3] });
                break;
            case 8:
                await LiveTests.MailgunAsync(new[] { LiveTests.Commands[4] });
                break;
            case 9:
                await LiveTests.BrevoAsync(new[] { LiveTests.Commands[5] });
                break;
            case 10:
                await LiveTests.ZeptoMailAsync(new[] { LiveTests.Commands[6] });
                break;
            case 11:
                await LiveTests.AzureCSAsync(new[] { LiveTests.Commands[7] });
                break;
        }
    }

    private static void TryClearScreen()
    {
        try
        {
            System.Console.Clear();
        }
        catch (IOException)
        {
            // No attached console (e.g. piped input); clearing is a no-op.
        }
    }

    private static void WaitToContinue()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("Press Enter to continue...");
        System.Console.ReadLine();
    }
}