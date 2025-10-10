using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.API.Mappings.DTOs.ProductionCount;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface IProductionCountService
    {
        //// <summary>
        /// Ambil data Production Count (group by LineMaster), 
        /// otomatis filter berdasarkan shift 08:00–08:00 pakai DateTime.Now.
        /// </summary>
        /// <param name="lineNo">Opsional, filter berdasarkan nomor line</param>
        Task<ApiResponse<List<GetHistoryListProductionCountDto>>> GetAllAsync(int? lineNo = null);

        // MASTER - GET list (tetap pakai pagination sesuai kebutuhan sebelumnya)
        Task<ApiResponse<List<ProductionCountMasterDto>>> GetMasterAsync(
            int page = 1,
            int limit = 10,
            int? lineNo = null,
            int? lineMasterId = null);

        // MASTER - PUT by Id
        Task<(bool Success, string? Message)> UpdateMasterAsync(int id, ProductionCountMasterUpdateRequest request);
    }
}