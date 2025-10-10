using System.ComponentModel.DataAnnotations;

namespace Web.API.Mappings.Request
{
    public class RegisterUser
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Username minimal 3 karakter dan maksimal 50 karakter")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password minimal 6 karakter")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role wajib diisi")]
        [StringLength(20)]
        public string Role { get; set; } = "user";
    }
    public class CreateUserNameOnlyRequest
    {
        public string UserName { get; set; } = default!;
        public string Role { get; set; } = default!;
    }
}
