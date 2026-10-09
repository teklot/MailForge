# Communication

The `MailForge.Communication` package generalizes MailForge from an email-only
library into a channel-aware notification framework — without changing the email API. It
sits **on top of** the core `MailForge` pipeline: the `EmailChannel` delegates to the
existing `IEmailSender`, so every email behavior (validation, retries, auditing, failover)
is preserved as-is. Channels live in their own packages — `MailForge.Telegram` ships the
first non-email one, and any channel plugs into the same `INotificationProvider` contract.

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

## Telegram Channel

`MailForge.Telegram` adds Telegram as a first-class channel: `TelegramContent` carries one
or more chat ids plus the message text, an optional parse mode, media, and an inline
keyboard. `TelegramChannel` hands the notification to `TelegramNotificationProvider`, which
calls the Bot API (`sendMessage`, `sendPhoto`, `sendDocument`). Transient failures (429,
5xx, transport errors) are thrown as `NotificationException` with `IsTransient == true`;
permanent API rejections (unknown chat, invalid payload) return a `Failed` result.

```csharp
using MailForge.Communication.Extensions;
using MailForge.Communication.Models;
using MailForge.Telegram;
using MailForge.Telegram.Extensions;

services.AddCommunication(builder => builder
    .UseEmailChannel()
    .UseTelegramChannel(new TelegramOptions { BotToken = "123:ABC" }));

INotificationSender sender = ...;

var result = await sender.SendAsync(new Notification(
    ChannelType.Telegram,
    new TelegramContent(
        chatId: "@ops-alerts",
        text: "<b>Build failed</b> on <i>main</i>",
        parseMode: TelegramParseMode.Html,
        keyboard: new TelegramKeyboard(
            TelegramKeyboardButton.WithUrl("Open build", "https://ci.example.com/builds/42")))));
```

- **Media** — `TelegramMedia.FromPhoto(url)`, `TelegramMedia.FromDocument(url)`, or
  `TelegramMedia.FromDocument(bytes, fileName)` (uploaded as multipart). When media is
  present, the message text becomes the caption.
- **Keyboard** — buttons are `WithUrl(text, url)` or `WithCallbackData(text, data)`; rows
  are conveyed as `TelegramKeyboard(button, ...)` or `TelegramKeyboard(rows)`.
- **Many chats** — `TelegramContent(chatIds, text, ...)` sends sequentially; a failure on
  a later chat is reported as `Failed` with a note of how many chats already received it.

`UseTelegramChannel(INotificationProvider)` registers the channel over an existing provider
— useful for custom providers or future failover chains.