using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Web.API.Domain.Entities;
using Web.API.Mappings.Response;
using Web.API.Persistence.Context;
using Web.API.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Web.API.Mappings.DTOs.ProductionPlan;
using Mapster;
using Microsoft.AspNetCore.Http;
using MiniExcelLibs;
using Web.API.Mappings.Request;
using MapsterMapper;
using Web.API.Mappings.Export;

namespace Web.API.Persistence.Repository
{
    public class ProductionPlanService : IProductionPlanService
    {
        private readonly AppDbContext _context;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IMapper _mapper;
        public ProductionPlanService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ApiResponse<List<GetProductionPlanDto>>> GetAllAsync(
            DateOnly? date = null,
            DateOnly? startDate = null,
            DateOnly? endDate = null,
            int? lineNo = null,
            int page = 1,
            int limit = 10) // default 10
        {
            page = Math.Max(page, 1);

            var response = new ApiResponse<List<GetProductionPlanDto>> { Data = new() };

            try
            {
                var query =
                    from plan in _context.ProductionPlanMasters.AsNoTracking()
                    join line in _context.LineMasters.AsNoTracking()
                        on plan.LineMasterId equals line.Id into lineGroup
                    from line in lineGroup.DefaultIfEmpty()
                    join product in _context.ProductMasters.AsNoTracking()
                        on plan.ProductMasterId equals product.Id into productGroup
                    from product in productGroup.DefaultIfEmpty()
                    join status in _context.WorkStatusMasters.AsNoTracking()
                        on plan.WorkStatusMasterId equals status.Id into statusGroup
                    from status in statusGroup.DefaultIfEmpty()
                        // === LEFT JOIN ke ProductionHistory ===
                    join hist in _context.ProductionHistories.AsNoTracking()
                        on plan.Id equals hist.ProductionPlanId into histGroup
                    from hist in histGroup.OrderByDescending(h => h.Timestamp).Take(1).DefaultIfEmpty()
                    select new
                    {
                        Plan = plan,
                        Line = line,
                        Product = product,
                        Status = status,
                        ActualQty = hist != null ? hist.ActualQty : 0
                    };

                // Filter tanggal
                if (date.HasValue)
                {
                    query = query.Where(x => x.Plan.PlanDate == date.Value);
                }
                else
                {
                    if (startDate.HasValue) query = query.Where(x => x.Plan.PlanDate >= startDate.Value);
                    if (endDate.HasValue) query = query.Where(x => x.Plan.PlanDate <= endDate.Value);
                }

                // Filter line
                if (lineNo.HasValue)
                    query = query.Where(x => x.Line != null && x.Line.LineNo == lineNo.Value);

                // Hitung total data
                var totalData = await query.CountAsync();

                // Ambil data dengan pagination
                var skip = (page - 1) * limit;
                var data = await query
                    .OrderByDescending(x => x.Plan.PlanDate)
                    .ThenByDescending(x => x.Plan.Id)
                    .Skip(skip)
                    .Take(limit)
                    .Select(x => new GetProductionPlanDto
                    {
                        Id = x.Plan.Id,
                        LineName = x.Line != null ? x.Line.LineName : null,
                        ProductName = x.Product != null ? x.Product.ProductName : null,
                        PlanDate = x.Plan.PlanDate,
                        PlanQty = x.Plan.PlanQty,
                        ActualQty = x.ActualQty, // kalau null → 0
                        statusLabel = x.Status != null ? x.Status.StatusLabel : "Unknown",
                        CreatedAt = x.Plan.CreatedAt
                    })
                    .ToListAsync();

                response.Success = true;
                response.Message = "Get production plans success";
                response.Data = data;
                response.Pagination = new Pagination
                {
                    Curr_Page = page,
                    Limit = limit,
                    Total_Page = (int)Math.Ceiling((double)totalData / limit)
                };
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
                response.Data = new List<GetProductionPlanDto>();
            }

            return response;
        }

