# MailForge

**The .NET email platform for multi-provider delivery.**

MailForge is a layered .NET email framework that gives you one strongly-typed API for
sending, templating, validation, retries, auditing, and failover across eight email
providers — plus a local Studio inbox for development and a channel-agnostic notification
layer: email, Telegram, or any channel you register.

## Quick Links

- [Getting Started](getting-started.md) - Install and send your first email
- [Architecture](architecture.md) - Understand the layers and delivery pipeline
- [Communication](concepts/communication.md) - Channel-agnostic notifications
- [API Reference](~/api/index.md) - Complete API documentation
- [Migration Guide](migration.md) - upgrading from earlier releases
- [Provider Verification](provider-verification.md) - Live provider commands and the release checklist

## Packages

| Package | Description |
|---------|-------------|
| [MailForge](api/MailForge.yml) | Core pipeline: models, builders, templates, validation, retries, auditing, failover |
| [MailForge.Communication](api/MailForge.Communication.yml) | Channel-agnostic notifications: `INotificationSender`, channels, middleware |
| [MailForge.Telegram](api/MailForge.Telegram.yml) | Telegram Bot API channel: text, photos, documents, and inline keyboards |
| [MailForge.Smtp](api/MailForge.Smtp.yml) | SMTP delivery via MailKit |
| [MailForge.Resend](api/MailForge.Resend.yml) | [Resend](https://resend.com) API provider |
| [MailForge.AmazonSES](api/MailForge.AmazonSES.yml) | Amazon Simple Email Service provider |
| [MailForge.Postmark](api/MailForge.Postmark.yml) | Postmark API provider |
| [MailForge.Mailgun](api/MailForge.Mailgun.yml) | Mailgun API provider |
| [MailForge.Brevo](api/MailForge.Brevo.yml) | Brevo (Sendinblue) API provider |
| [MailForge.ZeptoMail](api/MailForge.ZeptoMail.yml) | Zoho ZeptoMail API provider |
| [MailForge.AzureCS](api/MailForge.AzureCS.yml) | Azure Communication Services provider |

## Example

```csharp
using MailForge.Extensions;
using MailForge.Interfaces;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@example.com")
    .UseProvider(new MailForge.Postmark.PostmarkEmailProvider(
        new MailForge.Postmark.PostmarkOptions { ServerToken = "..." })));

await using var provider = services.BuildServiceProvider();
var sender = provider.GetRequiredService<IEmailSender>();

var result = await sender.SendAsync(
    MailForge.Models.EmailMessage.Create()
        .To("user@example.com")
        .Subject("Hello from MailForge")
        .Html("<h1>Hello</h1><p>Delivered through Postmark.</p>")
        .Build());

Console.WriteLine(result.Status);
```