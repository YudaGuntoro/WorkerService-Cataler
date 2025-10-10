using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.Auth
{
    // DTO hasil autentikasi
    public sealed class AuthResult
    {
        public string Token { get; init; } = default!;
        public DateTime ExpiresAt { get; init; }
        public string UserId { get; init; } = default!;
        public string UserName { get; init; } = default!;
        public string Role { get; init; } = default!;
    }

}
