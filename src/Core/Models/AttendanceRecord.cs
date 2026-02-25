using System;

namespace LectorHuellas.Core.Models
{
    public enum AttendanceType
    {
        CheckIn,
        CheckOut
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public AttendanceType Type { get; set; }

        // Enriched fields (populated by trigger for external reports)
        public string? FirstNames { get; set; }
        public string? LastNames { get; set; }
        public string? EmployeeCode { get; set; }
        public int? Hour { get; set; }
        public int? Minute { get; set; }
        public DateTime? DateOnly { get; set; }
        public string? ShiftCode { get; set; }

        public Employee Employee { get; set; } = null!;
    }
}
