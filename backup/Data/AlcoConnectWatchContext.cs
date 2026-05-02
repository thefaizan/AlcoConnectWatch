using System.Data.Entity;
using AlcoConnectWatch.Models;

namespace AlcoConnectWatch.Data
{
    public class AlcoConnectWatchContext : DbContext
    {
        public AlcoConnectWatchContext() : base("name=AlcoConnectWatchDb")
        {
        }

        public DbSet<EvacRecord> EvacRecords { get; set; }
        public DbSet<AlcoConnectRecord> AlcoConnectRecords { get; set; }
        public DbSet<FileImportLog> FileImportLogs { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EvacRecord>()
                .HasIndex(e => new { e.ExtractedId, e.RosterDate, e.WorkSite })
                .HasName("IX_EvacRecords_Lookup");

            modelBuilder.Entity<AlcoConnectRecord>()
                .HasIndex(a => new { a.StaffId, a.TestDate, a.Site })
                .HasName("IX_AlcoConnectRecords_Lookup");

            modelBuilder.Entity<AlcoConnectRecord>()
                .HasIndex(a => new { a.StaffId, a.TestDate, a.Site, a.TestTime })
                .HasName("IX_AlcoConnectRecords_Dedup");

            modelBuilder.Entity<FileImportLog>()
                .HasIndex(f => f.ImportedAt)
                .HasName("IX_FileImportLogs_ImportedAt");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique()
                .HasName("IX_Users_Email");

            base.OnModelCreating(modelBuilder);
        }
    }
}
