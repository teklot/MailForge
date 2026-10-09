# Getting Started with MailForge

## Installation

Install the core package via NuGet:

```bash
dotnet add package MailForge
```

For channel-agnostic notifications (see [Communication](concepts/communication.md)):

```bash
dotnet add package MailForge.Communication
```

For the Telegram channel (text, photos, documents, inline keyboards):

```bash
dotnet add package MailForge.Telegram
```

Then add the provider packages you want to use:

```bash
# SMTP
dotnet add package MailForge.Smtp

# API-based providers
dotnet add package MailForge.Resend
dotnet add package MailForge.AmazonSES
dotnet add package MailForge.Postmark
dotnet add package MailForge.Mailgun
dotnet add package MailForge.Brevo
dotnet add package MailForge.ZeptoMail
dotnet add package MailForge.AzureCS
```

## Minimal Setup

Register MailForge with [dependency injection](https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection):

```csharp
using Microsoft.Extensions.DependencyInjection;
using MailForge.Extensions;
using MailForge.Interfaces;

var services = new ServiceCollection();
services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@example.com")
    .UseProvider(new SmtpEmailProvider(new SmtpOptions
    {
        Host = "smtp.example.com",
        Port = 587,
        Username = "user",
        Password = "secret"
    })));

await using var serviceProvider = services.BuildServiceProvider();
var sender = serviceProvider.GetRequiredService<IEmailSender>();
```

Without an explicit `UseProvider`, MailForge registers a `FakeEmailProvider` that captures
messages in memory instead of sending them — useful in development and tests.

## Sending Your First Email

Build a message with `EmailMessage.Create()` and send it:

```csharp
using MailForge.Models;

var result = await sender.SendAsync(EmailMessage.Create()
    .To("jane@example.com", "Jane")
    .Cc("ops@example.com")
    .Subject("Welcome to Acme")
    .Html("<h1>Welcome</h1><p>Thanks for joining <b>Acme</b>!</p>")
    .Text("Welcome to Acme! Thanks for joining.")
    .Tag("kind", "welcome")
    .Build());
```

The returned `EmailDeliveryResult` reports the final status (`Sent`, `FailedValidation`, or
`Failed`) and the provider's message id.

## Typed Emails

Subclass `Email<TModel>` to bind the message shape to a model:

```csharp
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

var result = await sender.SendAsync(new WelcomeEmail(new WelcomeModel("Jane")));
```

## Templates

Two renderers ship with MailForge:

- **Inline** — `{{Property.Path}}` placeholders resolved against the model, with array/index
  support (`{{Items[0].Name}}`).
- **Razor** — full Razor templates compiled with RazorEngineCore.

```csharp
services.AddMailForge(builder => builder
    .RegisterTemplate("receipt", "<p>Receipt for @Model.Amount paid (ref @Model.Reference).</p>"));
```

See [Templates](concepts/templates.md) for details.

## Local Development

For local work without a real provider:

1. **FakeEmailProvider** records all sent messages; inspect `SentMessages` in tests and demos.
2. **MailForge Studio** captures every message into a local inbox with a web dashboard and an
   SMTP relay you can point any tool at. See [Studio](concepts/studio.md).
3. Run live checks against [smtp4dev](https://github.com/rnwood/smtp4dev) or a real provider
   with the `live-*` commands, documented in [Provider Verification](provider-verification.md).

## Channel-Agnostic Notifications

`MailForge.Communication` sends the same content through a channel-neutral
`INotificationSender` — the email channel delegates to the pipeline you configured
above, the Telegram channel sends through the Telegram Bot API, and future channels
plug in the same way:

```csharp
using MailForge.Communication.Abstractions;
using MailForge.Communication.Extensions;
using MailForge.Communication.Models;

services.AddCommunication(builder => builder.UseEmailChannel());

var notifications = serviceProvider.GetRequiredService<INotificationSender>();
var notificationResult = await notifications.SendAsync(
    new Notification(ChannelType.Email, new EmailContent(
        EmailMessage.Create()
            .From("noreply@example.com")
            .To("jane@example.com")
            .Subject("Hi")
            .Text("Hi")
            .Build())));
```

See [Communication](concepts/communication.md) for channels, middleware, and results.

```csharp
using MailForge.Communication.Abstractions;
using MailForge.Communication.Extensions;
using MailForge.Communication.Models;
using MailForge.Telegram;
using MailForge.Telegram.Extensions;

services.AddCommunication(builder => builder
    .UseEmailChannel()
    .UseTelegramChannel(new TelegramOptions { BotToken = "123:abc" }));

var notifications = serviceProvider.GetRequiredService<INotificationSender>();
var telegramResult = await notifications.SendAsync(new Notification(
    ChannelType.Telegram,
    new TelegramContent("chat-id", "<b>Server down!</b>", TelegramParseMode.Html)));
```

## Next Steps

- [Architecture](architecture.md) - how the pipeline composes retries, logging, audit, and failover
- [Providers](concepts/providers.md) - provider options, auth, and capability notes
- [API Reference](api/index.md) - browse the generated API documentation