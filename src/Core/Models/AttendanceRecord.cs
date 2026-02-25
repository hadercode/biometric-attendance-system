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

        public Employee Employee { get; set; } = null!;
    }
}
