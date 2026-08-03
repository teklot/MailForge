using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MailForge.Abstractions;

namespace MailForge.Core.Validation
{
    /// <summary>
    /// The default validator. Checks that the message has a sender, at least one recipient,
    /// a subject, a body, and that all addresses are well formed.
    /// </summary>
    public sealed class DefaultEmailValidator : IEmailValidator
    {
        private static readonly Regex AddressPattern = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>Validates a message and returns the problems found.</summary>
        public Task<IReadOnlyList<ValidationProblem>> ValidateAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            var problems = new List<ValidationProblem>();

            if (message.From == null || string.IsNullOrWhiteSpace(message.From.Address))
                problems.Add(new ValidationProblem("MissingSender", "The message must have a sender address."));
            else if (!AddressPattern.IsMatch(message.From.Address))
                problems.Add(new ValidationProblem("InvalidSender", $"The sender address '{message.From.Address}' is not well formed."));

            if (message.Recipients == null || message.Recipients.Count == 0)
                problems.Add(new ValidationProblem("MissingRecipients", "The message must have at least one recipient."));

            if (message.Recipients != null)
            {
                foreach (var invalid in message.Recipients.Where(r => r.Address == null || !AddressPattern.IsMatch(r.Address.Address)))
                    problems.Add(new ValidationProblem("InvalidRecipient", $"The recipient address '{invalid.Address?.Address}' is not well formed."));
            }

            if (string.IsNullOrWhiteSpace(message.Subject))
                problems.Add(new ValidationProblem("MissingSubject", "The message must have a subject."));

            if (string.IsNullOrWhiteSpace(message.HtmlBody) && string.IsNullOrWhiteSpace(message.TextBody))
                problems.Add(new ValidationProblem("MissingBody", "The message must have an HTML or plain-text body."));

            return Task.FromResult<IReadOnlyList<ValidationProblem>>(problems);
        }
    }
}
