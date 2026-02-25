using System.Collections.Generic;
using System.Threading.Tasks;
using LectorHuellas.Core.Models;

namespace LectorHuellas.Core.Services
{
    public interface IEmployeeService
    {
        Task<Employee> RegisterEmployeeAsync(string code, string firstNames, string lastNames, string positionId, string managementId, string departmentId, string unitId, string shiftId, string photoPath, string address, string phone, DateTime? birthDate, DateTime? hireDate, string message, byte[] fingerprintTemplate);
        Task<Employee?> UpdateEmployeeAsync(int id, string code, string firstNames, string lastNames, string positionId, string managementId, string departmentId, string unitId, string shiftId, string photoPath, string address, string phone, DateTime? birthDate, DateTime? hireDate, string message);
        Task<bool> HasAttendanceRecordsAsync(int employeeId);
        Task<bool> DeleteEmployeeAsync(int id);
        Task<bool> DeactivateEmployeeAsync(int id);
        Task<List<Employee>> GetAllEmployeesAsync();
        Task<List<Employee>> GetEmployeesPaginatedAsync(int page, int pageSize, string? searchTerm = null);
        Task<int> GetTotalEmployeesCountAsync(string? searchTerm = null);
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<List<(int employeeId, byte[] templateData)>> GetAllTemplatesForIdentificationAsync();
        Task<List<Employee>> GetEmployeesWithTemplatesAsync();
        Task<List<FingerprintTemplate>> GetEmployeeFingerprintsAsync(int employeeId);
        Task SaveEmployeeFingerprintsAsync(int employeeId, Dictionary<FingerType, byte[]> fingerprints);
    }
}
