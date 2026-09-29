# Migration Guide

This guide covers moving to MailForge **v1.0.0** from earlier releases.

## Pre-1.0 → v1.0.0

v1.0 is the first stable release. For consumers using the documented public API through
`AddMailForge` + `UseProvider`, **no source changes are required.** The changes below only
affect API surface that was publicly visible but never intended to be public.

### Public surface tightened

These were implementation details that leaked into the public API during the pre-1.0
releases. They are now **internal** (excluded from the frozen surface):

- `(Options, HttpClient)` constructors on the six HTTP providers — **Resend, Postmark,
  Mailgun, Brevo, ZeptoMail, AzureCS**. Construct with the options-only constructor
  (`new ResendEmailProvider(options)`) which creates the `HttpClient` internally. If you
  need an injected client for testing, use the internal seam via the test project's
  friend-assembly access.
- `SmtpMessageMapper` and `SesMessageMapper` — the shared `MimeMessageMapper` (in the core
  `MailForge` package) remains public and is the supported MIME conversion point.
- `PlainTextGenerator` — plain-text generation stays on through
  `EnableAutoPlainText`; the standalone utility type is no longer public.

### Behavior notes

- **No breaking behavior changes.** Retry, failover, validation, and rendering semantics
  are unchanged from the pre-1.0 releases.
- The default auto plain-text behavior is unchanged (enabled).

## Staying current

- All library assemblies share a single version (from `Directory.Build.props`).
- Additive, non-breaking changes land in minor releases.
- Breaking changes require a major version. Watch the release notes.