using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.ProductionPlan;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface IProductionPlanService
    {

        Task<ApiResponse<List<GetProductionPlanDto>>> GetAllAsync(
         DateOnly? date = null,
         DateOnly? startDate = null,
         DateOnly? endDate = null,
         int? lineNo = null,
         int page = 1,
         int limit = 10);

        Task<(bool Success, string? Message)> CreateAsync(ProductionPlanCreateDto dto);
        Task<(bool Success, string? Message)> UpdateAsync(int id,ProductionPlanMaster dto);
        Task<(bool Success, string? Message)> DeleteAsync(int id);

        Task<List<ProductMasterDto>> GetAllProductMaster();
        Task<List<LineMasterDto>> GetAllLineMaster();
        Task<(bool Success, string? Message)> ImportExcelAsync(IFormFile file);

        Task<(bool Success, string? Message, byte[]? Bytes, string FileName)> ExportProductionPlansAsync(
            string templatePath,
            DateOnly? date = null,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            int? lineNo = null,
            CancellationToken ct = default);

        /// <summary>
        /// Ambil semua target (0–23 jam) untuk satu line.
        /// </summary>
        Task<(bool Success, List<ProductionCountMaster> Data, string? Message)> GetAllTargetsByLineAsync(
            int lineNoId,
            CancellationToken ct = default);
    }
}
