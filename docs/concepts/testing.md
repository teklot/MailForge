# Testing

MailForge is designed to be testable without network access.

## FakeEmailProvider

`FakeEmailProvider` never touches the network. It records every message sent through it
(`SentMessages`) and clears with `Clear()`:

```csharp
var provider = new FakeEmailProvider();
await provider.SendAsync(message);

Assert.Single(provider.SentMessages);
Assert.Equal("Welcome to Acme", provider.SentMessages[0].Subject);
```

It advertises `ProviderCapabilities.All`, so nothing is dropped during capture.

## Pipeline Tests

Build the real pipeline against the fake provider and drive it through `IEmailSender`:

```csharp
var services = new ServiceCollection();
services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@example.com")
    .UseProvider(new FakeEmailProvider())
    .RegisterTemplate("receipt", "..."));

await using var provider = services.BuildServiceProvider();
var sender = provider.GetRequiredService<IEmailSender>();

var result = await sender.SendAsync(new ReceiptEmail(new ReceiptModel(...)));
Assert.Equal(EmailDeliveryStatus.Sent, result.Status);
```

This exercises rendering, validation, retries, and middleware without any provider.

## Provider Adapters

For provider-specific behavior, adapters accept an injected transport through internal
constructors visible to the test assembly:

- SMTP: `SmtpEmailProvider(SmtpOptions, ISmtpClient)`
- Amazon SES: `AmazonSesEmailProvider(AmazonSesOptions, IAmazonSimpleEmailServiceV2)`
- HTTP providers: `XxxEmailProvider(XxxOptions, HttpClient)` — supply a stub/mock
  `HttpClient` handler and assert on the request.

These seams are internal to the shipping packages; use them through the test project only.

Example with a stub `HttpMessageHandler`:

```csharp
var client = new HttpClient(new StubHandler(r => r.RequestUri!.Host == "api.resend.com"));
var provider = new ResendEmailProvider(new ResendOptions { ApiKey = "re_test" }, client);
var result = await provider.SendAsync(message);
Assert.True(result.Succeeded);
```

## Cancellation

Everything is cancellable end to end. Pass a `CancellationToken` to `SendAsync`; the
token aborts provider attempts and failover loops. In tests, a pre-cancelled token makes
`TaskCanceledException`/`OperationCanceledException` path easy to assert.

## Best Practices

- Use `FakeEmailProvider` for logic tests and the internal seams for provider-contract tests.
- Keep provider tests hermetic: never require live credentials. Live verification is a
  separate, documented release gate — see [Provider Verification](../provider-verification.md).
- Assert on `EmailDeliveryResult.Status`, not just `Succeeded`, to catch validation
  short-circuits.