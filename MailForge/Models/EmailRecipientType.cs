namespace MailForge.Models
{
    /// <summary>Specifies the role of a recipient on an email message.</summary>
    public enum EmailRecipientType
    {
        /// <summary>The primary recipient.</summary>
        To = 0,

        /// <summary>A carbon-copy recipient.</summary>
        Cc = 1,

        /// <summary>A blind-carbon-copy recipient.</summary>
        Bcc = 2
    }
}
