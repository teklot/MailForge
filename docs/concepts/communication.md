# Communication

The `MailForge.Communication` package (v1.1.0) generalizes MailForge from an email-only
library into a channel-aware notification framework — without changing the email API. It
sits **on top of** the core `MailForge` pipeline: the first channel (`EmailChannel`)
delegates to the existing `IEmailSender`, so every email behavior (validation, retries,
auditing, failover) is preserved as-is.

## The Model

A communication is a single `Notification` addressed to one channel:

- **`Notification`** — `Channel` (a `ChannelType`), `Content`, `Priority`
  (`Low`/`Normal`/`High`/`Urgent`), optional case-insensitive `Headers` and `Tags`, and a
  generated `MessageId`/`CreatedAt`.
- **`ChannelType`** — a string identity with case-insensitive equality and constant
  entries: `Email`, `Sms`, `Telegram`, `Webhook`, `WhatsApp`, `Push`. Custom channels are
  just `new ChannelType("my-channel")`.
- **`NotificationContent`** — per-channel payloads. `EmailContent` wraps the existing
  `EmailMessage`; future channels add their own content types.

```csharp
using MailForge.Communication.Abstractions;
using MailForge.Communication.Models;

var notification = new Notification(
    ChannelType.Email,
    new EmailContent(emailMessage),
    NotificationPriority.High,
    headers: new Dictionary<string, string> { { "X-Sev", "2" } },
    tags: new Dictionary<string, string> { { "kind", "order" } });
```

## Sending

`INotificationSender` routes a notification to the channel registered for its
`ChannelType`:

```csharp
INotificationSender sender = ...;
NotificationDeliveryResult result = await sender.SendAsync(notification);
```

`NotificationDeliveryResult` mirrors `EmailDeliveryResult`: `Status`
(`Sent` | `FailedValidation` | `Failed`), `Succeeded`, `ProviderResult`
(the underlying `ProviderDeliveryResult`), and `Details`. Unknown channels and provider
failures are mapped to `Failed`; unexpected channel exceptions are wrapped in a
`NotificationException` with `IsTransient = true`.

## Channels and Middleware

- **`IChannel`** — sends a `Notification` and returns a `NotificationDeliveryResult`.
- **`INotificationProvider`** — a channel-agnostic provider contract (channel vs. provider
  separation). `EmailChannel` implements it by delegating to `IEmailSender`.
- **`INotificationMiddleware`** — runs around the channel (with a
  `NextNotificationHandler`), matching the `IEmailMiddleware` mental model. Middleware runs
  in registration order and can short-circuit.

## Registration

```csharp
services.AddCommunication(builder => builder
    .UseEmailChannel()
    .AddMiddleware(new KillSwitchMiddleware()));
```

`UseChannel(IChannel)` and `UseChannel(Func<IServiceProvider, IChannel>)` register any
channel; `UseEmailChannel()` wires the email adapter over the `IEmailSender` registered by
`AddMailForge`. `AddCommunication` registers `INotificationSender`,
`IChannelRegistry`, and every configured channel and middleware as singletons.

See [Core](core.md) for the email pipeline and
[Architecture](../architecture.md) for where communication fits.