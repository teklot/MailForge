using MailForge.Models;
using MailForge.Providers;
using MailForge.Shared;
using MimeKit;

namespace MailForge.Smtp
{
    /// <summary>
    /// Converts a MailForge <see cref="EmailMessage"/> into a MimeKit <see cref="MimeMessage"/>.
    /// </summary>
    public static class SmtpMessageMapper
    {
        /// <summary>Builds a MIME message from a MailForge message.</summary>
        public static MimeMessage ToMimeMessage(EmailMessage message) =>
            MimeMessageMapper.ToMimeMessage(message);
    }
}
