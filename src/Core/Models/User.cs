using System;

namespace LectorHuellas.Core.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int RolId { get; set; }
        public string Status { get; set; } = "Habilitado";
    }
}
