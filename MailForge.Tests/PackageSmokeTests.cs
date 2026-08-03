using System.Reflection;
using MailForge.Abstractions;
using MailForge.AmazonSES;
using MailForge.Core;
using MailForge.Resend;
using MailForge.Smtp;

namespace MailForge.Tests
{
    public class PackageSmokeTests
    {
        [Theory]
        [InlineData(typeof(MailForge.Abstractions.PackageInfo), "MailForge.Abstractions")]
        [InlineData(typeof(MailForge.Core.PackageInfo), "MailForge.Core")]
        [InlineData(typeof(MailForge.Smtp.PackageInfo), "MailForge.Smtp")]
        [InlineData(typeof(MailForge.Resend.PackageInfo), "MailForge.Resend")]
        [InlineData(typeof(MailForge.AmazonSES.PackageInfo), "MailForge.AmazonSES")]
        public void PackageMarker_MatchesPackageId(Type markerType, string expectedPackageId)
        {
            var field = markerType.GetField("Name")!;
            var name = field.GetValue(null);
            Assert.Equal(expectedPackageId, name);
        }

        [Theory]
        [InlineData(typeof(MailForge.Abstractions.PackageInfo))]
        [InlineData(typeof(MailForge.Core.PackageInfo))]
        [InlineData(typeof(MailForge.Smtp.PackageInfo))]
        [InlineData(typeof(MailForge.Resend.PackageInfo))]
        [InlineData(typeof(MailForge.AmazonSES.PackageInfo))]
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
                Assembly.GetAssembly(typeof(MailForge.Abstractions.PackageInfo)),
                Assembly.GetAssembly(typeof(MailForge.Core.PackageInfo)),
                Assembly.GetAssembly(typeof(MailForge.Smtp.PackageInfo)),
                Assembly.GetAssembly(typeof(MailForge.Resend.PackageInfo)),
                Assembly.GetAssembly(typeof(MailForge.AmazonSES.PackageInfo)),
            };

            Assert.Equal(5, assemblies.Length);
            Assert.All(assemblies, a => Assert.NotNull(a));
            Assert.Equal(5, assemblies.Distinct().Count());
        }
    }
}
