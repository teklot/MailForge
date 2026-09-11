using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// Design-time factory that lets the EF Core CLI tooling ("dotnet ef migrations add")
    /// build a <see cref="StudioDbContext"/> without an application host.
    /// </summary>
    public sealed class StudioDbContextFactory : IDesignTimeDbContextFactory<StudioDbContext>
    {
        /// <summary>Creates a context for design-time tooling.</summary>
        public StudioDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<StudioDbContext>()
                .UseSqlite("Data Source=mailforge-studio.db")
                .Options;
            return new StudioDbContext(options);
        }
    }
}