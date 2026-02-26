using LectorHuellas.Core.Models;

namespace LectorHuellas.Core.Services
{
    public class SessionService
    {
        public UserSession? CurrentUser { get; private set; }

        public void StartSession(User user)
        {
            CurrentUser = new UserSession
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FirstName + " " + user.LastName,
                RolId = user.RolId
            };
        }

        public void ClearSession()
        {
            CurrentUser = null;
        }

        public bool IsAuthenticated => CurrentUser != null;
        
        public bool CanConfigureSettings => CurrentUser?.RolId == 1;
    }
}
