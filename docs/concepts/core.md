# Core

The `MailForge` package contains the framework pipeline: models, builders, typed emails,
rendering, validation, retries, auditing, middleware, and failover.

## Models

- **`EmailMessage`** — the neutral wire model: `From`, `Subject`, `HtmlBody`, `TextBody`,
  `Priority`, `Recipients` (To/Cc/Bcc), `Attachments`, `Headers`, `Tags`, `MessageId`,
  `CreatedAt`. Build one with `EmailMessage.Create()`.
- **`EmailAddress`** / **`EmailRecipient`** — addresses with an optional display name and a
  recipient role (`To`, `Cc`, `Bcc`).
- **`EmailAttachment`** — attachment bytes with file name, media type, inline (`IsInline`,
  `ContentId`) support, and `FromFile`, `GuessMediaType` helpers.
- **`EmailDeliveryResult`** — the outcome of a send through the full pipeline: `Status`
  (`Sent` | `FailedValidation` | `Failed`), `Succeeded`, `ProviderResult`,
  `CompletedAt`, `Details`.
- **`ProviderDeliveryResult`** — a single provider attempt result: `Succeeded`,
  `ProviderMessageId`, `Details`.
- **`EmailException`** — provider failures; `IsTransient` drives retry and failover policy.

## Building a Message

```csharp
using MailForge.Models;

var message = EmailMessage.Create()
    .From("noreply@example.com", "Acme")
    .To("jane@example.com")
    .Cc("billing@example.com")
    .Subject("Order #1042 confirmed")
    .Html("<h1>Order confirmed</h1><p>On its way.</p>")
    .Text("Order #1042 confirmed. On its way.")
    .Priority(EmailPriority.High)
    .Header("X-Sev", "2")
    .Tag("kind", "order")
    .Attachment(new EmailAttachment("invoice.pdf", bytes, "application/pdf"))
    .Build();
```

## Typed Emails

Subclass `Email<TModel>` and build the message in the constructor from the model:

```csharp
public sealed class WelcomeEmail : Email<WelcomeModel>
{
    public WelcomeEmail(WelcomeModel model) : base(model)
    {
        AddTo("user@example.com");
        SetSubject("Welcome {{Name}}!");
        SetHtml("<h1>Welcome {{Name}}!</h1>");
        AddTag("kind", "welcome");
    }
}
```

The protected builder surface is `SetFrom`, `AddTo`, `AddCc`, `AddBcc`, `SetSubject`,
`UseTemplate`, `SetHtml`, `SetText`, `SetPriority`, `Attach`, `AddHeader`, `AddTag`.

Send typed emails with `sender.SendAsync(new WelcomeEmail(model))`.

## The Sender

`IEmailSender` is the entry point. `EmailSender` composes rendering, validation, retries,
logging, audit, and your middleware, then hands the message to the configured provider.
Register it via `AddMailForge`; you can also construct `EmailSender` directly if you are
not using dependency injection.

## Validation

The default `DefaultEmailValidator` requires a sender, at least one recipient, a subject,
and a body, and checks every address is well formed. Add more validators:

```csharp
services.AddMailForge(builder => builder
    .AddValidator(new MyDomainValidator())
    .AddValidator(new PrivacyGuard()));
```

Validators are `IEmailValidator` implementations returning
`IReadOnlyList<ValidationProblem>`.

## Auto Plain Text

Plain-text generation from `HtmlBody` is **enabled by default** — a `TextBody` is derived
from HTML when only HTML was supplied, stripping `<script>`/`<style>`/`<title>` blocks and
collapsing whitespace at block boundaries. Disable it with `EnableAutoPlainText(false)`.

## Retries, Logging, and Auditing

- **Retries** — `WithRetries(int maxAttempts, TimeSpan? baseDelay)` (default 3 attempts, 1s
  base delay). Only transient `EmailException`s are retried, with exponential backoff
  (`baseDelay × 2^attempt`) plus up to 20% jitter, capped at attempt 6.
- **Logging** — `LoggingEmailMiddleware` writes a structured log line per outcome when an
  `ILogger` is registered.
- **Audit** — `UseAuditSink(IEmailAuditSink)` records each attempt through
  `IEmailAuditSink.RecordAsync(EmailAuditRecord)`.

## Custom Middleware

Implement `IEmailMiddleware` (see `NextEmailHandler`) and register it
with `AddMiddleware`, which runs after the built-in retry/logging/audit middleware:

```csharp
public sealed class KillSwitchMiddleware : IEmailMiddleware
{
    public async Task<EmailDeliveryResult> InvokeAsync(EmailDeliveryContext context, NextEmailHandler next)
    {
        if (context.Items.ContainsKey("maintenance"))
            return EmailDeliveryResult.Failed(context.Message, "killed by maintenance mode");
        return await next();
    }
}
```