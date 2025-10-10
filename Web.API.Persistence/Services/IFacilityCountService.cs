using Web.API.Mappings.DTOs.FacilityCount;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface IFacilityCountService
    {
        Task<ApiResponse<List<FacilityCountRealtimeDto>>> GetLiveAsync(
            string machineKey,
            string topic,
            int lineMasterId,
            int page = 1,
            int limit = 10,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken ct = default);

        Task<ApiResponse<int>> CreateSnapshotAsync(
            string machineKey,
            string topic,
            int lineMasterId,
            CancellationToken ct = default);

        Task<ApiResponse<int>> UpdateDeviceConfigByIdAsync(
            int id,
            string? newDevice = null,
            string? category = null,
            long? limitValue = null,
            CancellationToken ct = default);

        Task<(bool Success, string? Message, byte[]? Bytes, string? FileName)> ExportFacilityCountAsync(
            string templatePath,
            int? lineNo = null,
            CancellationToken ct = default);

    }
}
