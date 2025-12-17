using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.API.Mappings.DTOs.CoatWidthControl;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface ICoatWidthControlService
    {
        Task<ApiResponse<List<CoatWidthControlDto>>> GetAllAsync(
        int page = 1,
        int limit = 10,
        int? lineMasterId = null,
        string? subProductName = null,
        int? coatingNo = null,                  // ⬅️ tambahin di sini
        DateTime? recordDate = null,
        DateTime? startRecordDate = null,
        DateTime? endRecordDate = null
    );

        Task<ApiResponse<List<CoatWidthControlDto>>> GetByDateRangeAsync(
            DateTime? recordDate = null,
            DateTime? startRecordDate = null,
            DateTime? endRecordDate = null,
            int? lineMasterId = null,
            string? subProductName = null,
            int? coatingNo = null                   // ⬅️ tambahin di sini
        );
        Task<ApiResponse<CoatWidthControlDto?>> GetByIdAsync(int id,CancellationToken ct = default);
        // === CRUD ===
        Task<ApiResponse<CoatWidthControlDto?>> GetByIdAsync(int id);

        Task<(bool Success, string? Message, int? Id)> CreateAsync(CoatWidthControlCreate request);

        Task<(bool Success, string? Message, int? Id)> UpdateAsync(int id, CoatWidthControlCreate request);


        Task<(bool Success, string? Message, double? Kpa)> CalculateKpaRecommendationAsync(CalculateKpaRecommendationRequest request);

        Task<(bool Success, string? Message)> DeleteAsync(int id);

        Task<ApiResponse<CoatWidthControlDto?>> GetLatestBySubProductNameAsync(string subProductName);
    }
}
