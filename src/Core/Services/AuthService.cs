using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LectorHuellas.Core.Data;
using LectorHuellas.Core.Models;

namespace LectorHuellas.Core.Services
{
    public class AuthService : IAuthService
    {
        public async Task<User?> LoginAsync(string username, string password)
        {
            try
            {
                using var db = new AppDbContext();
                // Plain text password comparison for now (as requested for simplicity)
                // In production, use hashing (e.g. BCrypt)
                var user = await db.Users
                    .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.Password == password);

                if (user != null && user.Status == "Habilitado")
                {
                    return user;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AuthService Error: {ex.Message}");
                return null;
            }
        }
    }
}
