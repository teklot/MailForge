# MailForge Studio

MailForge Studio is a local development inbox. It captures every message your pipeline
sends — through its own `IEmailProvider` — into a SQLite store, renders a web dashboard,
and exposes an SMTP relay you can point any mail tool at. Nothing leaves your machine.

```
App pipeline → StudioEmailProvider → SQLite store → Dashboard (localhost) · SMTP relay
```

## Wiring

`AddMailForgeStudio` registers the capture store, the `StudioEmailProvider`, and optionally
the SMTP relay. Point the MailForge pipeline at the Studio provider:

```csharp
services.AddMailForgeStudio(options =>
{
    options.DatabasePath = "mailforge-studio.db";
    options.EnableSmtpRelay = true;   // default
    options.SmtpRelayPort = 2525;     // 0 picks an ephemeral port
});

services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@mailforge.dev")
    .UseProvider(sp => sp.GetRequiredService<StudioEmailProvider>()));
```

Everything the pipeline "sends" lands in the local inbox instead of the internet.

## Configuration (`StudioCaptureOptions`)

| Option | Default | Purpose |
|--------|---------|---------|
| `DatabasePath` | `mailforge-studio.db` | SQLite file; delete it to reset the inbox (schema rebuilds on next use) |
| `EnableSmtpRelay` | `true` | Run the local SMTP listener |
| `SmtpRelayHost` | `127.0.0.1` | Relay bind address |
| `SmtpRelayPort` | `2525` | Relay port; `0` picks an ephemeral port |
| `BuildConnectionString()` | — | SQLite connection string (pooling disabled so the file is never locked) |

## Storage Model

`IStudioCaptureStore` exposes the inbox: `CapturedMessage` (from, subject, html/text bodies,
priority, timestamps), plus `CapturedRecipient`, `CapturedAttachment`, and `CapturedHeader`
children. The `StudioEmailProvider` stores the `EmailMessage` faithfully, and
`MimeMessageConverter` round-trips raw MIME for SMTP-relay captures and exports.

## Web Dashboard

`StudioWebHost.Build(StudioWebOptions)`, or run it directly with `StudioWebHost.StartAsync`.
It serves an HTML dashboard (`/`, `/messages/{id}`) and a JSON API grouped at
`/api/messages`:

| Route | Method | Purpose |
|-------|--------|---------|
| `/api/messages/` | GET | Paginated inbox list (`StudioMessageQuery` search/filter) |
| `/api/messages/{id}` | GET | Message detail |
| `/api/messages/{id}/export?format=eml\|html\|json` | GET | Export in the given format (`StudioMimeExporter`) |
| `/api/messages/{id}/raw` | GET | Regenerated raw MIME source (`.eml`) |
| `/api/messages/{id}/attachments/{attachmentId}` | GET | Attachment download |
| `/api/messages/{id}/replay` | POST | Re-send a captured message via SMTP (`MessageReplayer`) |

Replay SMTP targets are configured in `StudioWebOptions` (`SmtpHost`, `SmtpPort`,
`TimeoutMilliseconds`).

## Running the Demo

The console app has two harnesses (no provider credentials needed):

```bash
dotnet run --project MailForge.Console -- studio-demo     # sends samples + prints inbox
dotnet run --project MailForge.Console -- studio-web      # dashboard on http://127.0.0.1:5000
```

`studio-web [port] [dbPath] [relayPort]` — the relay listens on `relayPort` (default 2525;
pass `off` to disable it). When `dbPath` is omitted both commands share a temporary
`mailforge-studio.db`, so `studio-demo` output shows up in the dashboard.