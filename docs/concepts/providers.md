# Providers

Eight provider packages implement `IEmailProvider`. Each accepts an options object with the
credentials and transport settings it needs. Register any of them with `UseProvider` (or a
factory) in `AddMailForge`.

```csharp
services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@example.com")
    .UseProvider(new ResendEmailProvider(new ResendOptions { ApiKey = "re_..." })));
```

## Capability Matrix

| Provider | Package | Attachments | Inline images | Headers | Tags |
|----------|---------|:-----------:|:-------------:|:-------:|:----:|
| SMTP | MailForge.Smtp | ✓ | ✓ | ✓ | ✓ |
| Amazon SES | MailForge.AmazonSES | ✓ | ✓ | ✓ | ✓ |
| Postmark | MailForge.Postmark | ✓ | ✓ | ✓ | ✓ |
| Mailgun | MailForge.Mailgun | ✓ | ✓ | ✓ | ✓ |
| Brevo | MailForge.Brevo | ✓ | ✓ | ✓ | ✓ |
| Resend | MailForge.Resend | ✓ | ❌ | ✓ | ✓ |
| ZeptoMail | MailForge.ZeptoMail | ✓ | ✓ | ❌ | ❌ |
| Azure Communication Services | MailForge.AzureCS | ✓ | ✓¹ | ✓ | ❌ |

¹ Azure CS has no `cid:` attachment mechanism; inline images are rewritten as `data:` URIs
in the HTML body.

## SMTP (`MailForge.Smtp`)

```csharp
var provider = new SmtpEmailProvider(new SmtpOptions
{
    Host = "smtp.example.com",
    Port = 587,
    SecureSocketOptions = SecureSocketOptions.StartTls,
    Username = "user",
    Password = "secret",
    LocalDomain = "local",
    TimeoutMilliseconds = 30000
});
```

Built on MailKit. Validation is deferred to the SMTP server, so invalid recipients surface
as delivery failures rather than validation errors.

## Amazon SES (`MailForge.AmazonSES`)

```csharp
var provider = new AmazonSesEmailProvider(new AmazonSesOptions
{
    Region = "us-east-1",
    AccessKey = "...",      // optional; the SDK default credential chain is used when absent
    SecretKey = "...",
    ConfigurationSetName = null,
    MaxErrorRetry = 3
});
```

Messages are sent through the raw-message API. Credentials fall back to the standard AWS
credential chain (`AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY`, profiles, IAM roles).

## Postmark (`MailForge.Postmark`)

```csharp
var provider = new PostmarkEmailProvider(new PostmarkOptions
{
    ServerToken = "server-token",
    MessageStream = "outbound"   // default
});
```

`BaseUri` and `Timeout` are configurable. Attachments use Postmark's inline attachment
format, so inline images work out of the box.

## Mailgun (`MailForge.Mailgun`)

```csharp
var provider = new MailgunEmailProvider(new MailgunOptions
{
    ApiKey = "key-...",
    Domain = "mg.example.com"   // required
});
```

The domain must be verified with Mailgun. `BaseUri` and `Timeout` are configurable.

## Brevo (`MailForge.Brevo`)

```csharp
var provider = new BrevoEmailProvider(new BrevoOptions { ApiKey = "xkeysib-..." });
```

`BaseUri` and `Timeout` are configurable.

## Resend (`MailForge.Resend`)

```csharp
var provider = new ResendEmailProvider(new ResendOptions { ApiKey = "re_..." });
```

Resend does not support inline images; mark the capabilities accordingly (see the matrix).
`BaseUri` and `Timeout` are configurable.

## Zoho ZeptoMail (`MailForge.ZeptoMail`)

```csharp
var provider = new ZeptoMailEmailProvider(new ZeptoMailOptions { SendApiKey = "..." });
```

Headers and tags are not supported by the ZeptoMail API; any `Headers`/`Tags` on a message
are dropped. `BaseUri` and `Timeout` are configurable.

## Azure Communication Services (`MailForge.AzureCS`)

```csharp
var provider = new AzureCSEmailProvider(new AzureCSOptions
{
    Endpoint = "https://my-resource.communication.azure.com",
    AccessKey = "base64-access-key",   // as it appears in the connection string
    SenderAddress = "noreply@verified-domain.com"
});
```

- Requests are signed with HMAC-SHA256; `AccessKey` is Base64-decoded before signing.
- The sender must match a domain verified for the resource — Azure does not allow arbitrary
  From addresses.
- Tags are not supported (dropped).

## Using a Provider with Dependency Injection

Providers are plain objects; register them yourself and supply a factory:

```csharp
services.AddSingleton(new ResendEmailProvider(new ResendOptions { ApiKey = Config.Resend }));

services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@example.com")
    .UseProvider(sp => sp.GetRequiredService<ResendEmailProvider>()));
```

See [Failover](failover.md) to route across multiple providers.