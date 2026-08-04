using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Templates;

namespace MailForge
{
    /// <summary>
    /// The default <see cref="IEmailSender"/> implementation. It renders typed emails, applies
    /// automatic plain-text generation, validates the message, and runs it through the
    /// configured middleware pipeline and provider.
    /// </summary>
    public sealed class EmailSender : IEmailSender
    {
        private readonly IEmailProvider _provider;
        private readonly IReadOnlyList<IEmailMiddleware> _middleware;
        private readonly IReadOnlyList<IEmailValidator> _validators;
        private readonly IEmailTemplateRenderer _templateRenderer;
        private readonly IInlineTemplateRenderer _inlineRenderer;
        private readonly ITemplateRegistry _templateRegistry;
        private readonly EmailAddress? _defaultFrom;
        private readonly bool _autoPlainText;

        /// <summary>Creates an email sender from its dependencies.</summary>
        public EmailSender(
            IEmailProvider provider,
            IEnumerable<IEmailMiddleware> middleware,
            IEnumerable<IEmailValidator> validators,
            IEmailTemplateRenderer templateRenderer,
            IInlineTemplateRenderer inlineRenderer,
            ITemplateRegistry templateRegistry,
            EmailAddress? defaultFrom = null,
            bool autoPlainText = true)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _middleware = (middleware ?? Enumerable.Empty<IEmailMiddleware>()).ToArray();
            _validators = (validators ?? Enumerable.Empty<IEmailValidator>()).ToArray();
            _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
            _inlineRenderer = inlineRenderer ?? throw new ArgumentNullException(nameof(inlineRenderer));
            _templateRegistry = templateRegistry ?? throw new ArgumentNullException(nameof(templateRegistry));
            _defaultFrom = defaultFrom;
            _autoPlainText = autoPlainText;
        }

        /// <summary>Sends a message defined inline.</summary>
        public Task<EmailDeliveryResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));
            return SendAsyncCore(email, cancellationToken);
        }

        /// <summary>Sends a strongly typed email.</summary>
        public async Task<EmailDeliveryResult> SendAsync<TModel>(Email<TModel> email, CancellationToken cancellationToken = default)
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));

            var message = await BuildMessageAsync(email, cancellationToken);
            return await SendAsyncCore(message, cancellationToken);
        }

        private async Task<EmailMessage> BuildMessageAsync<TModel>(Email<TModel> email, CancellationToken cancellationToken)
        {
            string? html = null;
            string? text = null;

            if (email.InlineHtml != null)
                html = _inlineRenderer.Render(email.InlineHtml, email.Model);
            if (email.InlineText != null)
                text = _inlineRenderer.Render(email.InlineText, email.Model);

            if (email.TemplateName != null)
            {
                var template = _templateRegistry.Get(email.TemplateName);
                html = _templateRenderer.Render(template, email.Model);
            }

            if (_autoPlainText && html != null && text == null)
                text = PlainTextGenerator.Generate(html);

            var subject = email.Subject == null ? null : _inlineRenderer.Render(email.Subject, email.Model);

            var recipients = new List<EmailRecipient>(email.To.Count + email.Cc.Count + email.Bcc.Count);
            recipients.AddRange(email.To);
            recipients.AddRange(email.Cc);
            recipients.AddRange(email.Bcc);

            var from = email.From ?? _defaultFrom
                ?? throw new InvalidOperationException("No sender address is configured. Set a default via UseDefaultFrom(...) or call SetFrom(...) on the email.");

            return new EmailMessage(
                from,
                recipients,
                subject,
                html,
                text,
                email.Priority,
                email.Attachments,
                email.Headers.ToDictionary(pair => pair.Key, pair => pair.Value),
                email.Tags.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        private async Task<EmailDeliveryResult> SendAsyncCore(EmailMessage message, CancellationToken cancellationToken)
        {
            foreach (var validator in _validators)
            {
                var problems = await validator.ValidateAsync(message, cancellationToken);
                if (problems != null && problems.Count > 0)
                    return EmailDeliveryResult.FailedValidation(message, string.Join("; ", problems));
            }

            var context = new EmailDeliveryContext(message);
            return await RunPipelineAsync(context, 0, cancellationToken);
        }

        private async Task<EmailDeliveryResult> RunPipelineAsync(EmailDeliveryContext context, int index, CancellationToken cancellationToken)
        {
            if (index < _middleware.Count)
                return await _middleware[index].InvokeAsync(context, () => RunPipelineAsync(context, index + 1, cancellationToken));

            cancellationToken.ThrowIfCancellationRequested();

            ProviderDeliveryResult providerResult;
            try
            {
                providerResult = await _provider.SendAsync(context.Message, cancellationToken);
            }
            catch (EmailException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new EmailException(
                    $"The provider '{_provider.Name}' failed to send the message: {exception.Message}",
                    exception,
                    isTransient: true);
            }

            return providerResult.Succeeded
                ? EmailDeliveryResult.Sent(context.Message, providerResult)
                : EmailDeliveryResult.Failed(context.Message, providerResult);
        }
    }
}
