using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.Auth;
using Web.API.Mappings.DTOs.Notification;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface IUserService
    {
        Task<AuthResult?> AuthenticateAsync(string userName, string password);
        Task<(bool Success, string? ErrorMessage)> CreateUserAsync(RegisterUser request);
        Task<bool> UpdateUserAsync(UpdateUser request);
        Task<bool> DeleteUserAsync(uint Id);
        Task<(bool Success, string? Message)> CreateUserNameOnlyAsync(CreateUserNameOnlyRequest request);
        Task<List<Rolelist>> GetRolesAsync();

        Task<ApiResponse<List<NotificationDto>>> GetNotificationsAsync(
        int? type = null,
        CancellationToken ct = default);

        Task<ApiResponse<NotificationDto>> CreateNotificationAsync(
            CreateNotificationDto input,
            CancellationToken ct = default);

        // UPDATE
        Task<ApiResponse<int>> UpdateNotificationAsync(
            int id,
            UpdateNotificationRequest req,
            CancellationToken ct = default);

        Task<ApiResponse<List<UserListItemDto>>> GetUsersAsync(
        string? search = null,       // cari di username (contains)
        string? role = null,         // filter role
        bool? isActive = null,       // filter aktif/nonaktif
        int page = 1,
        int limit = 10,
        CancellationToken ct = default);

        Task<ApiResponse<List<UserListItemDto>>> GetUserListAsync(
        string? search = null,
        bool? isActive = null,
        CancellationToken ct = default);

        Task<(bool Success, string? Message)> DeleteNotificationListAsync(
        int id,
        CancellationToken ct = default);

        Task<ApiResponse<string?>> GetSenderEmailAsync(CancellationToken ct = default);
        Task<ApiResponse<int>> UpdateSenderEmailAsync(string email, CancellationToken ct = default);
    }
}
