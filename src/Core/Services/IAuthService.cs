using LectorHuellas.Core.Models;
using System.Threading.Tasks;

namespace LectorHuellas.Core.Services
{
    public interface IAuthService
    {
        Task<User?> LoginAsync(string username, string password);
    }
}
