using MailForge.Studio.Capture.Entities;
using Microsoft.EntityFrameworkCore;

namespace MailForge.Studio.Capture
{
    /// <summary>
    /// The EF Core database context backing MailForge Studio's local capture database.
    /// </summary>
    public sealed class StudioDbContext : DbContext
    {
        /// <summary>Creates a context using the supplied options.</summary>
        public StudioDbContext(DbContextOptions<StudioDbContext> options)
            : base(options)
        {
        }

        /// <summary>The captured messages.</summary>
        public DbSet<CapturedMessage> Messages => Set<CapturedMessage>();

        /// <summary>The captured recipients.</summary>
        public DbSet<CapturedRecipient> Recipients => Set<CapturedRecipient>();

        /// <summary>The captured attachments and inline images.</summary>
        public DbSet<CapturedAttachment> Attachments => Set<CapturedAttachment>();

        /// <summary>The captured headers.</summary>
        public DbSet<CapturedHeader> Headers => Set<CapturedHeader>();

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CapturedMessage>(builder =>
            {
                builder.HasKey(m => m.Id);
                builder.Property(m => m.MessageId).IsRequired().HasMaxLength(100);
                builder.HasIndex(m => m.MessageId).IsUnique();
                builder.Property(m => m.From).HasMaxLength(512);
                builder.Property(m => m.Subject).HasMaxLength(512);
                builder.Property(m => m.CreatedAt)
                    .HasConversion(
                        v => v.UtcDateTime,
                        v => new System.DateTimeOffset(v, System.TimeSpan.Zero));
                builder.HasIndex(m => m.CreatedAt);

                builder.HasMany(m => m.Recipients)
                    .WithOne()
                    .HasForeignKey(r => r.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(m => m.Attachments)
                    .WithOne()
                    .HasForeignKey(a => a.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                builder.HasMany(m => m.Headers)
                    .WithOne()
                    .HasForeignKey(h => h.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CapturedRecipient>(builder =>
            {
                builder.HasKey(r => r.Id);
                builder.Property(r => r.Address).HasMaxLength(512);
                builder.Property(r => r.DisplayName).HasMaxLength(256);
            });

            modelBuilder.Entity<CapturedAttachment>(builder =>
            {
                builder.HasKey(a => a.Id);
                builder.Property(a => a.FileName).HasMaxLength(256);
                builder.Property(a => a.MediaType).HasMaxLength(128);
                builder.Property(a => a.ContentId).HasMaxLength(256);
            });

            modelBuilder.Entity<CapturedHeader>(builder =>
            {
                builder.HasKey(h => h.Id);
                builder.Property(h => h.Name).HasMaxLength(128);
            });
        }
    }
}