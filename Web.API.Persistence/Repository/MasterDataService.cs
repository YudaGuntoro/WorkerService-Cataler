using MapsterMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.API.Domain.Entities;
using Web.API.Mappings.Response;
using Web.API.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Web.API.Persistence.Services;
using Microsoft.AspNetCore.Http;
using MiniExcelLibs;
using Web.API.Mappings.Request;
using Mapster;
using Web.API.Mappings.DTOs.MasterData;
using Microsoft.AspNetCore.Routing.Template;

namespace Web.API.Persistence.Repository
{
    public class MasterDataService : IMasterDataService
    {
        private readonly AppDbContext _context;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IMapper _mapper;
        public MasterDataService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ApiResponse<List<CardNoMaster>>> GetCardNoMastersAsync(
            int page = 1,
            int limit = 10,
            int? id = null,                     
            string? lineNo = null,
            string? productName = null,
            string? materialName = null)
        {
            var response = new ApiResponse<List<CardNoMaster>>();
            try
            {
                var query = _context.CardNoMasters
                    .AsNoTracking()
                    .Where(c => c.IsDeleted == null || c.IsDeleted == 0); // filter IsDeleted

                // ====== Tambahkan filter dinamis ======
                if (id.HasValue)
                    query = query.Where(c => c.Id == id.Value);  

                if (!string.IsNullOrEmpty(lineNo))
                    query = query.Where(c => c.LineNo == lineNo);

                static string EscapeLike(string s) => s
                .Replace(@"\", @"\\")
                .Replace("%", @"\%")
                .Replace("_", @"\_");

                if (!string.IsNullOrWhiteSpace(productName))
                {
                    var p = $"%{EscapeLike(productName.Trim())}%";
                    query = query.Where(c => EF.Functions.Like(c.ProductName, p, "\\"));
                }

                if (!string.IsNullOrWhiteSpace(materialName))
                {
                    var m = $"%{EscapeLike(materialName.Trim())}%";
                    query = query.Where(c => EF.Functions.Like(c.MaterialName, m, "\\"));
                }

                var totalItems = await query.CountAsync();

                var data = await query
                    .OrderBy(c => c.Id) // asumsi ada Id sebagai primary key
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                response.Success = true;
                response.Message = "Get CardNoMasters success";
                response.Data = data;
                response.Pagination = new Pagination
                {
                    Curr_Page = page,
                    Limit = limit,
                    Total_Page = (int)Math.Ceiling((double)totalItems / limit)
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());

                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                response.Data = new List<CardNoMaster>();
            }

            return response;
        }

        public async Task<ApiResponse<CardNoMaster?>> GetCardNoMasterByIdAsync(int id)
        {
            var response = new ApiResponse<CardNoMaster?>();

            try
            {
                var data = await _context.CardNoMasters
                    .AsNoTracking()
                    .Where(c => c.Id == id && (c.IsDeleted == null || c.IsDeleted == 0)) // ✅ cek IsDeleted
                    .FirstOrDefaultAsync();

                if (data is null)
                {
                    response.Success = false;
                    response.Message = $"Data dengan Id {id} tidak ditemukan.";
                    response.Data = null;
                }
                else
                {
                    response.Success = true;
                    response.Message = "Get CardNoMaster by Id success";
                    response.Data = data;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());

                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                response.Data = null;
            }

            return response;
        }

        public async Task<ApiResponse<List<GetSubProductMasterListDto>>> GetProductNamesByLineNoAsync(string lineNo)
        {
            var response = new ApiResponse<List<GetSubProductMasterListDto>>();
            try
            {
                var query = _context.CardNoMasters
                    .AsNoTracking()
                    .Where(c => c.LineNo == lineNo && (c.IsDeleted == null || c.IsDeleted == 0)) 
                    .Select(c => new GetSubProductMasterListDto
                    {
                        Id = c.Id,
                        LineNo = Convert.ToInt16(c.LineNo),
                        CardNo = Convert.ToInt16(c.CardNo),
                        ProductName = c.ProductName
                    });

                var data = await query.ToListAsync();

                response.Success = true;
                response.Message = "Get ProductName list success";
                response.Data = data;
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());

                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                response.Data = new List<GetSubProductMasterListDto>();
            }

            return response;
        }

        public async Task<ApiResponse<CardNoMaster?>> GetByCardNoAsync(int cardNo, int lineNo)
        {
            var response = new ApiResponse<CardNoMaster?>();
            try
            {
                var data = await _context.CardNoMasters
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        (c.IsDeleted == null || c.IsDeleted == 0) && // ✅ filter aktif
                        Convert.ToInt32(c.CardNo) == cardNo &&
                        Convert.ToInt32(c.LineNo) == lineNo);

                if (data is null)
                {
                    response.Success = false;
                    response.Message = $"CardNo '{cardNo}' dengan LineNo '{lineNo}' tidak ditemukan atau sudah dihapus.";
                    response.Data = null;
                }
                else
                {
                    response.Success = true;
                    response.Message = "Get CardNoMaster success";
                    response.Data = data;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());

                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                response.Data = null;
            }

            return response;
        }

