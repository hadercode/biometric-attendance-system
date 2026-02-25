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

        public async Task<bool> HasAttendanceRecordsAsync(int employeeId)
        {
            using var db = new AppDbContext();
            return await db.AttendanceRecords.AnyAsync(a => a.EmployeeId == employeeId);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            using var db = new AppDbContext();
            var employee = await db.Employees.FindAsync(id);
            if (employee == null) return false;

            // Check for attendance history
            var hasRecords = await db.AttendanceRecords.AnyAsync(a => a.EmployeeId == id);
            if (hasRecords)
                throw new InvalidOperationException("No se puede eliminar un empleado con historial de asistencia. Use la opción de desactivar.");

            db.Employees.Remove(employee);
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateEmployeeAsync(int id)
        {
            using var db = new AppDbContext();
            var employee = await db.Employees.FindAsync(id);
            if (employee == null) return false;

            employee.IsActive = false;
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
        /// Get all fingerprint templates across all active employees for SDK identification.
        /// Returns a flat list of (employeeId, templateData) pairs.
        /// </summary>
        public async Task<List<(int employeeId, byte[] templateData)>> GetAllTemplatesForIdentificationAsync()
        {
            using var db = new AppDbContext();
            var result = new List<(int employeeId, byte[] templateData)>();

            // Get multi-finger templates
            var fingerprints = await db.FingerprintTemplates
                .Include(f => f.Employee)
                .Where(f => f.Employee.IsActive)
                .ToListAsync();

            foreach (var fp in fingerprints)
                result.Add((fp.EmployeeId, fp.TemplateData));

            // Also check legacy single templates for backward compat
            var legacyEmployees = await db.Employees
                .Where(e => e.IsActive && e.FingerprintTemplate != null)
                .ToListAsync();

            foreach (var emp in legacyEmployees)
            {
                // Only add if no multi-finger templates exist for this employee
                if (!result.Any(r => r.employeeId == emp.Id) && emp.FingerprintTemplate != null)
                    result.Add((emp.Id, emp.FingerprintTemplate));
            }

            return result;
        }

        /// <summary>
        /// Get all active employees that have any fingerprint templates.
        /// </summary>
        public async Task<List<Employee>> GetEmployeesWithTemplatesAsync()
        {
            using var db = new AppDbContext();
            return await db.Employees
                .Where(e => e.IsActive && (e.FingerprintTemplate != null || e.Fingerprints.Any()))
                .OrderBy(e => e.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Get fingerprint templates for a specific employee.
        /// </summary>
        public async Task<List<FingerprintTemplate>> GetEmployeeFingerprintsAsync(int employeeId)
        {
            using var db = new AppDbContext();
            return await db.FingerprintTemplates
                .Where(f => f.EmployeeId == employeeId)
                .OrderBy(f => f.FingerType)
                .ToListAsync();
        }

        /// <summary>
        /// Save all fingerprints for an employee (replaces existing ones).
        /// </summary>
        public async Task SaveEmployeeFingerprintsAsync(int employeeId, Dictionary<FingerType, byte[]> fingerprints)
        {
            using var db = new AppDbContext();

            // Remove existing fingerprints
            var existing = await db.FingerprintTemplates
                .Where(f => f.EmployeeId == employeeId)
                .ToListAsync();
            db.FingerprintTemplates.RemoveRange(existing);

            // Add new ones
            foreach (var (fingerType, templateData) in fingerprints)
            {
                db.FingerprintTemplates.Add(new FingerprintTemplate
                {
                    EmployeeId = employeeId,
                    FingerType = fingerType,
                    TemplateData = templateData,
                    CapturedAt = DateTime.Now
                });
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Legacy: Identify an employee by comparing a captured template.
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
