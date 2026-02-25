using System;

namespace LectorHuellas.Core.Models
{
    /// <summary>
    /// Lightweight representation of a user for session management.
    /// Excludes sensitive data like password.
    /// </summary>
    public class UserSession
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RolId { get; set; }

        public bool IsAdmin => RolId == 1;
        public bool IsStandardAdmin => RolId == 3;
    }
}
