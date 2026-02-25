using Microsoft.EntityFrameworkCore;
using LectorHuellas.Core.Models;
using System;
using System.IO;

namespace LectorHuellas.Core.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;
        public DbSet<FingerprintTemplate> FingerprintTemplates { get; set; } = null!;

        private readonly DatabaseSettings? _settings;

        /// <summary>
        /// Default constructor — loads settings from dbsettings.json
        /// </summary>
        public AppDbContext()
        {
            _settings = DatabaseSettings.Load();
        }

        /// <summary>
        /// Constructor with explicit settings (used for test connection)
        /// </summary>
        public AppDbContext(DatabaseSettings settings)
        {
            _settings = settings;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured) return;

            var settings = _settings ?? DatabaseSettings.Load();
            var connectionString = settings.GetConnectionString();

            switch (settings.Provider)
            {
                case "PostgreSQL":
                    options.UseNpgsql(connectionString);
                    break;
                case "MySQL":
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                    break;
                default: // SQLite
                    // Ensure directory exists for SQLite
                    var dir = Path.GetDirectoryName(settings.GetSqlitePath());
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                    options.UseSqlite(connectionString);
                    break;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Position).IsRequired().HasMaxLength(150); // New property
                entity.Property(e => e.PhotoPath).HasMaxLength(400); // New property
                entity.HasIndex(e => e.DocumentId).IsUnique();
                entity.Property(e => e.FingerprintTemplate);
            });

            modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Type).HasConversion<string>();
                entity.HasOne(e => e.Employee)
                      .WithMany(e => e.AttendanceRecords)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Timestamp);
            });

            modelBuilder.Entity<FingerprintTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FingerType).HasConversion<string>();
                entity.Property(e => e.TemplateData).IsRequired();
                entity.HasOne(e => e.Employee)
                      .WithMany(e => e.Fingerprints)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.EmployeeId, e.FingerType }).IsUnique();
            });
        }

        public void EnsureCreated()
        {
            Database.EnsureCreated();
        }
    }
}
