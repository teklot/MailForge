namespace MailForge.Abstractions
{
    /// <summary>A single validation problem found on an <see cref="EmailMessage"/>.</summary>
    public sealed class ValidationProblem
    {
        /// <summary>A short machine-readable code (for example, "NoRecipients").</summary>
        public string Code { get; }

        /// <summary>A human-readable description of the problem.</summary>
        public string Message { get; }

        /// <summary>Creates a validation problem.</summary>
        public ValidationProblem(string code, string message)
        {
            Code = code;
            Message = message;
        }

        /// <summary>Returns a string representation of this problem.</summary>
        public override string ToString() => $"[{Code}] {Message}";
    }
}
