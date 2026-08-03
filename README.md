# MailForge — Transactional Communication for .NET

[![CI](https://github.com/teklot/MailForge/actions/workflows/ci.yml/badge.svg)](https://github.com/teklot/MailForge/actions/workflows/ci.yml)
[![NuGet Version](https://img.shields.io/nuget/v/MailForge.Core)](https://www.nuget.org/packages/MailForge.Core)
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
│       MailForge.Abstractions + MailForge.Core           │
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

1. **Layer 1 — Core Framework** (`MailForge.Abstractions`, `MailForge.Core`): Developer programming model, typed emails, pipeline, validation, and middleware abstractions (.NET Standard 2.0 compatible).
2. **Layer 2 — Provider SDK** (`MailForge.Smtp`, `MailForge.Resend`, `MailForge.AmazonSES`): Connectors handling authentication, external API communication, and delivery responses.
3. **Layer 3 — Local Studio** (`MailForge.Studio`): ASP.NET Core MVC + HTMX + Bootstrap web companion for local testing, email inspection, live preview, and message replay.
4. **Layer 4 — Server Platform** (`MailForge.Server`): Self-hosted Docker platform providing REST API submission, message queues, retry policies, template management, and admin dashboard.
5. **Layer 5 — Cloud Platform** (`MailForge.Cloud`): Managed SaaS platform offering multi-tenancy, usage-based billing (Stripe), domain authentication (SPF/DKIM/DMARC), and enterprise SLA.

## Packages

| Package | Description |
|---|---|
| **MailForge.Abstractions** | Provider-agnostic contracts: `IEmailProvider`, `IEmailSender`, `EmailMessage`, delivery status, and provider capability flags |
| **MailForge.Core** | Framework core: typed emails, middleware pipeline, validation, Razor/inline rendering, and DI integration |
| **MailForge.Smtp** | SMTP provider adapter for routing messages to any standard SMTP server |
| **MailForge.Resend** | Resend API provider adapter |
| **MailForge.AmazonSES** | Amazon Simple Email Service provider adapter |

## Installation

```shell
dotnet add package MailForge.Abstractions
dotnet add package MailForge.Core
dotnet add package MailForge.Smtp
dotnet add package MailForge.Resend
dotnet add package MailForge.AmazonSES
```

## Quick Start

Coming with Phase 1 (Framework MVP):

```csharp
services.AddMailForge(builder => builder
    .UseSmtp(smtp => smtp.Host("smtp.example.com").Port(587).Credentials(user, pass))
    .UseFakeForTesting());

var sender = services.GetRequiredService<IEmailSender>();
await sender.SendAsync(new WelcomeEmail(userModel));
```

## Repository Layout

```
MailForge.Abstractions/   Layer 1 contracts (IEmailProvider, IEmailSender, ...)
MailForge.Core/           Layer 1 framework (typed emails, pipeline, DI)
MailForge.Smtp/           Layer 2 SMTP provider
MailForge.Resend/         Layer 2 Resend provider
MailForge.AmazonSES/      Layer 2 Amazon SES provider
MailForge.Tests/          xUnit test suite
MailForge.Console/        Sample console application
```

## Supported Frameworks

- **.NET 10+**: Optimized for high-performance runtimes and modern C# idioms.
- **.NET Standard 2.0**: Broad compatibility across .NET 5+, .NET Core 2.0+, and .NET Framework 4.6.1+.

## Roadmap

```
Phase 0: Repository & CI/CD Setup (.NET Solution, NuGet pipeline)  ← current
Phase 1: Framework MVP (.NET Standard 2.0 Core, SMTP, Resend, Razor)
Phase 2: Studio Companion (ASP.NET Core MVC + HTMX + Bootstrap)
Phase 3: Server Self-Hosted Platform (.NET 10, REST API, Queue Worker, Postgres)
Phase 4: Cloud SaaS Commercial Launch (Multi-tenancy, Stripe Billing, DNS tooling)
Future:  Unified Communication Platform (Email, SMS, Push, Slack, Teams)
```

## License

Apache License 2.0. See [LICENSE](LICENSE).