        public async Task<(bool Success, string? Message)> CreateAsync(ProductionPlanCreateDto dto)
        {
            try
            {
                var lineExists = await _context.LineMasters.AnyAsync(x => x.Id == dto.LineMasterId);
                if (!lineExists)
                    return (false, $"LineMaster with ID {dto.LineMasterId} not found.");

                var productExists = await _context.ProductMasters.AnyAsync(x => x.Id == dto.ProductMasterId);
                if (!productExists)
                    return (false, $"ProductMaster with ID {dto.ProductMasterId} not found.");

                var plan = new ProductionPlanMaster
                {
                    LineMasterId = dto.LineMasterId,
                    ProductMasterId = dto.ProductMasterId,
                    PlanDate = dto.PlanDate,
                    PlanQty = dto.PlanQty,
                    WorkStatusMasterId = 1,
                    CreatedAt = DateTime.Now
                };

                _context.ProductionPlanMasters.Add(plan);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error creating production plan.");
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Message)> UpdateAsync(int id, ProductionPlanMaster dto)
        {
            try
            {
                // Cek apakah data plan dengan ID tersebut ada
                var existingPlan = await _context.ProductionPlanMasters.FindAsync(id);
                if (existingPlan == null)
                    return (false, $"Production plan with ID {id} not found.");

                // Validasi LineMaster
                var lineExists = await _context.LineMasters.AnyAsync(x => x.Id == dto.LineMasterId);
                if (!lineExists)
                    return (false, $"LineMaster with ID {dto.LineMasterId} not found.");

                // Validasi ProductMaster
                var productExists = await _context.ProductMasters.AnyAsync(x => x.Id == dto.ProductMasterId);
                if (!productExists)
                    return (false, $"ProductMaster with ID {dto.ProductMasterId} not found.");

                // Update data
                existingPlan.LineMasterId = dto.LineMasterId;
                existingPlan.ProductMasterId = dto.ProductMasterId;
                existingPlan.PlanDate = dto.PlanDate;
                existingPlan.PlanQty = dto.PlanQty;
                existingPlan.CreatedAt = DateTime.Now; // Optional: update waktu terakhir diubah

                _context.ProductionPlanMasters.Update(existingPlan);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error updating production plan with ID {id}.");
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string? Message)> DeleteAsync(int id)
        {
            try
            {
                var plan = await _context.ProductionPlanMasters.FindAsync(id);
                if (plan == null)
                    return (false, $"Production plan with ID {id} not found.");

                _context.ProductionPlanMasters.Remove(plan);
                await _context.SaveChangesAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error deleting production plan.");
                return (false, $"Unexpected error: {ex.Message}");
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<ProductMasterDto>> GetAllProductMaster()
        {
            var result = await _context.ProductMasters
                        .AsNoTracking()
                        .ProjectToType<ProductMasterDto>() // ← Mapster in action
                        .ToListAsync();

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<LineMasterDto>> GetAllLineMaster()
        {
            var result = await _context.LineMasters
                       .AsNoTracking()
                       .ProjectToType<LineMasterDto>() // ← Mapster in action
                       .ToListAsync();

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        /*public async Task<(bool Success, string? Message)> ImportExcelAsync(IFormFile file)
        {
            try
            {
                //Tentukan folder "UploadFile" di lokasi app
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFilePlan");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var extension = Path.GetExtension(file.FileName); // .xlsx
               
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName); // tanpa ekstensi
                var timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss"); // format aman
                var savedFileName = $"{originalFileName}_{timestamp}{extension}";
                var savedFilePath = Path.Combine(uploadFolder, savedFileName);

                using (var fileStream = new FileStream(savedFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                
                DateTime? parsedDateFromName = null;
                var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                var datePart = nameWithoutExt.Split('_').Last(); // ambil bagian terakhir setelah "_"
                if (DateTime.TryParse(datePart, out DateTime parsed))
                {
                    parsedDateFromName = parsed;
                }

                // Baca isi Excel dari file yang sudah tersimpan
                using var stream = new FileStream(savedFilePath, FileMode.Open, FileAccess.Read);
                var rows = await MiniExcel.QueryAsync<ProductionPlanUploadExcel>(stream);
                var list = rows.ToList();

                var errors = new List<string>();
                var lineNameToId = new Dictionary<string, int>();
                var productNameToId = new Dictionary<string, int>();

                // ====== VALIDASI ======
                foreach (var dto in list)
                {
                    if (string.IsNullOrWhiteSpace(dto.LINE_NO))
                    {
                        errors.Add("Kolom LINE_NO tidak boleh kosong.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(dto.PRODUCT_NAME))
                    {
                        errors.Add("Kolom PRODUCT_NAME tidak boleh kosong.");
                        continue;
                    }
                    if (dto.DATE == null || string.IsNullOrWhiteSpace(dto.DATE.ToString()))
                    {
                        errors.Add($"Kolom DATE tidak boleh kosong (LINE_NO: {dto.LINE_NO}).");
                        continue;
                    }

                    if (!lineNameToId.ContainsKey(dto.LINE_NO))
                    {
                        if (!short.TryParse(dto.LINE_NO, out short lineNo))
                        {
                            errors.Add($"LINE_NO '{dto.LINE_NO}' tidak valid.");
                            continue;
                        }

                        var line = await _context.LineMasters
                            .FirstOrDefaultAsync(x => x.LineNo == lineNo);

                        if (line == null)
                        {
                            errors.Add($"LineMaster dengan no '{dto.LINE_NO}' tidak ditemukan.");
                            continue;
                        }
                        lineNameToId[dto.LINE_NO] = line.Id;
                    }

                    if (!productNameToId.ContainsKey(dto.PRODUCT_NAME))
                    {
                        var product = await _context.ProductMasters
                            .FirstOrDefaultAsync(x => x.ProductName == dto.PRODUCT_NAME);

                        if (product == null)
                        {
                            errors.Add($"ProductMaster dengan nama '{dto.PRODUCT_NAME}' tidak ditemukan.");
                            continue;
                        }
                        productNameToId[dto.PRODUCT_NAME] = product.Id;
                    }
                }

                if (errors.Any())
                {
                    return (false, string.Join(" | ", errors));
                }

                // ====== INSERT DATA ======
                foreach (var dto in list)
                {
                    var planDate = DateOnly.FromDateTime(Convert.ToDateTime(dto.DATE));

                    var plan = new ProductionPlanMaster
                    {
                        LineMasterId = lineNameToId[dto.LINE_NO],
                        ProductMasterId = productNameToId[dto.PRODUCT_NAME],
                        PlanDate = planDate,
                        PlanQty = Convert.ToInt16(dto.QUANTITY_TARGET),
                        WorkStatusMasterId = 1,
                        CreatedAt = DateTime.Now,
                        FileName = originalFileName,
                        DateFile = DateOnly.FromDateTime(parsedDateFromName ?? DateTime.Now)
                    };

                    _context.ProductionPlanMasters.Add(plan);
                }

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Gagal mengimpor data produksi dari Excel.");
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }*/

        public async Task<(bool Success, string? Message)> ImportExcelAsync(IFormFile file)
        {
            try
            {
                //Tentukan folder "UploadFile" di lokasi app
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFilePlan");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                var extension = Path.GetExtension(file.FileName); // .xlsx

                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName); // tanpa ekstensi
                var timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss"); // format aman
                var savedFileName = $"{originalFileName}_{timestamp}{extension}";
                var savedFilePath = Path.Combine(uploadFolder, savedFileName);

                using (var fileStream = new FileStream(savedFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                DateTime? parsedDateFromName = null;
                var nameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                var datePart = nameWithoutExt.Split('_').Last(); // ambil bagian terakhir setelah "_"
                if (DateTime.TryParse(datePart, out DateTime parsed))
                {
                    parsedDateFromName = parsed;
                }

                // Baca isi Excel dari file yang sudah tersimpan
                using var stream = new FileStream(savedFilePath, FileMode.Open, FileAccess.Read);
                var rows = await MiniExcel.QueryAsync<ProductionPlanUploadExcel>(stream);
                var list = rows.ToList();

                var errors = new List<string>();
                var lineNameToId = new Dictionary<string, int>();

                // ✅ GANTI: key komposit utk Product (PRODUCT_NAME + LINE_NO)
                var productKeyToId = new Dictionary<string, int>();

                // ====== VALIDASI ======
                foreach (var dto in list)
                {
                    if (string.IsNullOrWhiteSpace(dto.LINE_NO))
                    {
                        errors.Add("Kolom LINE_NO tidak boleh kosong.");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(dto.PRODUCT_NAME))
                    {
                        errors.Add("Kolom PRODUCT_NAME tidak boleh kosong.");
                        continue;
                    }
                    if (dto.DATE == null || string.IsNullOrWhiteSpace(dto.DATE.ToString()))
                    {
                        errors.Add($"Kolom DATE tidak boleh kosong (LINE_NO: {dto.LINE_NO}).");
                        continue;
                    }

                    // Parse lineNo tiap iterasi (dibutuhkan juga utk Product lookup)
                    if (!short.TryParse(dto.LINE_NO, out short lineNoShort))
                    {
                        errors.Add($"LINE_NO '{dto.LINE_NO}' tidak valid.");
                        continue;
                    }

                    if (!lineNameToId.ContainsKey(dto.LINE_NO))
                    {
                        var line = await _context.LineMasters
                            .FirstOrDefaultAsync(x => x.LineNo == lineNoShort);

                        if (line == null)
                        {
                            errors.Add($"LineMaster dengan no '{dto.LINE_NO}' tidak ditemukan.");
                            continue;
                        }
                        lineNameToId[dto.LINE_NO] = line.Id;
                    }

                    // ✅ Lookup Product dengan kombinasi (ProductName, LineNo) + filter IsDeleted
                    var productKey = $"{dto.PRODUCT_NAME}|{dto.LINE_NO}";
                    if (!productKeyToId.ContainsKey(productKey))
                    {
                        var product = await _context.ProductMasters
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.ProductName == dto.PRODUCT_NAME &&
                                x.LineNo == lineNoShort &&
                                (x.IsDeleted == null || x.IsDeleted == 0));

                        if (product == null)
                        {
                            errors.Add($"ProductMaster '{dto.PRODUCT_NAME}' (LineNo {dto.LINE_NO}) tidak ditemukan.");
                            continue;
                        }
                        productKeyToId[productKey] = product.Id;
                    }
                }

                if (errors.Any())
                {
                    return (false, string.Join(" | ", errors));
                }

                // ====== INSERT DATA ======
                foreach (var dto in list)
                {
                    var planDate = DateOnly.FromDateTime(Convert.ToDateTime(dto.DATE));

                    // Ambil LineMasterId dari cache
                    var lineMasterId = lineNameToId[dto.LINE_NO];

                    // Ambil ProductMasterId dari cache komposit (ProductName + LineNo)
                    var productKey = $"{dto.PRODUCT_NAME}|{dto.LINE_NO}";
                    var productMasterId = productKeyToId[productKey];

                    var plan = new ProductionPlanMaster
                    {
                        LineMasterId = lineMasterId,
                        ProductMasterId = productMasterId,
                        PlanDate = planDate,
                        PlanQty = Convert.ToInt16(dto.QUANTITY_TARGET),
                        WorkStatusMasterId = 1,
                        CreatedAt = DateTime.Now,
                        FileName = originalFileName,
                        DateFile = DateOnly.FromDateTime(parsedDateFromName ?? DateTime.Now)
                    };

                    _context.ProductionPlanMasters.Add(plan);
                }

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Gagal mengimpor data produksi dari Excel.");
                return (false, $"Terjadi kesalahan: {ex.Message}");
            }
        }


        public async Task<(bool Success, string? Message, byte[]? Bytes, string FileName)> ExportProductionPlansAsync(
        string templatePath,
        DateOnly? date = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        int? lineNo = null,
        CancellationToken ct = default)
        {
            try
            {
                // === Query utama (tanpa pagination) ===
                var query =
                    from plan in _context.ProductionPlanMasters.AsNoTracking()
                    join line in _context.LineMasters.AsNoTracking()
                        on plan.LineMasterId equals line.Id into lineGroup
                    from line in lineGroup.DefaultIfEmpty()
                    join product in _context.ProductMasters.AsNoTracking()
                        on plan.ProductMasterId equals product.Id into productGroup
                    from product in productGroup.DefaultIfEmpty()
                    join hist in _context.ProductionHistories.AsNoTracking()
                        on plan.Id equals hist.ProductionPlanId into histGroup
                    from hist in histGroup.OrderByDescending(h => h.Timestamp).Take(1).DefaultIfEmpty()
                    select new
                    {
                        LineName = line != null ? line.LineName : null,
                        ProductName = product != null ? product.ProductName : null,
                        PlanDate = plan.PlanDate,
                        PlanQty = plan.PlanQty,
                        ActualQty = hist != null ? hist.ActualQty : 0L
                    };

                // === Filter tanggal ===
                if (date.HasValue)
                {
                    query = query.Where(x => x.PlanDate == date.Value);
                }
                else
                {
                    if (startDate.HasValue) query = query.Where(x => x.PlanDate >= startDate.Value);
                    if (endDate.HasValue) query = query.Where(x => x.PlanDate <= endDate.Value);
                }

                // === Filter line ===
                if (lineNo.HasValue)
                {
                    // kita perlu filter ke LineMasters dulu untuk dapat Id by LineNo
                    var lineIds = await _context.LineMasters
                        .AsNoTracking()
                        .Where(l => l.LineNo == lineNo.Value)
                        .Select(l => l.LineName) // akan dicocokkan dengan hasil query (karena kita pilih LineName)
                        .ToListAsync(ct);

                    if (lineIds.Count > 0)
                        query = query.Where(x => x.LineName != null && lineIds.Contains(x.LineName));
                    else
                        query = query.Where(x => false); // tidak ada line cocok
                }

                var list = await query
                    .OrderByDescending(x => x.PlanDate)
                    .ThenBy(x => x.ProductName)
                    .ToListAsync(ct);

                if (list.Count == 0)
                {
                    var scope = lineNo.HasValue ? $"LineNo={lineNo.Value}" : "ALL";
                    return (false, $"Tidak ada data untuk {scope} dalam periode yang dipilih.", null, "ProductionPlan.xlsx");
                }

                // === Map ke ExportProductionPlan ===
                var rowsExport = list.Select(x => new ExportProductionPlan
                {
                    LineName = x.LineName ?? "-",
                    ProductName = x.ProductName ?? "-",
                    PlanDate = x.PlanDate,                 // DateOnly?
                    PlanQty = x.PlanQty,
                    ActualQty = x.ActualQty
                }).ToList();

                // === Bentuk model utk template ===
                // Template butuh Data.No dan Data.Status juga → kita bentuk dengan anonymous type
                var rowsForTemplate = rowsExport
                    .Select((r, i) => new
                    {
                        No = i + 1,
                        LineName = r.LineName,
                        ProductName = r.ProductName,
                        PlanDate = r.PlanDate.HasValue ? r.PlanDate.Value.ToString("yyyy-MM-dd") : "",
                        PlanQty = r.PlanQty ?? 0,
                        ActualQty = r.ActualQty,
                    })
                    .ToList();

                var periodText = date.HasValue
                    ? date.Value.ToString("yyyy-MM-dd")
                    : $"{(startDate.HasValue ? startDate.Value.ToString("yyyy-MM-dd") : "-")} s/d {(endDate.HasValue ? endDate.Value.ToString("yyyy-MM-dd") : "-")}";

                var model = new
                {
                    ReportDate = DateTime.Now.ToString("dd-MMM-yy HH:mm"),
                    Period = periodText, // kalau templatenya punya placeholder Period silakan dipakai
                    Data = rowsForTemplate,
                    Ddata = rowsForTemplate // jaga-jaga jika ada typo {{Ddata.*}}
                };

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    if (File.Exists(templatePath))
                        await MiniExcel.SaveAsByTemplateAsync(ms, templatePath, model);
                    else
                        await MiniExcel.SaveAsAsync(ms, rowsForTemplate); // fallback: tanpa template

                    bytes = ms.ToArray();
                }

                var fileName = $"ProductionPlan_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return (true, null, bytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ExportProductionPlansAsync error");
                return (false, ex.Message, null, "ProductionPlan.xlsx");
            }
        }

        /// <summary>
        /// Mengambil semua baris ProductionCountMaster untuk satu line, diurutkan per operationHour.
        /// </summary>
        public async Task<(bool Success, List<ProductionCountMaster> Data, string? Message)>GetAllTargetsByLineAsync(int lineNoId, CancellationToken ct = default)
        {
            try
            {
                var items = await _context.ProductionCountMasters
                    .AsNoTracking()
                    .Where(x => x.LineMasterId == lineNoId)
                    .OrderBy(x => x.OperationHour)
                    .ToListAsync(ct);

                if (items.Count == 0)
                    return (false, new List<ProductionCountMaster>(), $"Data kosong untuk line {lineNoId}.");

                return (true, items, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error GetAllTargetsByLineAsync(lineNoId={Line})", lineNoId);
                return (false, new List<ProductionCountMaster>(), $"Unexpected error: {ex.Message}");
            }
        }
    }
} 
