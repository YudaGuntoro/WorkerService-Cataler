using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.API.Mappings.DTOs.HistoryList;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface ILogCycleService
    {
        Task<ApiResponse<List<GetHistoryListCycleTimeDto>>> GetAllAsync(
                int page = 1,
                int limit = 10,
                int? lineNo = null,
                DateTime? date = null,
                DateTime? startDate = null,
                DateTime? endDate = null);

        // === NEW: Export LogCycleTime ke Excel berbasis template MiniExcel ===
        Task<(bool Success, string? Message, byte[]? Bytes, string? FileName)> ExportCycleTimeAsync(
            int page = 1,
            int? limit = null,          // default 100 bila null
            int? lineNo = null,
            DateTime? date = null,
            DateTime? startDate = null,
            DateTime? endDate = null);

    }
}
