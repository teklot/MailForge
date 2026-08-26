using System.Reflection;
using MailForge.AmazonSES;
using MailForge.Brevo;
using MailForge.Mailgun;
using MailForge.Models;
using MailForge.Postmark;
using MailForge.Resend;
using MailForge.Smtp;
using MailForge.ZeptoMail;

namespace MailForge.Tests
{
    public class PackageSmokeTests
    {
        [Theory]
        [InlineData(typeof(EmailMessage), "MailForge")]
        [InlineData(typeof(SmtpEmailProvider), "MailForge.Smtp")]
        [InlineData(typeof(ResendEmailProvider), "MailForge.Resend")]
        [InlineData(typeof(AmazonSesEmailProvider), "MailForge.AmazonSES")]
        [InlineData(typeof(PostmarkEmailProvider), "MailForge.Postmark")]
        [InlineData(typeof(BrevoEmailProvider), "MailForge.Brevo")]
        [InlineData(typeof(ZeptoMailEmailProvider), "MailForge.ZeptoMail")]
        [InlineData(typeof(MailgunEmailProvider), "MailForge.Mailgun")]
        public void PackageMarker_MatchesPackageId(Type markerType, string expectedPackageId)
        {
            Assert.Equal(expectedPackageId, markerType.Assembly.GetName().Name);
        }

        [Theory]
        [InlineData(typeof(EmailMessage))]
        [InlineData(typeof(EmailSender))]
        [InlineData(typeof(SmtpEmailProvider))]
        [InlineData(typeof(ResendEmailProvider))]
        [InlineData(typeof(AmazonSesEmailProvider))]
        [InlineData(typeof(PostmarkEmailProvider))]
        [InlineData(typeof(BrevoEmailProvider))]
        [InlineData(typeof(ZeptoMailEmailProvider))]
        [InlineData(typeof(MailgunEmailProvider))]
        public void LibraryAssemblies_AreVersionedFromDirectoryBuildProps(Type markerType)
        {
            var version = markerType.Assembly.GetName().Version;
            Assert.NotNull(version);
            Assert.StartsWith("0.3.0.", version.ToString());
        }

        [Fact]
        public void Solution_ReferencesAllLibraries()
        {
            var assemblies = new[]
            {
                Assembly.GetAssembly(typeof(EmailMessage)),
                Assembly.GetAssembly(typeof(EmailSender)),
                Assembly.GetAssembly(typeof(SmtpEmailProvider)),
                Assembly.GetAssembly(typeof(ResendEmailProvider)),
                Assembly.GetAssembly(typeof(AmazonSesEmailProvider)),
                Assembly.GetAssembly(typeof(PostmarkEmailProvider)),
                Assembly.GetAssembly(typeof(BrevoEmailProvider)),
                Assembly.GetAssembly(typeof(ZeptoMailEmailProvider)),
                Assembly.GetAssembly(typeof(MailgunEmailProvider)),
            };

            Assert.Equal(9, assemblies.Length);
            Assert.All(assemblies, a => Assert.NotNull(a));
            Assert.Equal(8, assemblies.Distinct().Count());
        }
    }
}
