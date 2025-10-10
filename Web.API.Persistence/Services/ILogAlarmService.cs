using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.API.Mappings.DTOs.HistoryList;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface ILogAlarmService
    {
        Task<ApiResponse<List<GetAlarmLogDto>>> GetAllAsync(
          int page = 1,
          int limit = 10,
          int? lineNo = null,
          DateTime? startDate = null,
          DateTime? endDate = null);

        Task<(bool Success, string? Message, byte[]? Bytes, string? FileName)> ExportFailureDetailsAsync(
          string templatePath,
          int page = 1,
          int? limit = null,          // default 100 jika null
          int? lineNo = null,
          DateTime? date = null,
          DateTime? startDate = null,
          DateTime? endDate = null,
          string? keyword = null
        );
    }
}
