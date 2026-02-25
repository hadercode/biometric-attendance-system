using System.Collections.Generic;
using System.Threading.Tasks;
using LectorHuellas.Core.Models;

namespace LectorHuellas.Core.Services
{
    public interface ICommonService
    {
        Task<List<Department>> GetDepartmentsAsync();
        Task<List<Unit>> GetUnitsAsync();
        Task<List<Shift>> GetShiftsAsync();
        Task<List<Management>> GetManagementsAsync();
    }
}
