# API Reference

Browse the auto-generated API documentation for all MailForge packages.

## Packages

| Package | Description |
|---------|-------------|
| MailForge.Core (MailForge) | Models, builders, typed emails, templates, validation, retries, audit, middleware, failover, DI extensions |
| MailForge.Smtp | SMTP delivery via MailKit (`SmtpOptions`, `SmtpEmailProvider`) |
| MailForge.Resend | Resend API provider (`ResendOptions`, `ResendEmailProvider`) |
| MailForge.AmazonSES | Amazon SES raw-message provider (`AmazonSesOptions`, `AmazonSesEmailProvider`) |
| MailForge.Postmark | Postmark API provider (`PostmarkOptions`, `PostmarkEmailProvider`) |
| MailForge.Mailgun | Mailgun API provider (`MailgunOptions`, `MailgunEmailProvider`) |
| MailForge.Brevo | Brevo API provider (`BrevoOptions`, `BrevoEmailProvider`) |
| MailForge.ZeptoMail | Zoho ZeptoMail provider (`ZeptoMailOptions`, `ZeptoMailEmailProvider`) |
| MailForge.AzureCS | Azure Communication Services provider (`AzureCSOptions`, `AzureCSEmailProvider`) |

The API reference is generated with DocFX from XML doc comments. Internal helpers, mappers,
and test seams are intentionally excluded — they are not part of the supported surface.

> The v1.0 public API is frozen. Breaking changes require a major version.
> See [Architecture](../architecture.md#api-stability-policy).