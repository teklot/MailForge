# MailForge — Transactional Communication for .NET

[![CI](https://github.com/teklot/MailForge/actions/workflows/ci.yml/badge.svg)](https://github.com/teklot/MailForge/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/MailForge)](https://www.nuget.org/packages/MailForge)
[![.NET](https://img.shields.io/badge/.NET-net10.0%20%7C%20netstandard2.0-blue)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue)](LICENSE)

MailForge is a unified, layered transactional communication ecosystem for .NET applications. It bridges the gap between lightweight email libraries and large third-party email providers by offering a provider-agnostic framework, a local developer studio, a self-hosted server platform, and a managed cloud SaaS platform.

The developer experience goal: a strongly typed, production-ready transactional email after installing a single package with minimal configuration.

```csharp
await emailSender.SendAsync(new WelcomeEmail(userModel));
```

**Guiding principle:** Provider-agnostic by design. Business logic never couples to a specific sending service.

## How It Works

MailForge is structured into five progressive layers that evolve without architectural rewrites:

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
│    Layer 2    │     │    Layer 3    │     │ Layer 4 & 5   │
│   Providers   │     │ MailForge     │     │ Server / Cloud│
│ SMTP · Resend │     │ Studio        │     │ REST API &    │
│   · AmazonSES │     │ (Local Dev)   │     │ Queue Workers │
└───────┬───────┘     └───────────────┘     └───────┬───────┘
        │                                           │
        ▼                                           ▼
    Internet                                  Delivery Queue
```

1. **Layer 1 — Core Framework** (`MailForge`): Provider-agnostic contracts plus the developer programming model: typed emails, pipeline, validation, middleware, and template rendering (.NET Standard 2.0 compatible).
2. **Layer 2 — Provider SDK** (`MailForge.Smtp`, `MailForge.Resend`, `MailForge.AmazonSES`): Connectors handling authentication, external API communication, and delivery responses.
3. **Layer 3 — Local Studio** (`MailForge.Studio`): ASP.NET Core MVC + HTMX + Bootstrap web companion for local testing, email inspection, live preview, and message replay.
4. **Layer 4 — Server Platform** (`MailForge.Server`): Self-hosted Docker platform providing REST API submission, message queues, retry policies, template management, and admin dashboard.
5. **Layer 5 — Cloud Platform** (`MailForge.Cloud`): Managed SaaS platform offering multi-tenancy, usage-based billing (Stripe), domain authentication (SPF/DKIM/DMARC), and enterprise SLA.

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
        To("user@example.com");
        Subject("Welcome {{Name}}!");
        Template("welcome"); // or Html("...") for an inline template
        Tag("purpose", "welcome");
    }
}
```

Send it:

```csharp
var sender = services.GetRequiredService<IEmailSender>();
var result = await sender.SendAsync(new WelcomeEmail(new WelcomeModel { Name = "Jane" }));
```

Delivery is validated, logged, audited, and retried on transient failures through the
middleware pipeline before reaching the configured provider. For development and tests,
omit `UseProvider` to send through the in-memory `FakeEmailProvider`.

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

## Roadmap

```
Phase 0: Repository & CI/CD Setup (.NET Solution, NuGet pipeline)
Phase 1: Framework MVP (.NET Standard 2.0 Core, SMTP, Resend, Razor)  ← current
Phase 2: Studio Companion (ASP.NET Core MVC + HTMX + Bootstrap)
Phase 3: Server Self-Hosted Platform (.NET 10, REST API, Queue Worker, Postgres)
Phase 4: Cloud SaaS Commercial Launch (Multi-tenancy, Stripe Billing, DNS tooling)
Future:  Unified Communication Platform (Email, SMS, Push, Slack, Teams)
```

## License

Apache License 2.0. See [LICENSE](LICENSE).