        public async Task<(bool Success, string? Message)> InsertCardNoMasterAsync(CardNoMaster dto)
        {
            try
            {
                _context.CardNoMasters.Add(dto);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Gagal insert CardNoMaster");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Message)> UpdateAsync(int id, CardNoMasterUpdateDto dto)
        {
            var entity = await _context.CardNoMasters.FindAsync(id);
            if (entity == null)
                return (false, "Data tidak ditemukan.");

            dto.Adapt(entity);  // Map properties dari DTO ke entity

            await _context.SaveChangesAsync();

            return (true, null);
        }

        public async Task<(bool Success, string? Message)> ImportExcelAsync(IFormFile file)
        {
            try
            {
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFileMasterData");
                if (!Directory.Exists(uploadFolder))
                    Directory.CreateDirectory(uploadFolder);

                var extension = Path.GetExtension(file.FileName);
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
                var savedFileName = $"{originalFileName}_{timestamp}{extension}";
                var savedFilePath = Path.Combine(uploadFolder, savedFileName);

                using (var fileStream = new FileStream(savedFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                using var stream = new FileStream(savedFilePath, FileMode.Open, FileAccess.Read);
                var dtos = await MiniExcel.QueryAsync<CardNoMasterUploadExcel>(stream);
                var list = dtos.ToList();

                // Mapster config harus sudah jalan (cek no 1)
                var entities = list.Adapt<List<CardNoMaster>>();
            
                _context.CardNoMasters.AddRange(entities);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? "No inner exception";
                _logger.Error(ex, $"Gagal mengimpor data CardNoMaster dari Excel. Inner Exception: {innerMessage}");
                return (false, $"Terjadi kesalahan: {ex.Message} || Inner Exception: {innerMessage}");
            }
        }

        public async Task<ApiResponse<List<ProductMaster>>> GetProductMastersAsync(int lineNo)
        {
            var response = new ApiResponse<List<ProductMaster>>();
            try
            {
                IQueryable<ProductMaster> query = _context.ProductMasters.AsNoTracking()
                    .Where(x => x.IsDeleted == null || x.IsDeleted == 0);

                if (lineNo > 0)
                {
                    query = query.Where(x => x.LineNo == lineNo);
                }

                var data = await query
                    .OrderBy(x => x.Id)
                    .Select(x => new ProductMaster
                    {
                        Id = x.Id,
                        ProductName = x.ProductName,
                        LineNo = x.LineNo
                    })
                    .ToListAsync();

                response.Success = true;
                response.Message = "Get ProductMasters success";
                response.Data = data;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Gagal mengambil product_master");
                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                response.Data = new List<ProductMaster>();
            }

            return response;
        }


        public async Task<(bool Success, string? Message)> SoftDeleteAsync(int id)
        {
            var entity = await _context.CardNoMasters.FindAsync(id);
            if (entity == null)
                return (false, "Data tidak ditemukan.");

            // Tandai sebagai terhapus
            entity.IsDeleted = 1; // jika bool?: entity.IsDeleted = true;

            // (opsional) jejak waktu/oleh siapa jika kolomnya ada
            // entity.DeletedAt = DateTime.UtcNow;
            // entity.DeletedBy = user;

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool Success, string? Message, byte[]? Bytes, string? FileName, string TemplatePath)>
       ExportCardNoMastersAsync(string templatePath,CancellationToken ct = default)
        {

            try
            {
                // Ambil semua data tanpa filter
                var rows = await _context.CardNoMasters
                    .AsNoTracking()
                    .OrderBy(c => c.Id)
                    .ToListAsync(ct);

                if (rows.Count == 0)
                    return (false, "Data CardNoMasters kosong.", null, null, templatePath);

                // Bentuk data untuk export (rapih + nomor urut)
                var exportRows = rows.Select((c, i) => new
                {
                    No = i + 1,
                    c.LineNo,
                    c.LineName,
                    c.CardNo,
                    c.ProductName,
                    c.MaterialName,
                    c.PartNo,
                    c.MaterialNo,
                    c.SubstrateName,
                    c.TactTime,
                    c.PassHour,
                    c.CoatWidthMin,
                    c.CoatWidthTarget,
                    c.CoatWidthMax,
                    c.SolidityMin,
                    c.SolidityTarget,
                    c.SolidityMax,
                    c.Viscosity100Min,
                    c.Viscosity100Max,
                    c.Viscosity1Min,
                    c.Viscosity1Max,
                    c.PHmin,
                    c.PHmax
                }).ToList();

                // Model untuk MiniExcel Template (pakai exportRows supaya kolom rapi)
                var model = new
                {
                    ReportDate = DateTime.Now.ToString("dd-MMM-yy HH:mm"),
                    Data = exportRows,
                    Ddata = exportRows   // antisipasi placeholder typo {{Ddata.*}}
                };

                using var ms = new MemoryStream();
                if (File.Exists(templatePath))
                {
                    await MiniExcel.SaveAsByTemplateAsync(ms, templatePath, model);
                }
                else
                {
                    // Fallback jika template tidak ada: simpan tabel polos
                    await MiniExcel.SaveAsAsync(ms, rows);
                }

                var bytes = ms.ToArray();
                var fileName = $"CardNoMaster_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return (true, null, bytes, fileName, templatePath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ExportCardNoMastersAsync failed");
                return (false, ex.Message, null, null, templatePath);
            }
        }
    }
}
