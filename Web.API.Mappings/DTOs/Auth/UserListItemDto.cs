using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.DTOs.Auth
{
    public class UserListItemDto
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string? Role { get; set; }
        public bool IsActive { get; set; }
    }
}
