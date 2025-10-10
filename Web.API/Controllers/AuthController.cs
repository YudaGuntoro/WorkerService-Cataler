using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.API.Mappings.DTOs.Notification;
using Web.API.Mappings.Request;
using Web.API.Persistence.Services;

namespace Web.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _svc;

        public AuthController(IUserService userService)
        {
            _svc = userService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username dan password wajib diisi." });

            var result = await _svc.AuthenticateAsync(request.UserName, request.Password);
            if (result is null)
                return Unauthorized(new { message = "Username atau password salah." });

            return Ok(result);
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetUserList(
           [FromQuery] string? search = null,
           [FromQuery] bool? isActive = null,
           CancellationToken ct = default)
        {
            var resp = await _svc.GetUserListAsync(search, isActive, ct);
            return resp.Success ? Ok(resp) : BadRequest(resp);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _svc.GetRolesAsync();
            return Ok(roles);
        }

        [HttpGet("userslist")]
        public async Task<IActionResult> GetUsers(
        [FromQuery] string? search,
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
        {
            var resp = await _svc.GetUsersAsync(search, role, isActive, page, limit, ct);
            if (!resp.Success) return BadRequest(new { message = resp.Message ?? "Failed to get users" });
            return Ok(resp);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUser request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, error) = await _svc.CreateUserAsync(request);

            if (!success)
                return BadRequest(new { message = error });

            return Ok(new { message = "User berhasil dibuat." });
        }


        [HttpPost("register/name-only")]
        public async Task<IActionResult> RegisterNameOnly([FromBody] CreateUserNameOnlyRequest request)
        {
            if (request is null)
                return BadRequest(new { message = "Payload tidak boleh kosong." });

            var (ok, msg) = await _svc.CreateUserNameOnlyAsync(request);

            if (!ok)
            {
                // Jika pesan karena role admin / role invalid → 400
                if (msg?.Contains("Role", StringComparison.OrdinalIgnoreCase) == true)
                    return BadRequest(new { message = msg });

                // Jika pesan karena duplikat / konflik unik → 409
                if (msg?.Contains("duplikat", StringComparison.OrdinalIgnoreCase) == true ||
                    msg?.Contains("sudah dipakai", StringComparison.OrdinalIgnoreCase) == true)
                    return StatusCode(StatusCodes.Status409Conflict, new { message = msg });

                return BadRequest(new { message = msg ?? "Gagal membuat user." });
            }

            return Ok(new { message = $"User '{request.UserName.Trim()}' berhasil dibuat." });
        }


        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUser request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _svc.UpdateUserAsync(request);

            if (!result)
                return NotFound(new { message = "User tidak ditemukan." });

            return Ok(new { message = "User berhasil diupdate." });
        }

        [HttpDelete("{Id}")]
        public async Task<IActionResult> DeleteUser(uint Id)
        {
            var result = await _svc.DeleteUserAsync(Id);

            if (!result)
                return NotFound(new { message = "User tidak ditemukan." });

            return Ok(new { message = "User berhasil dihapus." });
        }

        // ========== NOTIFICATIONS ==========

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications([FromQuery] int? type, CancellationToken ct)
        {
            var resp = await _svc.GetNotificationsAsync(type, ct);
            if (!resp.Success) return BadRequest(new { message = resp.Message ?? "Failed to get notifications" });
            return Ok(resp);
        }

        [HttpPost("notifications")]
        [Consumes("application/json")]
        public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationRequest req, CancellationToken ct)
        {
            if (req is null)
                return BadRequest(new { message = "Payload is required." });

            // Validasi minimal
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { message = "Name wajib diisi." });
            if (string.IsNullOrWhiteSpace(req.Target))
                return BadRequest(new { message = "Target wajib diisi." });

            var resp = await _svc.CreateNotificationAsync(new CreateNotificationDto
            {
                Name = req.Name.Trim(),
                Target = req.Target.Trim(),
                Type = req.Type,
                Problems = req.Problems,
                ChangeOver = req.ChangeOver,
                FacilityCount = req.FacilityCount
            }, ct);

            return resp.Success
                ? Ok(resp)
                : BadRequest(new { message = resp.Message ?? "Failed to create notification" });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNotificationRequest req, CancellationToken ct)
        {
            if (req is null)
                return BadRequest(new { message = "Body tidak boleh kosong." });

            var res = await _svc.UpdateNotificationAsync(id, req, ct);
            return res.Success ? Ok(res) : BadRequest(res);
        }


        [HttpDelete("notificationlist/by-id/{id}")]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var (Success, Message) = await _svc.DeleteNotificationListAsync(id, ct);

            if (!Success)
                return NotFound(Message);

            return NoContent(); // ✅ 204 No Content jika berhasil dihapus permanen
        }

        [HttpGet("email-sender")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var resp = await _svc.GetSenderEmailAsync(ct);
            if (!resp.Success || resp.Data is null)
                return NotFound(new { message = resp.Message });

            return Ok(resp); // { success, message, data = "someone@domain.com" }
        }
        
        [HttpPut("update-email-sender")]
        public async Task<IActionResult> Update([FromBody] UpdateEmailSenderRequest req, CancellationToken ct)
        {
            var resp = await _svc.UpdateSenderEmailAsync(req.Email ?? string.Empty, ct);
            if (!resp.Success) return BadRequest(new { message = resp.Message });
            return Ok(resp);
        }
    }
}
