using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LectorHuellas.Data;
using LectorHuellas.Models;
using Microsoft.EntityFrameworkCore;

namespace LectorHuellas.Services
{
    public class AttendanceService
    {
        private readonly IFingerprintService _fingerprintService;

        public AttendanceService(IFingerprintService fingerprintService)
        {
            _fingerprintService = fingerprintService;
        }

        // ── Employee Management ─────────────────────────────────────────

        public async Task<Employee> RegisterEmployeeAsync(string fullName, string documentId, byte[] fingerprintTemplate)
        {
            using var db = new AppDbContext();
            var employee = new Employee
            {
                FullName = fullName,
                DocumentId = documentId,
                FingerprintTemplate = fingerprintTemplate,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee?> UpdateEmployeeAsync(int id, string fullName, string documentId, byte[]? newTemplate = null)
        {
            using var db = new AppDbContext();
            var employee = await db.Employees.FindAsync(id);
            if (employee == null) return null;

            employee.FullName = fullName;
            employee.DocumentId = documentId;
            if (newTemplate != null)
                employee.FingerprintTemplate = newTemplate;

            await db.SaveChangesAsync();
            return employee;
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            using var db = new AppDbContext();
            var employee = await db.Employees.FindAsync(id);
            if (employee == null) return false;

            db.Employees.Remove(employee);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            using var db = new AppDbContext();
            return await db.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FullName)
                .ToListAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            using var db = new AppDbContext();
            return await db.Employees.FindAsync(id);
        }

        /// <summary>
        /// Get all active employees that have fingerprint templates.
        /// Used by the SDK identification flow (FTRIdentifyN).
        /// </summary>
        public async Task<List<Employee>> GetEmployeesWithTemplatesAsync()
        {
            using var db = new AppDbContext();
            return await db.Employees
                .Where(e => e.IsActive && e.FingerprintTemplate != null)
                .OrderBy(e => e.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Identify an employee by comparing a captured template against all registered templates.
        /// Returns the matched employee or null if no match found.
        /// </summary>
        public async Task<Employee?> IdentifyByFingerprintAsync(byte[] capturedTemplate)
        {
            using var db = new AppDbContext();
            var employees = await db.Employees
                .Where(e => e.IsActive && e.FingerprintTemplate != null)
                .ToListAsync();

            foreach (var emp in employees)
            {
                if (emp.FingerprintTemplate != null &&
                    _fingerprintService.MatchTemplates(capturedTemplate, emp.FingerprintTemplate))
                {
                    return emp;
                }
            }

            return null;
        }

        // ── Attendance ──────────────────────────────────────────────────

        /// <summary>
        /// Record attendance for an employee. Automatically determines if it's a CheckIn or CheckOut
        /// based on the last record of the day.
        /// </summary>
        public async Task<(AttendanceRecord record, AttendanceType type)> RecordAttendanceAsync(int employeeId)
        {
            using var db = new AppDbContext();
            var today = DateTime.Today;

            // Find the last attendance record of the day
            var lastRecord = await db.AttendanceRecords
                .Where(r => r.EmployeeId == employeeId && r.Timestamp >= today)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            // Toggle: if last was CheckIn → CheckOut, otherwise CheckIn
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
            var totalEmployees = await db.Employees.CountAsync(e => e.IsActive);
            var presentToday = await db.AttendanceRecords
                .Where(r => r.Timestamp >= today)
                .Select(r => r.EmployeeId)
                .Distinct()
                .CountAsync();

            return (totalEmployees, presentToday, totalEmployees - presentToday);
        }
    }
}
