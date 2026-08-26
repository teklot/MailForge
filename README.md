# MailForge — Transactional Communication for .NET

[![CI](https://github.com/teklot/MailForge/actions/workflows/ci.yml/badge.svg)](https://github.com/teklot/MailForge/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/MailForge)](https://www.nuget.org/packages/MailForge)
[![.NET](https://img.shields.io/badge/.NET-net10.0%20%7C%20netstandard2.0-blue)](https://dotnet.microsoft.com/)

MailForge is a unified, layered transactional communication ecosystem for .NET applications. It bridges the gap between lightweight email libraries and large third-party email providers by offering a provider-agnostic framework, a local developer studio, and a self-hosted gateway server.

The developer experience goal: a strongly typed, production-ready transactional email after installing a single package with minimal configuration.

```csharp
await emailSender.SendAsync(new WelcomeEmail(userModel));
```

**Guiding principle:** Provider-agnostic by design. Business logic never couples to a specific sending service.

## Why MailForge?

Sending transactional email in .NET usually means choosing between low-level plumbing and
vendor lock-in — and repeating the same boilerplate every time. Raw SMTP libraries make you
hand-assemble messages, templates, retries, and logging. Vendor SDKs work well but only for
that one vendor, so swapping providers rewrites your email classes. MailForge fills that gap:
one strongly typed programming model, a real delivery pipeline, and failover across providers —
without tying your business logic to any single service.

| | MailKit / SmtpClient | Vendor SDKs<br>(Resend, SendGrid, SES) | FluentEmail | **MailForge** |
|---|:---:|:---:|:---:|:---:|
| Typed emails (`Email<TModel>`) | ✗ | ✗ | ✗ | ✔ |
| Razor + inline templates | ✗ | ✗ | add-on | ✔ |
| Auto plain-text generation | ✗ | ✗ | ✗ | ✔ |
| Delivery pipeline (validation, logging, retries, audit) | ✗ | ✗ | ✗ | ✔ |
| Multi-provider failover with precedence | ✗ | ✗ | ✗ | ✔ |
| Provider capability contracts | ✗ | ✗ | ✗ | ✔ |
| Swap providers without changing email classes | ✗ | ✗ | ~ | ✔ |
| Zero-setup testing (`FakeEmailProvider`) | ✗ | ✗ | ✗ | ✔ |
| DI-first registration | ✗ | ✔ | ✔ | ✔ |

## How It Works

MailForge is structured into four progressive layers that evolve without architectural rewrites:

```
┌─────────────────────────────────────────────────────────┐
│                      Developer App                      │
└────────────────────────────┬────────────────────────────┘
                             ▼
┌─────────────────────────────────────────────────────────┐
│              Layer 1: MailForge Framework               │
│                 MailForge (core)                        │
└────────────────────────────┬────────────────────────────┘
                             │
       ┌─────────────────────┼─────────────────────┐
       ▼                     ▼                     ▼
┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│    Layer 2    │     │    Layer 3    │     │    Layer 4    │
│   Providers   │     │ MailForge     │     │ MailForge     │
│ SMTP · Resend │     │ Studio        │     │ Gateway       │
│   · AmazonSES │     │ (Local Dev)   │     │ REST API &    │
│               │     │               │     │ Failover Queue│
└───────┬───────┘     └───────────────┘     └───────┬───────┘
        │                                           │
        ▼                                           ▼
    Internet                                  Delivery Queue
```

1. **Layer 1 — Core Framework** (`MailForge`): Provider-agnostic contracts plus the developer programming model: typed emails, pipeline, validation, middleware, template rendering, and multi-provider failover with precedence (.NET Standard 2.0 compatible).
2. **Layer 2 — Provider SDK** (`MailForge.Smtp`, `MailForge.Resend`, `MailForge.AmazonSES`): Connectors handling authentication, external API communication, and delivery responses.
3. **Layer 3 — Local Studio** (`MailForge.Studio`): ASP.NET Core MVC + HTMX + Bootstrap web companion for local testing, email inspection, live preview, and message replay.
4. **Layer 4 — Gateway** (`MailForge.Server`): Self-hosted service exposing the failover engine over REST with an async delivery queue and normalized webhooks.

## Packages

| Package | Description |
|---|---|
| **MailForge** | Core framework + contracts: `IEmailProvider`, `IEmailSender`, `EmailMessage`, typed emails, middleware pipeline, validation, Razor/inline rendering, and DI integration |
| **MailForge.Smtp** | SMTP provider adapter for routing messages to any standard SMTP server |
| **MailForge.Resend** | Resend API provider adapter |
| **MailForge.AmazonSES** | Amazon Simple Email Service provider adapter |

## Installation

```shell
dotnet add package MailForge
dotnet add package MailForge.Smtp
dotnet add package MailForge.Resend
dotnet add package MailForge.AmazonSES
```

## Quick Start

Configure the framework once at startup, then send strongly typed emails:

```csharp
services.AddMailForge(builder => builder
    .UseProvider(new SmtpEmailProvider(new SmtpOptions { Host = "smtp.example.com", Port = 587 }))
    .UseDefaultFrom("noreply@example.com")
    .RegisterTemplate("welcome", "<h1>Welcome @Model.Name</h1>"));
```

Define a typed email:

```csharp
public sealed class WelcomeEmail : Email<WelcomeModel>
{
    public WelcomeEmail(WelcomeModel model) : base(model)
    {
        AddTo("user@example.com");
        SetSubject("Welcome {{Name}}!");
        UseTemplate("welcome"); // or SetHtml("...") for an inline template
        AddTag("purpose", "welcome");
    }
}
```

Send it:

```csharp
var sender = services.GetRequiredService<IEmailSender>();
var result = await sender.SendAsync(new WelcomeEmail(new WelcomeModel("Jane")));
```

Delivery is validated, logged, audited, and retried on transient failures through the
middleware pipeline before reaching the configured provider. For development and tests,
omit `UseProvider` to send through the in-memory `FakeEmailProvider`.

## Providers

Each provider ships in its own package and is registered on the builder the same way;
swap providers without touching your email classes.

| Provider | Package | Attachments | Inline images | Headers | Tags | Status |
|---|---|:---:|:---:|:---:|:---:|---|
| **SMTP** | `MailForge.Smtp` | ✔ | ✔ | ✔ | ✔¹ | Shipped |
| **Resend** | `MailForge.Resend` | ✔ | ✗ | ✔ | ✔ | Shipped |
| **Amazon SES** | `MailForge.AmazonSES` | ✔ | ✔ | ✔ | ✔ | Shipped |
| **Postmark** | `MailForge.Postmark` | ✔ | ✔ | ✔ | ✔ | Shipped |
| **Mailgun** | `MailForge.Mailgun` | ✔ | ✔ | ✔ | ✔ | Shipped |
| **Brevo** | `MailForge.Brevo` | ✔ | ✔ | ✔ | ✔ | Shipped |
| **Azure Communication Services** | `MailForge.AzureCommunicationServices` | ✗ | ✗ | ✗ | ✗ | Planned |
| **ZeptoMail (Zoho)** | `MailForge.ZeptoMail` | ✔ | ✔ | ✗ | ✗ | Shipped |

¹ SMTP has no native tags; they are sent as `X-MailForge-Tag-<key>` headers. Planned providers
are on the roadmap; capability support will be confirmed as each ships.

### SMTP

```csharp
using MailForge.Smtp;

services.AddMailForge(builder => builder.UseProvider(new SmtpEmailProvider(new SmtpOptions
{
    Host = "smtp.example.com",
    Port = 587,
    Username = "user@example.com",
    Password = "password"
})));
```

Routes through any standard SMTP server, with optional TLS (implicit or STARTTLS) and authentication.

### Resend

```csharp
using MailForge.Resend;

services.AddMailForge(builder => builder.UseProvider(new ResendEmailProvider(new ResendOptions
{
    ApiKey = "re_..."
})));
```

### Amazon SES

```csharp
using MailForge.AmazonSES;

services.AddMailForge(builder => builder.UseProvider(new AmazonSesEmailProvider(new AmazonSesOptions
{
    Region = "us-east-1",
    AccessKey = "AKIA...",
    SecretKey = "..."
})));
```

When `AccessKey`/`SecretKey` are omitted, the AWS SDK default credential chain is used.

### Postmark

```csharp
using MailForge.Postmark;

services.AddMailForge(builder => builder.UseProvider(new PostmarkEmailProvider(new PostmarkOptions
{
    ServerToken = "your-server-token"
})));
```

Authenticates via `X-Postmark-Server-Token` header. Supports message streams, tags, and custom headers.

### Mailgun

```csharp
using MailForge.Mailgun;

services.AddMailForge(builder => builder.UseProvider(new MailgunEmailProvider(new MailgunOptions
{
    ApiKey = "key-...",
    Domain = "mg.example.com"
})));
```

Authenticates via HTTP Basic auth (`api:{key}`). Sends as multipart/form-data with `o:tag` and `h:*` fields for tags and custom headers.

### Brevo

```csharp
using MailForge.Brevo;

services.AddMailForge(builder => builder.UseProvider(new BrevoEmailProvider(new BrevoOptions
{
    ApiKey = "xkeysib-..."
})));
```

Authenticates via `api-key` header. Supports tags and custom headers.

### ZeptoMail (Zoho)

```csharp
using MailForge.ZeptoMail;

services.AddMailForge(builder => builder.UseProvider(new ZeptoMailEmailProvider(new ZeptoMailOptions
{
    SendApiKey = "your-send-api-key"
})));
```

Authenticates via `Zoho-enczapikey` header. Tags and custom headers are not supported by the ZeptoMail API.

## Failover

A `FailoverEmailProvider` tries a precedence-ordered chain of providers until one succeeds,
so a transient outage at one provider does not lose the message. Providers are listed first
= tried first.

```csharp
using MailForge.Smtp;
using MailForge.Resend;

services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@example.com")
    .UseFailover(
        new SmtpEmailProvider(new SmtpOptions { Host = "smtp.example.com", Port = 587 }),
        new ResendEmailProvider(new ResendOptions { ApiKey = "re_..." })));
```

By default each route fails over only on transient failures (`FailoverPolicy.TransientOnly`)
with a single attempt. To control behavior per provider, use routes:

```csharp
builder.UseFailover(
    new ProviderFailoverRoute(new SmtpEmailProvider(new SmtpOptions { Host = "smtp.example.com" })),
    new ProviderFailoverRoute(new ResendEmailProvider(new ResendOptions { ApiKey = "re_..." }),
        FailoverPolicy.AnyFailure, maxAttempts: 2));
```

- `FailoverPolicy.TransientOnly` — fails over on timeouts, rate limits, and temporary errors; permanent failures and rejections surface as-is.
- `FailoverPolicy.AnyFailure` — fails over on any failure, including permanent exceptions and provider rejections.
- `maxAttempts` — attempts allowed on the same provider before failing over (minimum 1).

When every route fails, the provider throws an `EmailException` whose `IsTransient` is true if
any failure was retryable. On success, `ProviderDeliveryResult.Details` records which provider
delivered the message.

## Live Testing

The sample console app includes live integration checks that send a real message through a
provider. They are handy for verifying a provider against [smtp4dev](https://github.com/rnwood/smtp4dev)
or a real service before relying on it in production.

```shell
# SMTP (smtp4dev on localhost:25, or any SMTP server)
dotnet run --project MailForge.Console -- live-smtp [host] [port] [to]

# Resend (API key as argument or RESEND_API_KEY)
dotnet run --project MailForge.Console -- live-resend [apiKey] [to]

# Amazon SES (region as argument or AWS_REGION; credentials from the default AWS chain)
dotnet run --project MailForge.Console -- live-ses [region] [to]
```

Environment variables honored by the live tests:

| Variable | Purpose |
|---|---|
| `RESEND_API_KEY` | Resend API key (also accepted as an argument) |
| `AWS_REGION` | SES region (also accepted as an argument) |
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` | SES credentials |
| `LIVE_FROM` | Sender address (default `sender@example.com`) |
| `LIVE_TO` | Recipient address (default `recipient@example.com`) |

## Repository Layout

```
MailForge/              Layer 1 core framework + contracts (typed emails, pipeline, DI)
MailForge.Smtp/         Layer 2 SMTP provider
MailForge.Resend/       Layer 2 Resend provider
MailForge.AmazonSES/    Layer 2 Amazon SES provider
MailForge.Tests/        xUnit test suite
MailForge.Console/      Sample console application
```

## Supported Frameworks

- **.NET 10+**: Optimized for high-performance runtimes and modern C# idioms.
- **.NET Standard 2.0**: Broad compatibility across .NET 5+, .NET Core 2.0+, and .NET Framework 4.6.1+.

