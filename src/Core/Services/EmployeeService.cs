using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LectorHuellas.Core.Data;
using LectorHuellas.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LectorHuellas.Core.Services
{
    public class EmployeeService : IEmployeeService
    {
        public async Task<Employee> RegisterEmployeeAsync(string code, string firstNames, string lastNames, string positionId, string managementId, string departmentId, string unitId, string shiftId, string photoPath, string address, string phone, DateTime? birthDate, DateTime? hireDate, string message, byte[] fingerprintTemplate)
        {
            using var db = new AppDbContext();
            var employee = new Employee
            {
                Code = code,
                FirstNames = firstNames,
                LastNames = lastNames,
                PositionId = positionId,
                ManagementId = managementId,
                DepartmentId = departmentId,
                UnitId = unitId,
                ShiftId = shiftId,
                PhotoPath = photoPath,
                Address = address ?? "",
                Phone = phone ?? "",
                BirthDate = birthDate,
                HireDate = hireDate ?? DateTime.Now,
                Message = message ?? "",
                Status = 1, // Active
                Listar = 1
            };

            db.Employees.Add(employee);
            await db.SaveChangesAsync();
            return employee;
        }

        public async Task<Employee?> UpdateEmployeeAsync(int id, string code, string firstNames, string lastNames, string positionId, string managementId, string departmentId, string unitId, string shiftId, string photoPath, string address, string phone, DateTime? birthDate, DateTime? hireDate, string message)
        {
            using var db = new AppDbContext();
            var employee = await db.Employees.FindAsync(id);
            if (employee == null) return null;

            employee.Code = code;
            employee.FirstNames = firstNames;
            employee.LastNames = lastNames;
            employee.PositionId = positionId;
            employee.ManagementId = managementId;
            employee.DepartmentId = departmentId;
            employee.UnitId = unitId;
            employee.ShiftId = shiftId;
            employee.PhotoPath = photoPath;
            employee.Address = address ?? "";
            employee.Phone = phone ?? "";
            employee.BirthDate = birthDate;
            employee.HireDate = hireDate;
            employee.Message = message ?? "";

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

            employee.Status = 0; // Deactivated
            await db.SaveChangesAsync();
            return true;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            using var db = new AppDbContext();
            return await db.Employees
                .Where(e => e.Status == 1)
                .OrderBy(e => e.LastNames)
                .ThenBy(e => e.FirstNames)
                .ToListAsync();
        }

        public async Task<List<Employee>> GetEmployeesPaginatedAsync(int page, int pageSize, string? searchTerm = null)
        {
            using var db = new AppDbContext();
            var query = db.Employees.Where(e => e.Status == 1);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.Code.Contains(searchTerm));
            }

            return await query
                .OrderBy(e => e.LastNames)
                .ThenBy(e => e.FirstNames)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalEmployeesCountAsync(string? searchTerm = null)
        {
            using var db = new AppDbContext();
            var query = db.Employees.Where(e => e.Status == 1);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.Code.Contains(searchTerm));
            }

            return await query.CountAsync();
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            using var db = new AppDbContext();
            return await db.Employees.FindAsync(id);
        }

        public async Task<List<(int employeeId, byte[] templateData)>> GetAllTemplatesForIdentificationAsync()
        {
            using var db = new AppDbContext();
            var result = new List<(int employeeId, byte[] templateData)>();

            var fingerprints = await db.FingerprintTemplates
                .Include(f => f.Employee)
                .Where(f => f.Employee.Status == 1)
                .ToListAsync();

            foreach (var fp in fingerprints)
                result.Add((fp.EmployeeId, fp.TemplateData));

            return result;
        }

        public async Task<List<Employee>> GetEmployeesWithTemplatesAsync()
        {
            using var db = new AppDbContext();
            return await db.Employees
                .Where(e => e.Status == 1 && e.Fingerprints.Any())
                .OrderBy(e => e.Code)
                .ToListAsync();
        }

        public async Task<List<FingerprintTemplate>> GetEmployeeFingerprintsAsync(int employeeId)
        {
            using var db = new AppDbContext();
            return await db.FingerprintTemplates
                .Where(f => f.EmployeeId == employeeId)
                .OrderBy(f => f.FingerType)
                .ToListAsync();
        }

        public async Task SaveEmployeeFingerprintsAsync(int employeeId, Dictionary<FingerType, byte[]> fingerprints)
        {
            using var db = new AppDbContext();

            var existing = await db.FingerprintTemplates
                .Where(f => f.EmployeeId == employeeId)
                .ToListAsync();
            db.FingerprintTemplates.RemoveRange(existing);

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
    }
}
