# Provider Verification

Live, real-network checks are an explicit release gate for every release. Each `live-*`
command in the console app sends a real message through a provider and prints `Succeeded`,
the provider message id, and details. Automated integration tests already cover every HTTP
provider against a local mock server; the `live-*` commands add the real network send, and
per-release results are published in the release notes rather than tracked in this page.

## The Commands

Run any command from the repository root:

```bash
dotnet run --project MailForge.Console -- <command> [args]
```

| Provider | Command | Arguments | Env vars (fallback) |
|----------|---------|-----------|-----------------------|
| SMTP | `live-smtp` | `[host] [port] [to]` | — |
| Resend | `live-resend` | `[apiKey] [to]` | `RESEND_API_KEY` |
| Amazon SES | `live-ses` | `[region] [to]` | `AWS_REGION`, `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` |
| Postmark | `live-postmark` | `[serverToken] [to]` | `POSTMARK_SERVER_TOKEN` |
| Mailgun | `live-mailgun` | `[apiKey] [domain] [to]` | `MAILGUN_API_KEY`, `MAILGUN_DOMAIN` |
| Brevo | `live-brevo` | `[apiKey] [to]` | `BREVO_API_KEY` |
| ZeptoMail | `live-zeptomail` | `[sendApiKey] [to]` | `ZEPTOMAIL_SEND_API_KEY` |
| Azure CS | `live-azurecs` | `[endpoint] [accessKey] [sender] [to]` | `AZURE_COMMUNICATION_ENDPOINT`, `AZURE_COMMUNICATION_ACCESS_KEY`, `AZURE_COMMUNICATION_SENDER` |
| Telegram | `live-telegram` | `[botToken] [chatId]` | `TELEGRAM_BOT_TOKEN`, `TELEGRAM_CHAT_ID` |

**Recipient / sender defaults:** `LIVE_TO` picks the recipient for every command (default
`recipient@example.com`); `LIVE_FROM` picks the sender (default `sender@example.com`).
Set either environment variable to override the default for a run.

Every email test message carries: subject, HTML + text bodies, one tag (`purpose=live-test`),
and one text attachment (`note.txt`). The Telegram command sends a plain-text message; the
attachment and tag details apply to the email commands.

### Local SMTP (offline)

For the SMTP command without a real server, use [smtp4dev](https://github.com/rnwood/smtp4dev):

```bash
dotnet run --project MailForge.Console -- live-smtp 127.0.0.1 25 you@example.com
```

Expect `Succeeded: True` and a message id. SMTP has no provider-level validation; confirm
delivery by inspecting the smtp4dev web UI.

## Known Provider Notes

- **SMTP:** validation is deferred to the server; a rejected recipient reports a delivery
  failure, not a validation error.
- **ZeptoMail / Azure CS:** headers and tags are dropped by the provider (see
  [Provider Capabilities](concepts/capabilities.md)).
- **Resend:** inline images are not supported; mark capabilities accordingly.
- **Azure CS:** the sender must be verified for the resource, and the access key is
  Base64-decoded before HMAC signing.
- **SES:** credentials fall back to the standard AWS credential chain when keys are absent.
- **Telegram:** the bot must have started a conversation with the chat (or be an admin of a
  channel) before it can send; get the chat id from a message the user sends the bot.

## Release Checklist

1. Full CI green on ubuntu-latest and windows-latest (Debug locally; Release in CI only).
2. `dotnet pack` builds all eleven packages; inspect each nupkg for README, LICENSE.
3. `docfx metadata` + `docfx build` succeed with zero warnings; generated API reflects the
   supported surface (no internal members).
4. All `live-*` commands return `Succeeded` against real accounts; the automated
   integration suite stays green; results published in the release notes.
5. Tag the release — the publish workflow pushes packages and the docs site deploys on `main`.