using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LectorHuellas.Core.Data;
using LectorHuellas.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LectorHuellas.Core.Services
{
    public class CommonService : ICommonService
    {
        public async Task<List<Department>> GetDepartmentsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var data = await db.Departments
                    .OrderBy(d => d.Name)
                    .ToListAsync();
                Console.WriteLine($"DEBUG: GetDepartmentsAsync loaded {data.Count} items.");
                foreach(var item in data.Take(5)) Console.WriteLine($"  - Dpto: [{item.Code}] {item.Name}");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG ERROR: GetDepartmentsAsync failed: {ex.Message}");
                return new List<Department>();
            }
        }

        public async Task<List<Unit>> GetUnitsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var data = await db.Units
                    .OrderBy(u => u.Name)
                    .ToListAsync();
                Console.WriteLine($"DEBUG: GetUnitsAsync loaded {data.Count} items.");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG ERROR: GetUnitsAsync failed: {ex.Message}");
                return new List<Unit>();
            }
        }

        public async Task<List<Shift>> GetShiftsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var data = await db.Shifts
                    .OrderBy(s => s.Description)
                    .ToListAsync();
                Console.WriteLine($"DEBUG: GetShiftsAsync loaded {data.Count} items.");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG ERROR: GetShiftsAsync failed: {ex.Message}");
                return new List<Shift>();
            }
        }

        public async Task<List<Management>> GetManagementsAsync()
        {
            try
            {
                using var db = new AppDbContext();
                var data = await db.Managements
                    .OrderBy(m => m.Name)
                    .ToListAsync();
                Console.WriteLine($"DEBUG: GetManagementsAsync loaded {data.Count} items.");
                foreach(var item in data.Take(5)) Console.WriteLine($"  - Mgr: [{item.Code}] {item.Name}");
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG ERROR: GetManagementsAsync failed: {ex.Message}");
                return new List<Management>();
            }
        }
    }
}
