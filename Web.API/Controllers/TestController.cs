using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // Endpoint ini hanya bisa diakses jika JWT valid
        [HttpGet("secure")]
        [Authorize]
        public IActionResult SecureEndpoint()
        {
            // Bisa ambil data user dari claim
            var userId = User.FindFirst("UserId")?.Value;
            var role = User.FindFirst("Role")?.Value;

            return Ok(new
            {
                Message = "Selamat! Token JWT kamu valid.",
                UserId = userId,
                Role = role
            });
        }

        // Endpoint publik tanpa token
        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult PublicEndpoint()
        {
            return Ok(new
            {
                Message = "Ini endpoint publik, tidak butuh token."
            });
        }
    }
}
