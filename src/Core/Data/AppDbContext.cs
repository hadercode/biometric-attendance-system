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
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<Unit> Units { get; set; } = null!;
        public DbSet<Shift> Shifts { get; set; } = null!;
        public DbSet<Management> Managements { get; set; } = null!;

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
                entity.ToTable("employees");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id");
                
                entity.Property(e => e.Code).HasColumnName("codigo").HasMaxLength(20);
                entity.Property(e => e.FirstNames).HasColumnName("nombres").IsRequired().HasMaxLength(50);
                entity.Property(e => e.LastNames).HasColumnName("apellidos").IsRequired().HasMaxLength(50);
                entity.Property(e => e.Address).HasColumnName("direccion").HasMaxLength(100);
                entity.Property(e => e.Phone).HasColumnName("telefono").HasMaxLength(30);
                entity.Property(e => e.PositionId).HasColumnName("ccargo");
                entity.Property(e => e.ManagementId).HasColumnName("cgerencia");
                entity.Property(e => e.DepartmentId).HasColumnName("cdpto");
                entity.Property(e => e.UnitId).HasColumnName("cunidad");
                entity.Property(e => e.ShiftId).HasColumnName("cturno");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.BirthDate).HasColumnName("fechan");
                entity.Property(e => e.HireDate).HasColumnName("fechai");
                entity.Property(e => e.LeaveDate).HasColumnName("fechae");
                entity.Property(e => e.Message).HasColumnName("mensaje");
                entity.Property(e => e.Listar).HasColumnName("listar");
            });

            modelBuilder.Entity<AttendanceRecord>(entity =>
            {
                entity.ToTable("attendance_records");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
                entity.Property(e => e.Timestamp).HasColumnName("timestamp");
                entity.Property(e => e.Type).HasConversion<string>().HasColumnName("type");

                // Enriched fields mapping
                entity.Property(e => e.FirstNames).HasColumnName("nombres").HasMaxLength(50);
                entity.Property(e => e.LastNames).HasColumnName("apellidos").HasMaxLength(50);
                entity.Property(e => e.EmployeeCode).HasColumnName("codigo").HasMaxLength(20);
                entity.Property(e => e.Hour).HasColumnName("hora");
                entity.Property(e => e.Minute).HasColumnName("min");
                entity.Property(e => e.DateOnly).HasColumnName("fecha");
                entity.Property(e => e.ShiftCode).HasColumnName("horario").HasMaxLength(10);
                
                entity.HasOne(e => e.Employee)
                      .WithMany(e => e.AttendanceRecords)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.Timestamp);
            });

            modelBuilder.Entity<FingerprintTemplate>(entity =>
            {
                entity.ToTable("fingerprint_templates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EmployeeId).HasColumnName("employee_id");
                entity.Property(e => e.FingerType).HasConversion<string>().HasColumnName("finger_type");
                entity.Property(e => e.TemplateData).IsRequired().HasColumnName("template_data");
                entity.Property(e => e.CapturedAt).HasColumnName("captured_At");

                entity.HasOne(e => e.Employee)
                      .WithMany(e => e.Fingerprints)
                      .HasForeignKey(e => e.EmployeeId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.EmployeeId, e.FingerType }).IsUnique();
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("departamento");
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Code).HasColumnName("codigo");
                entity.Property(e => e.Name).HasColumnName("dpto");
            });

            modelBuilder.Entity<Unit>(entity =>
            {
                entity.ToTable("unidad");
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Code).HasColumnName("codigo");
                entity.Property(e => e.Name).HasColumnName("unidad");
            });

            modelBuilder.Entity<Shift>(entity =>
            {
                entity.ToTable("turno");
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Code).HasColumnName("codigo");
                entity.Property(e => e.Description).HasColumnName("des");
                entity.Property(e => e.Limit).HasColumnName("limite");
                entity.Property(e => e.Dawn).HasColumnName("amanecer");
                entity.Property(e => e.Afternoon).HasColumnName("tarde");
                entity.Property(e => e.Over).HasColumnName("sobre");
                entity.Property(e => e.Slack).HasColumnName("holgura");
                entity.Property(e => e.Rest).HasColumnName("descanso");
                entity.Property(e => e.Duration).HasColumnName("duracion");
                entity.Property(e => e.Schedule).HasColumnName("horario");
                entity.Property(e => e.Entry).HasColumnName("entrada");
            });

            modelBuilder.Entity<Management>(entity =>
            {
                entity.ToTable("gerencia");
                entity.HasKey(e => e.Code);
                entity.Property(e => e.Code).HasColumnName("codigo");
                entity.Property(e => e.Name).HasColumnName("gerencia");
            });
        }

        public void EnsureCreated()
        {
            Database.EnsureCreated();
        }
    }
}
