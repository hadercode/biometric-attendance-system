using Microsoft.EntityFrameworkCore;
using LectorHuellas.Models;
using System;
using System.IO;

namespace LectorHuellas.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Employee> Employees { get; set; } = null!;
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;

        private static string DbPath
        {
            get
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LectorHuellas");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "attendance.db");
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.DocumentId).IsRequired().HasMaxLength(50);
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
        }

        public void EnsureCreated()
        {
            Database.EnsureCreated();
        }
    }
}
