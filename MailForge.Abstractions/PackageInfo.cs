namespace MailForge.Abstractions
{
    /// <summary>
    /// Phase 0 marker for the MailForge.Abstractions package.
    /// The shared abstraction contracts (IEmailProvider, IEmailSender, EmailMessage, ...)
    /// land here with the Phase 1 framework MVP.
    /// </summary>
    public static class PackageInfo
    {
        /// <summary>The canonical NuGet package identifier.</summary>
        public const string Name = "MailForge.Abstractions";
    }
}
