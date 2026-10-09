# Provider Capabilities

Every `IEmailProvider` advertises a `ProviderCapabilities` descriptor. The framework and
your code can read it to decide what a message can safely contain before sending.

## The Contract

| Flag | Meaning |
|------|---------|
| `SupportsAttachments` | Files can be attached to a message |
| `SupportsInlineImages` | Inline images (`cid:` references) are supported |
| `SupportsHeaders` | Arbitrary custom headers are preserved |
| `SupportsTags` | Key/value analytics tags are supported |
| `RequiresBothBodies` | The provider is limited to HTML-*or* text-only messages |

`ProviderCapabilities.All` enables everything. `ProviderCapabilities.BodyOnly` describes a
provider that only sends text/HTML bodies (no attachments, inline images, headers, or tags).

A capability that a provider lacks is **not delivered** — for example, headers and tags on
a message are dropped by the ZeptoMail provider, and the Azure CS provider rewrites inline
images as `data:` URIs because it has no `cid:` mechanism.

## Per-Provider Values

| Provider | Attachments | Inline images | Headers | Tags |
|----------|:-----------:|:-------------:|:-------:|:----:|
| SMTP | ✓ | ✓ | ✓ | ✓ |
| Amazon SES | ✓ | ✓ | ✓ | ✓ |
| Postmark | ✓ | ✓ | ✓ | ✓ |
| Mailgun | ✓ | ✓ | ✓ | ✓ |
| Brevo | ✓ | ✓ | ✓ | ✓ |
| Resend | ✓ | ❌ | ✓ | ✓ |
| ZeptoMail | ✓ | ✓ | ❌ | ❌ |
| Azure Communication Services | ✓ | ✓¹ | ✓ | ❌ |
| FakeEmailProvider (dev) | ✓ | ✓ | ✓ | ✓ |

¹ `data:` URI rewrite of inline images.

A `FailoverEmailProvider` combines its routes' capabilities with a logical AND: a message
that any single provider in the chain cannot carry is not safe to route, because failover
may land on the weaker provider.

## Contract scope

The contract is limited to these five flags. These are deliberately **not** part of it
(documented, not implemented):

- **Templates** — template rendering is a framework feature (`ITemplateRegistry`), not a
  provider capability.
- **Delivery tracking / opens / clicks** — `SupportsTracking` was considered and excluded;
  delivery status comes from `ProviderDeliveryResult` only.
- **Encryption middleware** — email-content encryption is planned for a future major release.
- **Automatic promotion back** in failover — see [Failover](failover.md).

Adding anything to the contract would be a breaking change (a major release).