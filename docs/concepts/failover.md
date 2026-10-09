# Failover

Multi-provider delivery keeps your app sending even when one provider degrades. Configure
failover with `UseFailover` on the builder:

```csharp
services.AddMailForge(builder => builder
    .UseDefaultFrom("noreply@example.com")
    .UseFailover(
        new ProviderFailoverRoute(
            new PostmarkEmailProvider(new PostmarkOptions { ServerToken = "..." }),
            FailoverPolicy.TransientOnly,
            maxAttempts: 2),
        new ProviderFailoverRoute(
            new SmtpEmailProvider(new SmtpOptions { Host = "smtp.example.com" }))));
```

## Routes

`ProviderFailoverRoute` ties a provider to a delivery policy:

- `Provider` — the `IEmailProvider` used for the route.
- `Policy` — `FailoverPolicy.TransientOnly` fails over only on transient failures;
  `FailoverPolicy.AnyFailure` fails over on permanent failures too.
- `MaxAttempts` — how many times a route is tried before the next route takes over
  (default 1).

`UseFailover` also accepts plain providers (`params IEmailProvider[]`, each with default
`TransientOnly` + 1 attempt) and a shared policy (`UseFailover(FailoverPolicy, params
IEmailProvider[])`). `FailoverEmailProvider` can be constructed directly as well.

## Semantics

- Providers are tried in `Precedence` order (registration order).
- A route with `MaxAttempts > 1` is re-attempted up to `MaxAttempts` times before moving on.
- **`TransientOnly` (default):** a transient `EmailException` is recorded and the chain
  continues (next attempt, then next route). A *permanent* `EmailException` is thrown
  immediately, and a provider that returns a permanent rejection is surfaced as a failed
  result — neither fails over.
- **`AnyFailure`:** permanent failures and non-success results are also recorded and the
  chain moves to the next route.
- Unexpected (non-`EmailException`) exceptions are treated as transient so the chain can recover.
- The first route that returns success wins; remaining routes are not tried.
- Cancellation (`CancellationToken`) aborts immediately.

### No promotion back

Routes are evaluated **per attempt** in precedence order. There is **no promotion** of a
message back to an earlier provider after it recovers — a degraded secondary does not
silently re-become primary for in-flight traffic. This keeps behavior deterministic and is
documented as a deliberate design decision; automatic promotion-on-recovery is a
non-feature.

### When all routes fail

`FailoverEmailProvider` throws an `EmailException` whose message aggregates every route
failure; `IsTransient` is `true` when any failure was retryable, so the retry middleware
can still treat the batch as retryable. Every attempt is passed through the audit sink when
one is configured, giving you the full try sequence per message.

## Failover vs. Retries

Retries (`WithRetries`) re-attempt the **same** provider on transient failures with
backoff. Failover moves across **different** providers. Configure both to get per-provider
resilience plus cross-provider redundancy:

```
attempt(provider A, 1) → attempt(provider A, 2) → attempt(provider A, 3)
    → attempt(provider B, 1) → attempt(provider B, 2) → ...
```