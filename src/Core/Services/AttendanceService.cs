using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LectorHuellas.Core.Data;
using LectorHuellas.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LectorHuellas.Core.Services
{
    public class AttendanceService
    {
        private readonly IFingerprintService _fingerprintService;
        private readonly IEmployeeService _employeeService;

        public AttendanceService(IFingerprintService fingerprintService, IEmployeeService employeeService)
        {
            _fingerprintService = fingerprintService;
            _employeeService = employeeService;
        }

        /// <summary>
        /// Highly optimized flow for ID Card screen: Uses SDK native 1:N identification
        /// Handles capture UI (callbacks) and matching in a single SDK call.
        /// </summary>
        public async Task<(Employee Employee, AttendanceType Type, bool NoMatch, bool IsCooldownActive)?> IdentifyAndRecordAsync()
        {
            // 1. Get all templates and map them to Employee IDs
            var templatesWithEmp = await _employeeService.GetAllTemplatesForIdentificationAsync();
            if (templatesWithEmp.Count == 0) return null;

            var templates = templatesWithEmp.Select(t => t.templateData).ToList();
            var empIds = templatesWithEmp.Select(t => t.employeeId).ToList();

            // 2. Call SDK native Identification (1:N)
            var (matchIndex, _) = await _fingerprintService.IdentifyFingerprintAsync(templates);

            if (matchIndex < 0 || matchIndex >= empIds.Count)
            {
                if (matchIndex == -1) // Specific "No Match Found"
                    return (null, default, true, false);
                    
                return null; // Cancelled (-2) or error
            }

            int matchedEmployeeId = empIds[matchIndex];

            // 3. Check for cooldown before recording
            var (isCooldown, _) = await CheckCooldownAsync(matchedEmployeeId);
            
            // 4. Get full employee info
            var employee = await _employeeService.GetEmployeeByIdAsync(matchedEmployeeId);
            if (employee == null) return null;

            if (isCooldown)
            {
                return (employee, default, false, true);
            }

            // 5. Record attendance
            var (_, type) = await RecordAttendanceAsync(matchedEmployeeId);
            
            return (employee, type, false, false);
        }

        // ── Attendance ──────────────────────────────────────────────────

        /// <summary>
        /// Checks if an employee has registered attendance in the last 5 minutes.
        /// </summary>
        public async Task<(bool isActive, AttendanceRecord? lastRecord)> CheckCooldownAsync(int employeeId)
        {
            using var db = new AppDbContext();
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);

            var lastRecentRecord = await db.AttendanceRecords
                .Where(r => r.EmployeeId == employeeId && r.Timestamp >= fiveMinutesAgo)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            return (lastRecentRecord != null, lastRecentRecord);
        }

        /// <summary>
        /// Record attendance for an employee. Automatically determines if it's a CheckIn or CheckOut
        /// based on the last record of the day.
        /// </summary>
        public async Task<(AttendanceRecord record, AttendanceType type)> RecordAttendanceAsync(int employeeId)
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;

            var lastRecord = await db.AttendanceRecords
                .Where(r => r.EmployeeId == employeeId && r.Timestamp >= today)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            var type = (lastRecord == null || lastRecord.Type == AttendanceType.CheckOut)
                ? AttendanceType.CheckIn
                : AttendanceType.CheckOut;

            var record = new AttendanceRecord
            {
                EmployeeId = employeeId,
                Timestamp = DateTime.Now,
                Type = type
            };

            db.AttendanceRecords.Add(record);
            await db.SaveChangesAsync();

            return (record, type);
        }

        // ── Reports ─────────────────────────────────────────────────────

        public async Task<List<AttendanceRecord>> GetAttendanceReportAsync(
            DateTime dateFrom, DateTime dateTo, int? employeeId = null)
        {
            using var db = new AppDbContext();
            var query = db.AttendanceRecords
                .Include(r => r.Employee)
                .Where(r => r.Timestamp >= dateFrom && r.Timestamp <= dateTo.AddDays(1));

            if (employeeId.HasValue)
                query = query.Where(r => r.EmployeeId == employeeId.Value);

            return await query
                .OrderByDescending(r => r.Timestamp)
                .ToListAsync();
        }

        public async Task<(int totalEmployees, int presentToday, int absentToday)> GetDashboardStatsAsync()
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;
            var totalEmployees = await db.Employees.CountAsync(e => e.Status == 1);
            var presentToday = await db.AttendanceRecords
                .Where(r => r.Timestamp >= today)
                .Select(r => r.EmployeeId)
                .Distinct()
                .CountAsync();

            return (totalEmployees, presentToday, totalEmployees - presentToday);
        }

        public async Task<List<AttendanceRecord>> GetRecentRecordsAsync(int count = 10)
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;
            return await db.AttendanceRecords
                .Include(r => r.Employee)
                .Where(r => r.Timestamp >= today)
                .OrderByDescending(r => r.Timestamp)
                .Take(count)
                .ToListAsync();
        }
    }
}
