using System.IO;
using MailForge.Models;
using MailForge.Providers;
using MailForge.Shared;
using MimeKit;

namespace MailForge.AmazonSES
{
    /// <summary>
    /// Converts a MailForge <see cref="EmailMessage"/> into a MIME message and serializes it to
    /// the byte stream required by the Amazon SES raw-message API.
    /// </summary>
    public static class SesMessageMapper
    {
        /// <summary>Builds a MIME message from a MailForge message.</summary>
        public static MimeMessage ToMimeMessage(EmailMessage message) =>
            MimeMessageMapper.ToMimeMessage(message);

        /// <summary>Serializes a MailForge message to the raw MIME bytes accepted by Amazon SES.</summary>
        public static byte[] ToRawMessage(EmailMessage message)
        {
            var mime = MimeMessageMapper.ToMimeMessage(message);
            using (var stream = new MemoryStream())
            {
                mime.WriteTo(stream);
                return stream.ToArray();
            }
        }
    }
}
