using System.Reflection;
using MailForge.AmazonSES;
using MailForge.AzureCS;
using MailForge.Brevo;
using MailForge.Communication;
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
        // Must match <Version> in Directory.Build.props.
        private const string ExpectedVersion = "1.1.0";
        [Theory]
        [InlineData(typeof(EmailMessage), "MailForge")]
        [InlineData(typeof(NotificationSender), "MailForge.Communication")]
        [InlineData(typeof(SmtpEmailProvider), "MailForge.Smtp")]
        [InlineData(typeof(ResendEmailProvider), "MailForge.Resend")]
        [InlineData(typeof(AmazonSesEmailProvider), "MailForge.AmazonSES")]
        [InlineData(typeof(PostmarkEmailProvider), "MailForge.Postmark")]
        [InlineData(typeof(BrevoEmailProvider), "MailForge.Brevo")]
        [InlineData(typeof(ZeptoMailEmailProvider), "MailForge.ZeptoMail")]
        [InlineData(typeof(MailgunEmailProvider), "MailForge.Mailgun")]
        [InlineData(typeof(AzureCSEmailProvider), "MailForge.AzureCS")]
        public void PackageMarker_MatchesPackageId(Type markerType, string expectedPackageId)
        {
            Assert.Equal(expectedPackageId, markerType.Assembly.GetName().Name);
        }

        [Theory]
        [InlineData(typeof(EmailMessage))]
        [InlineData(typeof(EmailSender))]
        [InlineData(typeof(NotificationSender))]
        [InlineData(typeof(SmtpEmailProvider))]
        [InlineData(typeof(ResendEmailProvider))]
        [InlineData(typeof(AmazonSesEmailProvider))]
        [InlineData(typeof(PostmarkEmailProvider))]
        [InlineData(typeof(BrevoEmailProvider))]
        [InlineData(typeof(ZeptoMailEmailProvider))]
        [InlineData(typeof(MailgunEmailProvider))]
        [InlineData(typeof(AzureCSEmailProvider))]
        public void LibraryAssemblies_AreVersionedFromDirectoryBuildProps(Type markerType)
        {
            var version = markerType.Assembly.GetName().Version;
            Assert.NotNull(version);
            Assert.StartsWith(ExpectedVersion + ".", version.ToString());
        }

        [Fact]
        public void LibraryAssemblies_ShareOneVersion()
        {
            var assemblies = new[]
            {
                Assembly.GetAssembly(typeof(EmailMessage)),
                Assembly.GetAssembly(typeof(EmailSender)),
                Assembly.GetAssembly(typeof(NotificationSender)),
                Assembly.GetAssembly(typeof(SmtpEmailProvider)),
                Assembly.GetAssembly(typeof(ResendEmailProvider)),
                Assembly.GetAssembly(typeof(AmazonSesEmailProvider)),
                Assembly.GetAssembly(typeof(PostmarkEmailProvider)),
                Assembly.GetAssembly(typeof(BrevoEmailProvider)),
                Assembly.GetAssembly(typeof(ZeptoMailEmailProvider)),
                Assembly.GetAssembly(typeof(MailgunEmailProvider)),
                Assembly.GetAssembly(typeof(AzureCSEmailProvider)),
            };

            Assert.All(assemblies, a => Assert.NotNull(a));

            var versionStrings = assemblies.Select(a => a!.GetName().Version!.ToString()).ToArray();
            Assert.All(versionStrings, v => Assert.StartsWith(ExpectedVersion + ".", v));
            Assert.Single(versionStrings.Distinct());
        }

        [Fact]
        public void Solution_ReferencesAllLibraries()
        {
            var assemblies = new[]
            {
                Assembly.GetAssembly(typeof(EmailMessage)),
                Assembly.GetAssembly(typeof(EmailSender)),
                Assembly.GetAssembly(typeof(NotificationSender)),
                Assembly.GetAssembly(typeof(SmtpEmailProvider)),
                Assembly.GetAssembly(typeof(ResendEmailProvider)),
                Assembly.GetAssembly(typeof(AmazonSesEmailProvider)),
                Assembly.GetAssembly(typeof(PostmarkEmailProvider)),
                Assembly.GetAssembly(typeof(BrevoEmailProvider)),
                Assembly.GetAssembly(typeof(ZeptoMailEmailProvider)),
                Assembly.GetAssembly(typeof(MailgunEmailProvider)),
                Assembly.GetAssembly(typeof(AzureCSEmailProvider)),
            };

            Assert.Equal(11, assemblies.Length);
            Assert.All(assemblies, a => Assert.NotNull(a));
            Assert.Equal(10, assemblies.Distinct().Count());
        }
    }
}
