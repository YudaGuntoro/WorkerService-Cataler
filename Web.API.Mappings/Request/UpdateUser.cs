using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.API.Mappings.Request
{
    public class UpdateUser
    {
        [Required(ErrorMessage = "Id wajib diisi")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username minimal 3 karakter dan maksimal 50 karakter")]
        public string UserName { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        public string? Password { get; set; } // nullable, karena bisa saja tidak diubah

        [Required(ErrorMessage = "Role wajib diisi")]
        [StringLength(20)]
        public string Role { get; set; } = "user";

        public bool IsActive { get; set; } = true;
    }
}
