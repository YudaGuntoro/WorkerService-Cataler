using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.MasterData;
using Web.API.Mappings.Response;

namespace Web.API.Persistence.Services
{
    public interface IMasterDataService
    {
        Task<ApiResponse<List<CardNoMaster>>> GetCardNoMastersAsync(
           int page = 1,
           int limit = 10,
           int? id = null,              
           string? lineNo = null,
           string? productName = null,
           string? materialName = null);

        Task<ApiResponse<CardNoMaster?>> GetCardNoMasterByIdAsync(int id);
        Task<ApiResponse<List<GetSubProductMasterListDto>>> GetProductNamesByLineNoAsync(string lineNo);
        Task<(bool Success, string? Message)> ImportExcelAsync(IFormFile file);
        Task<(bool Success, string? Message)> UpdateAsync(int id, CardNoMasterUpdateDto dto);
        Task<(bool Success, string? Message)> InsertCardNoMasterAsync(CardNoMaster dto);
        Task<ApiResponse<List<ProductMaster>>> GetProductMastersAsync(int LineNo);
        Task<ApiResponse<CardNoMaster?>> GetByCardNoAsync(int cardNo,int LineNo);
        Task<(bool Success, string? Message)> SoftDeleteAsync(int id);
        Task<(bool Success, string? Message, byte[]? Bytes, string? FileName, string TemplatePath)>ExportCardNoMastersAsync(string templatePath, CancellationToken ct = default);
    }
}
