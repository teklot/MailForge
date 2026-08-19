using MailForge.Interfaces;
using MailForge.Models;
using MailForge.Templates;
using MailForge.Validation;

namespace MailForge.Tests
{
    public class CancellationTests
    {
        private sealed class TokenCapturingProvider : IEmailProvider
        {
            public string Name => "TokenCapture";
            public ProviderCapabilities Capabilities => ProviderCapabilities.All;
            public CancellationToken? ReceivedToken { get; private set; }

            public Task<ProviderDeliveryResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
            {
                ReceivedToken = cancellationToken;
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(ProviderDeliveryResult.Success("ok"));
            }
        }

        private sealed class TokenCapturingMiddleware : IEmailMiddleware
        {
            public CancellationToken? ReceivedToken { get; private set; }

            public Task<EmailDeliveryResult> InvokeAsync(EmailDeliveryContext context, NextEmailHandler next)
            {
                ReceivedToken = TestContext.Current.CancellationToken;
                return next();
            }
        }

        private static EmailMessage CreateMessage() =>
            EmailMessage.Create()
                .From("a@example.com")
                .To("b@example.com")
                .Subject("s")
                .Text("t")
                .Build();

        [Fact]
        public async Task SendAsync_PassesTokenToProvider()
        {
            var provider = new TokenCapturingProvider();
            var cts = new CancellationTokenSource();
            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: new TemplateRegistry());

            await sender.SendAsync(CreateMessage(), cts.Token);

            Assert.Equal(cts.Token, provider.ReceivedToken);
        }

        [Fact]
        public async Task SendAsync_ThrowsOnPreCancelledToken()
        {
            var provider = new TokenCapturingProvider();
            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: new TemplateRegistry());

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                sender.SendAsync(CreateMessage(), cts.Token));
        }

        [Fact]
        public async Task SendAsync_TypedEmail_ThrowsOnPreCancelledToken()
        {
            var provider = new TokenCapturingProvider();
            var sender = new EmailSender(
                provider,
                middleware: Array.Empty<IEmailMiddleware>(),
                validators: new IEmailValidator[] { new DefaultEmailValidator() },
                templateRenderer: new RazorEmailTemplateRenderer(),
                inlineRenderer: new InlineTemplateRenderer(),
                templateRegistry: new TemplateRegistry());

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var email = new SimpleEmail();
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                sender.SendAsync(email, cts.Token));
        }

        private sealed class SimpleEmail : Email<object>
        {
            public SimpleEmail() : base(new object())
            {
                SetFrom("a@example.com");
                AddTo("b@example.com");
                SetSubject("s");
                SetText("t");
            }
        }
    }
}
