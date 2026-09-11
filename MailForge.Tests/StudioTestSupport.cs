using System;
using System.IO;
using MailForge.Studio.Capture;
using Microsoft.EntityFrameworkCore;

namespace MailForge.Tests
{
    /// <summary>Utility helpers shared by the MailForge Studio tests.</summary>
    internal static class StudioTestSupport
    {
        /// <summary>The name of the SQLite file referenced by a database path.</summary>
        public static string FileName(string databasePath) => Path.GetFileName(databasePath);

        /// <summary>Creates a SQLite-backed context factory on a unique temporary file.</summary>
        public static IDbContextFactory<StudioDbContext> CreateTempContextFactory(out string databasePath)
        {
            databasePath = Path.Combine(Path.GetTempPath(), $"mailforge-studio-test-{Guid.NewGuid():N}.db");
            return new TestContextFactory(databasePath);
        }

        /// <summary>Deletes the SQLite database file and any sidecar files created by SQLite.</summary>
        public static void CleanupDatabase(string databasePath)
        {
            foreach (var candidate in new[] { databasePath, databasePath + "-wal", databasePath + "-shm" })
            {
                if (File.Exists(candidate))
                    File.Delete(candidate);
            }
        }

        private sealed class TestContextFactory : IDbContextFactory<StudioDbContext>, IDisposable
        {
            private readonly DbContextOptions<StudioDbContext> _options;

            public TestContextFactory(string databasePath)
            {
                _options = new DbContextOptionsBuilder<StudioDbContext>()
                    .UseSqlite($"Data Source={databasePath};Pooling=false")
                    .Options;
            }

            public StudioDbContext CreateDbContext() => new StudioDbContext(_options);

            public void Dispose()
            {
            }
        }
    }
}