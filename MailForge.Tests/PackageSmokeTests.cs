using System.Reflection;
using MailForge.AmazonSES;
using MailForge.Resend;
using MailForge.Smtp;

namespace MailForge.Tests
{
    public class PackageSmokeTests
    {
        [Theory]
        [InlineData(typeof(EmailMessage), "MailForge")]
        [InlineData(typeof(SmtpEmailProvider), "MailForge.Smtp")]
        [InlineData(typeof(ResendEmailProvider), "MailForge.Resend")]
        [InlineData(typeof(AmazonSesEmailProvider), "MailForge.AmazonSES")]
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
        public void LibraryAssemblies_AreVersionedFromDirectoryBuildProps(Type markerType)
        {
            var version = markerType.Assembly.GetName().Version;
            Assert.NotNull(version);
            Assert.StartsWith("0.1.0.", version.ToString());
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
            };

            Assert.Equal(5, assemblies.Length);
            Assert.All(assemblies, a => Assert.NotNull(a));
            Assert.Equal(4, assemblies.Distinct().Count());
        }
    }
}
