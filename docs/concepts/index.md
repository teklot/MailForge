# Concepts

MailForge is split into focused concepts. Start with [Core](core.md) and [Providers](providers.md).

| Topic | Description |
|-------|-------------|
| [Core](core.md) | Message models, builders, typed emails, the sending pipeline, validation |
| [Communication](communication.md) | Channel-agnostic `INotificationSender` and the email channel adapter |
| [Templates](templates.md) | Inline `{{...}}` rendering and Razor template rendering |
| [Failover](failover.md) | Multi-provider delivery with policies and max attempts |
| [Providers](providers.md) | The eight provider packages: setup, auth, and capability notes |
| [Studio](studio.md) | Local capture inbox, web dashboard, and SMTP relay |
| [Testing](testing.md) | FakeEmailProvider, cancellation, and test patterns |
| [Provider Capabilities](capabilities.md) | The capability contract providers advertise |