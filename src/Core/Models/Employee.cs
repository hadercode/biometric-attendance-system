using System;
using System.Collections.Generic;

namespace LectorHuellas.Core.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public byte[]? FingerprintTemplate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
        public ICollection<FingerprintTemplate> Fingerprints { get; set; } = new List<FingerprintTemplate>();
    }
}
