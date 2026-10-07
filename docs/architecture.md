# Architecture

## Overview

MailForge is a layered framework:

```
Developer App → Communication → Layer 1: Framework → Layer 2: Provider SDK · MailForge Studio → Internet
```

**Communication layer (v1.1.0).** `MailForge.Communication` adds a channel-agnostic
`INotificationSender` on top of `Layer 1`. Channels deliver through the framework: the
`EmailChannel` adapter delegates to `IEmailSender`, so email behavior is unchanged. See
[Communication](concepts/communication.md).

**Layer 1 — Framework.** The core pipeline: message models and builders, typed emails,
template rendering, validation, retries, auditing, middleware, and failover. You interact
with a single `IEmailSender`.

**Layer 2 — Provider SDK.** One adapter per email provider, exposed as an
`IEmailProvider`. Each provider translates an `EmailMessage` into the provider's wire
format and reports a `ProviderDeliveryResult` (success, provider message id, details).

**MailForge Studio.** A local development inbox that captures messages through an
`IEmailProvider` of its own, stores them in SQLite, and serves a web dashboard plus an
SMTP relay. It never talks to the network.

```
┌───────────────────────────────────────────────────────────────────────────┐
│  Developer App                                                            │
│      │                                                                    │
│      ▼                                                                    │
│  COMMUNICATION  INotificationSender (MailForge.Communication, v1.1.0)     │
│      │  routes Notification → channel → middleware                       │
│      ▼                                                                    │
│  LAYER 1  IEmailSender                                                    │
│      │  template render → validation → retry → logging → audit → your mw  │
│      ▼                                                                    │
│  LAYER 2  IEmailProvider (SMTP · Resend · SES · Postmark · Mailgun ·      │
│           Brevo · ZeptoMail · AzureCS)  ·  MailForge Studio (local)       │
│              │                                                            │
│              └──────────────► Internet ───────────────────────────►       │
└───────────────────────────────────────────────────────────────────────────┘
```

## Delivery Pipeline

When you call `IEmailSender.SendAsync`, `EmailSender` runs the message through a fixed
pipeline before handing it to the active provider:

```
Render body (template or inline content)
    → validate (DefaultEmailValidator + your validators)
    → RetryEmailMiddleware
    → LoggingEmailMiddleware
    → AuditEmailMiddleware (when configured)
    → your middleware (AddMiddleware, in registration order)
    → provider.SendAsync
```

- **Render** resolves `Email<TModel>` bodies through the template renderers.
- **Validation** failures short-circuit before any attempt.
- **Retry** re-attempts transient failures (`EmailException.IsTransient == true`) up to the
  configured attempt count (`WithRetries`, default 3) with an exponential base delay.
- **Logging** writes a structured log line per outcome when an `ILogger` is available.
- **Audit** records every attempt through `IEmailAuditSink.RecordAsync` when
  `UseAuditSink` is configured.

## Failover

`UseFailover` wraps multiple providers. A message is attempted on each `ProviderFailoverRoute`
in precedence order until one succeeds or the routes are exhausted. Routes carry a
`FailoverPolicy` (`TransientOnly` or `AnyFailure`) and a per-route `MaxAttempts`.

Current semantics: routes are evaluated **per attempt** in precedence order; there is **no
promotion back** to an earlier provider after it recovers. See
[Failover](concepts/failover.md) for the exact rules and worked examples.

## Provider Capabilities

Providers advertise what they support through `ProviderCapabilities` — attachments, inline
images, custom headers, tags, and whether both bodies are required. The framework reads
these to guide what you can safely send through a given provider. See
[Provider Capabilities](concepts/capabilities.md).

## API Stability Policy

From v1.0.0 the public API is **frozen**:

- Additive changes (new members, new packages) are allowed in minor releases.
- **Any breaking change to a public signature requires a major version bump.**
- Public surface is exactly what the generated API reference shows; internal helpers
  (mappers, plain-text generation, test seams) are excluded and not supported for consumers.

## Dependency Injection

`AddMailForge(Action<MailForgeBuilder>)` registers:

- `IEmailSender` (singleton) with the configured pipeline
- `IEmailProvider` (singleton; `FakeEmailProvider` when none is configured)
- `ITemplateRegistry`, `IInlineTemplateRenderer`, `IEmailTemplateRenderer`
- every validator and middleware you add via the builder

The builder methods: `UseProvider`, `UseDefaultFrom`, `EnableAutoPlainText`, `WithRetries`,
`UseAuditSink`, `RegisterTemplate`, `AddValidator`, `AddMiddleware`, `UseFailover`.

## Communication

`AddCommunication(Action<CommunicationBuilder>)` (from `MailForge.Communication`) registers
`INotificationSender`, `IChannelRegistry`, and your channels on top of the existing
registration. The `CommunicationBuilder` methods: `UseChannel`, `UseChannel(factory)`,
`UseEmailChannel`, `AddMiddleware`. Email stays the single shipping channel — see
[Communication](concepts/communication.md).