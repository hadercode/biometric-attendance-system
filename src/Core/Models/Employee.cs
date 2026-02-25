using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LectorHuellas.Core.Models
{
    [Table("employees")]
    public class Employee
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("codigo")]
        [StringLength(20)]
        public string Code { get; set; } = string.Empty;

        [Column("nombres")]
        [StringLength(50)]
        public string FirstNames { get; set; } = string.Empty;

        [Column("apellidos")]
        [StringLength(50)]
        public string LastNames { get; set; } = string.Empty;

        [NotMapped]
        public string FullName => $"{FirstNames} {LastNames}".Trim();

        [Column("direccion")]
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;

        [Column("telefono")]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Column("ccargo")]
        [StringLength(3)]
        public string PositionId { get; set; } = string.Empty;

        [Column("cgerencia")]
        [StringLength(3)]
        public string ManagementId { get; set; } = string.Empty;

        [Column("cdpto")]
        [StringLength(3)]
        public string DepartmentId { get; set; } = string.Empty;

        [Column("cunidad")]
        [StringLength(3)]
        public string UnitId { get; set; } = string.Empty;

        [Column("cturno")]
        [StringLength(3)]
        public string ShiftId { get; set; } = string.Empty;

        [Column("status")]
        public int Status { get; set; }

        [Column("fechan")]
        public DateTime? BirthDate { get; set; }

        [Column("fechai")]
        public DateTime? HireDate { get; set; }

        [Column("fechae")]
        public DateTime? LeaveDate { get; set; }

        [Column("mensaje")]
        [StringLength(200)]
        public string Message { get; set; } = string.Empty;

        [Column("listar")]
        public int? Listar { get; set; }

        // Biometric system specific fields
        [NotMapped]
        public string? PhotoPath { get; set; }
        
        [NotMapped]
        public bool IsActive => Status == 1;

        [ForeignKey("ManagementId")]
        public virtual Management? Management { get; set; }

        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; }

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<FingerprintTemplate> Fingerprints { get; set; } = new List<FingerprintTemplate>();
    }
}
