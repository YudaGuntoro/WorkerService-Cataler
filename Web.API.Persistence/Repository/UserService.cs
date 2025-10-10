using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Web.API.Domain.Entities;
using Web.API.Persistence.Context;
using NLog;
using Web.API.Persistence.Services;
using Web.API.Mappings.Request;
using Web.API.Mappings.DTOs.Auth;
using MySqlConnector;
using Web.API.Mappings.DTOs.Notification;
using Web.API.Mappings.Response;
using System.Net.Mail;

namespace Web.API.Persistence.Repository
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;

        public UserService(AppDbContext context, IMapper mapper, IConfiguration config)
        {
            _context = context;
            _mapper = mapper;
            _config = config;
        }

        public async Task<ApiResponse<List<UserListItemDto>>> GetUsersAsync(
        string? search = null,
        string? role = null,
        bool? isActive = null,
        int page = 1,
        int limit = 10,
        CancellationToken ct = default)
        {
            page = Math.Max(1, page);
            limit = Math.Max(1, limit);

            var resp = new ApiResponse<List<UserListItemDto>> { Data = new() };

            try
            {
                var q = _context.Users.AsNoTracking().AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var kw = search.Trim().ToLower();
                    q = q.Where(u => u.UserName.ToLower().Contains(kw));
                }

                if (!string.IsNullOrWhiteSpace(role))
                {
                    q = q.Where(u => (u.Role ?? "") == role);
                }

                if (isActive.HasValue)
                {
                    // kolom IsActive bertipe bool? di kode-mu
                    q = q.Where(u => (u.IsActive ?? false) == isActive.Value);
                }

                var total = await q.CountAsync(ct);

                var data = await q
                    .OrderBy(u => u.UserName)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(u => new UserListItemDto
                    {
                        Id = u.Id.ToString(),
                        UserName = u.UserName,
                        Role = u.Role,
                        IsActive = u.IsActive ?? false
                    })
                    .ToListAsync(ct);

                resp.Success = true;
                resp.Message = "Get users success";
                resp.Data = data;
                resp.Pagination = new Pagination
                {
                    Curr_Page = page,
                    Limit = limit,
                    Total = total,
                    Total_Page = total == 0 ? 0 : (int)Math.Ceiling((double)total / limit)
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetUsersAsync error");
                resp.Success = false;
                resp.Message = $"Exception: {ex.Message}";
                resp.Data = new();
                resp.Pagination = new Pagination { Curr_Page = page, Limit = limit, Total = 0, Total_Page = 0 };
            }

            return resp;
        }
        public async Task<AuthResult?> AuthenticateAsync(string userName, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == userName && (u.IsActive ?? false));

                if (user == null)
                {
                    _logger.Warn($"Login gagal: user '{userName}' tidak ditemukan atau tidak aktif.");
                    return null;
                }

                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    _logger.Warn($"Login gagal: password salah untuk user '{userName}'.");
                    return null;
                }

                _logger.Info($"Login sukses: user '{userName}'.");

                var (token, expiresAt) = GenerateJwtToken(user);

                return new AuthResult
                {
                    Token = token,
                    ExpiresAt = expiresAt,
                    UserId = user.Id.ToString(),
                    UserName = user.UserName,
                    Role = user.Role ?? "User"
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error saat proses login untuk user '{userName}'.");
                throw;
            }
        }
        private (string token, DateTime expiresAt) GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey))
                throw new InvalidOperationException("JWT Key tidak ditemukan di konfigurasi.");

            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresAt = DateTime.UtcNow.AddHours(2);

            var claims = new[]
            {
        new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("UserId", user.Id.ToString()),
        new Claim("Role", user.Role ?? "User")
    };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }

        public async Task<(bool Success, string? ErrorMessage)> CreateUserAsync(RegisterUser request)
        {
            try
            {
                var normalizedUserName = request.UserName?.Trim();
                if (string.IsNullOrWhiteSpace(normalizedUserName))
                    return (false, "Username wajib diisi.");

                if (await _context.Users.AnyAsync(u => u.UserName == normalizedUserName))
                    return (false, $"Username '{normalizedUserName}' sudah dipakai.");

                var email = request.Email?.Trim();
                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email wajib diisi.");

                if (string.IsNullOrWhiteSpace(request.Password))
                    return (false, "Password wajib diisi.");

                // 🔒 Kunci role admin
                if (!string.IsNullOrWhiteSpace(request.Role) &&
                    request.Role.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Tidak boleh mendaftarkan user dengan role 'admin'.");
                }

                var user = new User
                {
                    UserId = Guid.NewGuid().ToString("N")[..20],
                    UserName = normalizedUserName,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Email = email,
                    Role = string.IsNullOrWhiteSpace(request.Role) ? "user" : request.Role!.Trim(),
                    IsActive = true
                };

                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                _logger.Info("User {User} berhasil dibuat.", normalizedUserName);
                return (true, null);
            }
            catch (DbUpdateException dbEx)
            {
                var dbMsg = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.Error(dbEx, "Gagal membuat user (DB error)");
                return (false, dbMsg);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Gagal membuat user (General error)");
                return (false, ex.Message);
            }
        }


        public async Task<(bool Success, string? Message)> CreateUserNameOnlyAsync(CreateUserNameOnlyRequest request)
        {
            try
            {
                var name = (request.UserName ?? string.Empty).Trim();
                var role = (request.Role ?? string.Empty).Trim();

                if (string.IsNullOrWhiteSpace(name))
                    return (false, "Username wajib diisi.");
                if (string.IsNullOrWhiteSpace(role))
                    return (false, "Role wajib diisi.");

                // ❌ Larang role admin via endpoint ini
                if (role.Equals("admin", StringComparison.OrdinalIgnoreCase))
                    return (false, "Pendaftaran role 'admin' tidak diperbolehkan melalui endpoint ini.");

                // ✅ Cek apakah role ada di tabel rolelist
                var roleExists = await _context.Rolelists
                    .AsNoTracking()
                    .AnyAsync(r => r.Role == role);
                if (!roleExists)
                    return (false, $"Role '{role}' tidak sesuai (tidak terdaftar di rolelist).");

                // ✅ Cek unik username (case-insensitive sesuai collate DB)
                var exists = await _context.Users.AsNoTracking()
                    .AnyAsync(u => EF.Functions.Collate(u.UserName, "utf8mb4_0900_ai_ci") == name);
                if (exists)
                    return (false, $"Username '{name}' sudah dipakai.");

                // ✅ Buat entity baru
                var entity = new User
                {
                    UserId = Guid.NewGuid().ToString("N").Substring(0, 20),
                    UserName = name,
                    Role = role,
                    IsActive = true,
                    Email = null,
                    PasswordHash = null,
                    PhoneNumber = null
                };

                _context.Users.Add(entity);
                await _context.SaveChangesAsync();

                _logger.Info($"User '{name}' berhasil dibuat dengan role '{role}'.");
                return (true, null);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message?.IndexOf("Duplicate entry", StringComparison.OrdinalIgnoreCase) >= 0
                || (ex.InnerException is MySqlException mysqlEx && mysqlEx.Number == 1062))
            {
                return (false, "Username sudah dipakai (duplikat saat commit).");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CreateUserNameOnlyAsync failed");
                return (false, $"Problem lain: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
        public async Task<bool> UpdateUserAsync(UpdateUser request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id);

                if (user == null)
                    return false;

                user.UserName = request.UserName;
                user.Role = request.Role;
                user.IsActive = request.IsActive;

                // Kalau password ada di request, hash ulang
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.Info($"User {user.UserId} berhasil diupdate.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Gagal mengupdate user.");
                return false;
            }
        }

        // ------ Soft Delete (IsActive = 0)
        public async Task<bool> DeleteUserAsync(uint id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                    return false;

                // Soft delete
                user.IsActive = false;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.Info($"User {id} berhasil di-nonaktifkan (soft delete).");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Gagal menonaktifkan user {id}.");
                return false;
            }
        }

        public async Task<List<Rolelist>> GetRolesAsync()
        {
            return await _context.Rolelists
                .AsNoTracking()
                .ToListAsync();
        }

        // === GET ALL (filter by Type optional) ===
        public async Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(
            int? type = null,
            CancellationToken ct = default)
        {
            var resp = new ApiResponse<List<NotificationDto>> { Data = new() };
            try
            {
                var q = _context.Notifications.AsNoTracking().AsQueryable();

                if (type.HasValue)
                    q = q.Where(n => n.Type == type.Value);

                var list = await q
                    .OrderByDescending(n => n.Id)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Name = n.Name,
                        Target = n.Target,
                        Type = n.Type,
                        Problems = n.Problems,
                        ChangeOver = n.ChangeOver,
                        FacilityCount = n.FacilityCount
                    })
                    .ToListAsync(ct);

                resp.Success = true;
                resp.Message = "Get notifications success";
                resp.Data = list;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetNotificationsAsync error");
                resp.Success = false;
                resp.Message = ex.Message;
            }
            return resp;
        }

        // === CREATE ===
        public async Task<ApiResponse<NotificationDto>> CreateNotificationAsync(
         CreateNotificationDto input,
         CancellationToken ct = default)
        {
            var resp = new ApiResponse<NotificationDto>();
            try
            {
                var entity = new Notification
                {
                    Name = input.Name,
                    Target = input.Target,
                    Type = input.Type,
                    Problems = input.Problems,
                    ChangeOver = input.ChangeOver,
                    FacilityCount = input.FacilityCount
                };

                await _context.Notifications.AddAsync(entity, ct);
                await _context.SaveChangesAsync(ct);

                resp.Success = true;
                resp.Message = "Notification created";
                resp.Data = new NotificationDto
                {
                    Name = entity.Name,
                    Target = entity.Target,
                    Type = entity.Type,
                    Problems = entity.Problems,
                    ChangeOver = entity.ChangeOver,
                    FacilityCount = entity.FacilityCount
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CreateNotificationAsync error");

                resp.Success = false;

                // Ambil pesan inner exception kalau ada
                var innerMsg = ex.InnerException?.Message ?? string.Empty;
                resp.Message = string.IsNullOrWhiteSpace(innerMsg)
                    ? ex.Message
                    : $"{ex.Message} | Inner: {innerMsg}";
            }

            return resp;
        }

        public async Task<ApiResponse<int>> UpdateNotificationAsync(
    int id,
    UpdateNotificationRequest req,
    CancellationToken ct = default)
        {
            var resp = new ApiResponse<int> { Data = 0 };
            try
            {
                var entity = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
                if (entity is null)
                {
                    resp.Success = false;
                    resp.Message = $"Notification Id={id} not found.";
                    return resp;
                }

                var entry = _context.Entry(entity);
                var changed = false;

                if (req.Name != null)
                {
                    entity.Name = req.Name; entry.Property(e => e.Name).IsModified = true; changed = true;
                }
                if (req.Target != null)
                {
                    entity.Target = req.Target.Trim(); entry.Property(e => e.Target).IsModified = true; changed = true;
                }
                if (req.Type!=null)
                {
                    entity.Type = req.Type; entry.Property(e => e.Type).IsModified = true; changed = true;
                }
                if (req.Problems !=null)
                {
                    entity.Problems = req.Problems; entry.Property(e => e.Problems).IsModified = true; changed = true;
                }
                if (req.ChangeOver != null)
                {
                    entity.ChangeOver = req.ChangeOver; entry.Property(e => e.ChangeOver).IsModified = true; changed = true;
                }
                if (req.FacilityCount != null)
                {
                    entity.FacilityCount = req.FacilityCount; entry.Property(e => e.FacilityCount).IsModified = true; changed = true;
                }

                if (!changed)
                {
                    resp.Success = false;
                    resp.Message = "Tidak ada field yang diubah.";
                    return resp;
                }

                var affected = await _context.SaveChangesAsync(ct);
                resp.Success = affected > 0;
                resp.Message = affected > 0 ? "Notification updated" : "Tidak ada record yang terubah.";
                resp.Data = affected;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "UpdateNotificationAsync error");
                return new ApiResponse<int> { Success = false, Message = ex.Message, Data = 0 };
            }
        }

        public async Task<ApiResponse<List<UserListItemDto>>> GetUserListAsync(
            string? search = null,
            bool? isActive = null,
            CancellationToken ct = default)
        {
            var resp = new ApiResponse<List<UserListItemDto>> { Data = new() };

            try
            {
                var q = _context.Users.AsNoTracking().AsQueryable();

                // exclude admin
                q = q.Where(u => (u.Role ?? "").ToLower() != "admin");

                // optional search
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var kw = search.Trim().ToLower();
                    q = q.Where(u => u.UserName.ToLower().Contains(kw));
                }

                // optional filter isActive
                if (isActive.HasValue)
                    q = q.Where(u => (u.IsActive ?? false) == isActive.Value);

                var data = await q
                    .OrderBy(u => u.UserName)
                    .Select(u => new UserListItemDto
                    {
                        Id = u.Id.ToString(),
                        UserName = u.UserName,
                        Role = u.Role,
                        IsActive = u.IsActive ?? false
                    })
                    .ToListAsync(ct);

                resp.Success = true;
                resp.Message = "Get user list success (excluding admin)";
                resp.Data = data;
                resp.Pagination = null; // tidak pakai pagination
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetUserListAsync error");
                resp.Success = false;
                resp.Message = $"Exception: {ex.Message}";
                resp.Data = new();
            }
            return resp;
        }

        public async Task<(bool Success, string? Message)> DeleteNotificationListAsync(int id, CancellationToken ct = default)
        {
            try
            {
                var entity = await _context.Notifications.FindAsync(new object[] { id }, ct);
                if (entity == null)
                    return (false, "Data tidak ditemukan.");

                _context.Notifications.Remove(entity);
                await _context.SaveChangesAsync(ct);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DeleteAsync error");
                return (false, $"Gagal menghapus data: {ex.Message}");
            }
        }
        public async Task<ApiResponse<string?>> GetSenderEmailAsync(CancellationToken ct = default)
        {
            var resp = new ApiResponse<string?>();
            try
            {
                var email = await _context.EmailSenders
                    .AsNoTracking()
                    .OrderBy(e => e.Email)           // atau tanpa ORDER BY kalau tidak perlu deterministik
                    .Select(e => e.Email)
                    .FirstOrDefaultAsync(ct);

                if (string.IsNullOrWhiteSpace(email))
                {
                    resp.Success = false;
                    resp.Message = "No email found.";
                    resp.Data = null;
                    return resp;
                }

                resp.Success = true;
                resp.Message = "OK";
                resp.Data = email;
                return resp;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetSenderEmailAsync error");
                return new ApiResponse<string?> { Success = false, Message = ex.Message, Data = null };
            }
        }

        public async Task<ApiResponse<int>> UpdateSenderEmailAsync(string email, CancellationToken ct = default)
        {
            var resp = new ApiResponse<int> { Data = 0 };

            try
            {
                var newEmail = email?.Trim();
                if (string.IsNullOrWhiteSpace(newEmail))
                    return new ApiResponse<int> { Success = false, Message = "Email wajib diisi.", Data = 0 };

                // Validasi format email sederhana
                try { _ = new MailAddress(newEmail); }
                catch { return new ApiResponse<int> { Success = false, Message = "Format email tidak valid.", Data = 0 }; }

                using var tx = await _context.Database.BeginTransactionAsync(ct);

                // UPDATE baris yang ada (ambil satu saja)
                var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE email_sender SET email = {newEmail} LIMIT 1", ct);

                // Jika belum ada baris sama sekali → INSERT satu baris
                if (affected == 0)
                {
                    affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"INSERT INTO email_sender (email) VALUES ({newEmail})", ct);
                }

                await tx.CommitAsync(ct);

                resp.Success = true;
                resp.Message = "Email sender updated.";
                resp.Data = affected; // 1 untuk update/insert
                return resp;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "UpdateSenderEmailAsync error");
                return new ApiResponse<int> { Success = false, Message = ex.Message, Data = 0 };
            }
        }
    }
}
