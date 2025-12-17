using Microsoft.EntityFrameworkCore;
using MapsterMapper;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;
using Web.API.Mappings.DTOs.MasterData;
using Web.API.Persistence.Context;
using Web.API.Persistence.Services;
using Web.API.Domain.Entities;
using System.Linq;
using Web.API.Mappings.DTOs.CoatWidthControl;
using Mapster;

namespace Web.API.Persistence.Repository
{
    public class CoatWidthControlService : ICoatWidthControlService
    {
        private readonly AppDbContext _context;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IMapper _mapper;

        public CoatWidthControlService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // === GET ALL (paging) + filters (lineMasterId, subProductName, coatingNo, tanggal) ===
        public async Task<ApiResponse<List<CoatWidthControlDto>>> GetAllAsync(
          int page = 1,
          int limit = 10,
          int? lineMasterId = null,
          string? subProductName = null,
          int? coatingNo = null,
          DateTime? recordDate = null,
          DateTime? startRecordDate = null,
          DateTime? endRecordDate = null)
        {
            var res = new ApiResponse<List<CoatWidthControlDto>> { Data = new() };

            try
            {
                page = page <= 0 ? 1 : page;
                limit = limit <= 0 ? 10 : limit;

                // ===== Base query + filters ke table utama =====
                var q = _context.CoatWidthControls
                                .AsNoTracking()
                                .Include(x => x.LineMaster) // untuk LineName
                                .AsQueryable();

                if (lineMasterId.HasValue)
                    q = q.Where(x => x.LineMasterId == lineMasterId.Value);

                if (!string.IsNullOrWhiteSpace(subProductName))
                    q = q.Where(x => x.SubProductName != null &&
                                     x.SubProductName.Contains(subProductName));

                if (coatingNo.HasValue)
                    q = q.Where(x => x.CoatingNo == coatingNo.Value);

                if (recordDate.HasValue)
                {
                    var d = DateOnly.FromDateTime(recordDate.Value.Date);
                    q = q.Where(x => x.RecordDate != null && x.RecordDate.Value == d);
                }
                else
                {
                    if (startRecordDate.HasValue && endRecordDate.HasValue)
                    {
                        var s = DateOnly.FromDateTime(startRecordDate.Value.Date);
                        var e = DateOnly.FromDateTime(endRecordDate.Value.Date);
                        q = q.Where(x => x.RecordDate != null &&
                                         x.RecordDate.Value >= s &&
                                         x.RecordDate.Value <= e);
                    }
                    else if (startRecordDate.HasValue)
                    {
                        var s = DateOnly.FromDateTime(startRecordDate.Value.Date);
                        q = q.Where(x => x.RecordDate != null && x.RecordDate.Value >= s);
                    }
                    else if (endRecordDate.HasValue)
                    {
                        var e = DateOnly.FromDateTime(endRecordDate.Value.Date);
                        q = q.Where(x => x.RecordDate != null && x.RecordDate.Value <= e);
                    }
                }

                // Total items setelah filter
                var total = await q.CountAsync();

                // ===== LEFT JOIN ke Users dua kali (ProdMember & ProdStaff) =====
                // Catatan: pakai cast (int?) jika FK nullable
                var projected =
                    from c in q
                    join m in _context.Users.AsNoTracking() on c.ProdMemberId equals (int?)m.Id into mgrp
                    from m in mgrp.DefaultIfEmpty()
                    join s in _context.Users.AsNoTracking() on c.ProdStaffId equals (int?)s.Id into sgrp
                    from s in sgrp.DefaultIfEmpty()
                    select new CoatWidthControlDto
                    {
                        Id = c.Id,
                        LineMasterId = c.LineMasterId,
                        SubProductName = c.SubProductName,
                        CoatingNo = c.CoatingNo ?? 0,
                        RecordDate = c.RecordDate ?? default,
                        KpaRecommend = c.KpaRecommend.HasValue ? (decimal?)c.KpaRecommend.Value : null,
                        Solidity = c.Solidity,
                        Vis100rpm = c.Vis100rpm,
                        Vis1rpm = c.Vis1rpm,
                        Bcd4digit = c.Bcd4digit,
                        CoatingPressureKpa = c.CoatingPressureKpa.HasValue ? (decimal?)c.CoatingPressureKpa.Value : null,
                        CoatWidthAvg = c.CoatWidthAvg.HasValue ? (decimal?)c.CoatWidthAvg.Value : null,
                        ProdMemberId = c.ProdMemberId ?? 0,
                        ProdStaffId = c.ProdStaffId ?? 0,
                        ProdMemberName = m != null ? m.UserName : null,
                        ProdStaffName = s != null ? s.UserName : null,
                        Emisi = c.Emisi,
                        Remark = c.Remark,
                        KpaAccuracy = c.KpaAccuracy.HasValue ? (decimal?)c.KpaAccuracy.Value : null,
                        CreatedAt = c.CreatedAt,

                        // enrich
                        LineName = c.LineMaster != null ? (c.LineMaster.LineName ?? "Unknown") : "Unknown",
                        ProductName = "Unknown" // ganti kalau sudah ada relasi produk
                    };

                var data = await projected
                    .OrderByDescending(x => x.RecordDate)
                    .ThenByDescending(x => x.CreatedAt)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                res.Success = true;
                res.Message = "Get coat width control success";
                res.Data = data;
                res.Pagination = new Pagination
                {
                    Curr_Page = page,
                    Limit = limit,
                    Total_Page = (int)Math.Ceiling((double)total / limit)
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetAll coat width control failed");
                res.Success = false;
                res.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                res.Data = new List<CoatWidthControlDto>();
            }
            return res;
        }



        public async Task<ApiResponse<CoatWidthControlDto?>> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var res = new ApiResponse<CoatWidthControlDto?>();

            try
            {
                var q = _context.CoatWidthControls
                                .AsNoTracking()
                                .Include(x => x.LineMaster)
                                .Where(x => x.Id == id) // ✅ filter by Id
                                .AsQueryable();

                var projected =
                    from c in q
                    join m in _context.Users.AsNoTracking() on c.ProdMemberId equals (int?)m.Id into mgrp
                    from m in mgrp.DefaultIfEmpty()
                    join s in _context.Users.AsNoTracking() on c.ProdStaffId equals (int?)s.Id into sgrp
                    from s in sgrp.DefaultIfEmpty()
                    select new CoatWidthControlDto
                    {
                        Id = c.Id,
                        LineMasterId = c.LineMasterId,
                        SubProductName = c.SubProductName,
                        CoatingNo = c.CoatingNo ?? 0,
                        RecordDate = c.RecordDate ?? default,
                        KpaRecommend = c.KpaRecommend.HasValue ? (decimal?)c.KpaRecommend.Value : null,
                        Solidity = c.Solidity,
                        Vis100rpm = c.Vis100rpm,
                        Vis1rpm = c.Vis1rpm,
                        Bcd4digit = c.Bcd4digit,
                        CoatingPressureKpa = c.CoatingPressureKpa.HasValue ? (decimal?)c.CoatingPressureKpa.Value : null,
                        CoatWidthAvg = c.CoatWidthAvg.HasValue ? (decimal?)c.CoatWidthAvg.Value : null,
                        ProdMemberId = c.ProdMemberId ?? 0,
                        ProdStaffId = c.ProdStaffId ?? 0,
                        ProdMemberName = m != null ? m.UserName : null,
                        ProdStaffName = s != null ? s.UserName : null,
                        Emisi = c.Emisi,
                        Remark = c.Remark,
                        KpaAccuracy = c.KpaAccuracy.HasValue ? (decimal?)c.KpaAccuracy.Value : null,
                        CreatedAt = c.CreatedAt,

                        // enrich
                        LineName = c.LineMaster != null ? (c.LineMaster.LineName ?? "Unknown") : "Unknown",
                        ProductName = "Unknown"
                    };

                var data = await projected.FirstOrDefaultAsync(ct);

                if (data is null)
                {
                    res.Success = false;
                    res.Message = $"Data dengan Id '{id}' tidak ditemukan.";
                    res.Data = null;
                    return res;
                }

                res.Success = true;
                res.Message = "Get coat width control by Id success";
                res.Data = data;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetById coat width control failed");
                res.Success = false;
                res.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                res.Data = null;
            }

            return res;
        }


        // === GET BY DATE RANGE (tanpa paging) + filters (lineMasterId, subProductName, coatingNo, tanggal) ===
        public async Task<ApiResponse<List<CoatWidthControlDto>>> GetByDateRangeAsync(
            DateTime? recordDate = null,
            DateTime? startRecordDate = null,
            DateTime? endRecordDate = null,
            int? lineMasterId = null,
            string? subProductName = null,
            int? coatingNo = null)                // ⬅️ filter baru (opsional)
        {
            var res = new ApiResponse<List<CoatWidthControlDto>> { Data = new() };

            try
            {
                var q = _context.CoatWidthControls
                                .AsNoTracking()
                                .Include(c => c.LineMaster)
                                .AsQueryable();

                // ===== Filters =====
                if (lineMasterId.HasValue)
                    q = q.Where(x => x.LineMasterId == lineMasterId.Value);

                if (!string.IsNullOrWhiteSpace(subProductName))
                    q = q.Where(x => x.SubProductName != null &&
                                     x.SubProductName.Contains(subProductName));

                if (coatingNo.HasValue)
                    q = q.Where(x => x.CoatingNo == coatingNo.Value);

                if (recordDate.HasValue)
                {
                    var d = DateOnly.FromDateTime(recordDate.Value.Date);
                    q = q.Where(x => x.RecordDate != null && x.RecordDate.Value == d);
                }
                else
                {
                    if (startRecordDate.HasValue && endRecordDate.HasValue)
                    {
                        var s = DateOnly.FromDateTime(startRecordDate.Value.Date);
                        var e = DateOnly.FromDateTime(endRecordDate.Value.Date);
                        q = q.Where(x => x.RecordDate != null &&
                                         x.RecordDate.Value >= s &&
                                         x.RecordDate.Value <= e);
                    }
                    else if (startRecordDate.HasValue)
                    {
                        var s = DateOnly.FromDateTime(startRecordDate.Value.Date);
                        q = q.Where(x => x.RecordDate != null && x.RecordDate.Value >= s);
                    }
                    else if (endRecordDate.HasValue)
                    {
                        var e = DateOnly.FromDateTime(endRecordDate.Value.Date);
                        q = q.Where(x => x.RecordDate != null && x.RecordDate.Value <= e);
                    }
                }

                var data = await q
                    .OrderByDescending(x => x.RecordDate)
                    .ThenByDescending(x => x.CreatedAt)
                    .ProjectToType<CoatWidthControlDto>()   // bisa langsung projection (tanpa ToList dulu)
                    .ToListAsync();

                res.Success = true;
                res.Message = "Get coat width control success";
                res.Data = data;
                res.Pagination = null; // no paging
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetByDateRange coat width control failed");
                res.Success = false;
                res.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                res.Data = new List<CoatWidthControlDto>();
            }

            return res;
        }

        public async Task<ApiResponse<CoatWidthControlDto?>> GetByIdAsync(int id)
        {
            var res = new ApiResponse<CoatWidthControlDto?>();

            try
            {
                // Proyeksi langsung ke DTO (JOIN ke LineMaster akan di-generate otomatis)
                var dto = await _context.CoatWidthControls
                    .AsNoTracking()
                    .Where(x => x.Id == id)
                    .ProjectToType<CoatWidthControlDto>()   // Mapster projection
                    .FirstOrDefaultAsync();

                if (dto is null)
                {
                    res.Success = false;                   // gunakan false agar jelas "not found"
                    res.Message = "Data tidak ditemukan";
                    res.Data = null;
                    return res;
                }

                // ProductName dari SubProductName (fallback) — jika config Mapster sudah atur, baris ini opsional
                dto.ProductName ??= dto.SubProductName ?? "Unknown";

                res.Success = true;
                res.Message = "Get by id success";
                res.Data = dto;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetById coat width control failed");
                res.Success = false;
                res.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                res.Data = null;
            }

            return res;
        }
        public async Task<(bool Success, string? Message, int? Id)> CreateAsync(CoatWidthControlCreate request)
        {
            try
            {
                var entity = _mapper.Map<CoatWidthControl>(request);
               
                entity.CreatedAt = DateTime.Now;

                _context.CoatWidthControls.Add(entity);
                await _context.SaveChangesAsync();

                return (true, "Created successfully", entity.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Create coat width control failed");
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? Message, int? Id)> UpdateAsync(int id, CoatWidthControlCreate request)
        {
            try
            {
                var entity = await _context.CoatWidthControls.FirstOrDefaultAsync(x => x.Id == id);
                if (entity is null)
                    return (false, "Data tidak ditemukan.", null);

                _mapper.Map(request, entity); // request -> entity
                await _context.SaveChangesAsync();

                return (true, "Updated successfully", entity.Id);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Update coat width control failed");
                return (false, ex.Message, null);
            }
        }

        public async Task<(bool Success, string? Message)> DeleteAsync(int id)
        {
            try
            {
                var entity = await _context.CoatWidthControls.FirstOrDefaultAsync(x => x.Id == id);
                if (entity is null) return (false, "Data tidak ditemukan.");

                _context.CoatWidthControls.Remove(entity);
                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Delete coat width control failed");
                return (false, ex.Message);
            }
        }

        public async Task<(bool Success, string? Message, double? Kpa)> CalculateKpaRecommendationAsync(CalculateKpaRecommendationRequest request)
        {
            try
            {
                // ✅ Validasi manual (atau gunakan FluentValidation)
                if (request == null)
                    return (false, "Invalid request", null);

                var lineMasterId = await _context.LineMasters
                    .Where(x => x.LineNo == request.LineNo)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();

                // ✅ Ambil nilai min dan max dari tabel coat_width_control
                var minMax = await _context.CoatWidthControls
                    .Where(x => x.LineMasterId == lineMasterId &&
                            x.SubProductName == request.SubProductName)
                    .GroupBy(x => 1)
                    .Select(g => new
                    {
                        MinSolidity = g.Min(x => (decimal?)x.Solidity),
                        MaxSolidity = g.Max(x => (decimal?)x.Solidity),
                        MinVis100 = g.Min(x => (int?)x.Vis100rpm),
                        MaxVis100 = g.Max(x => (int?)x.Vis100rpm),
                        MinVis1 = g.Min(x => (int?)x.Vis1rpm),
                        MaxVis1 = g.Max(x => (int?)x.Vis1rpm),
                        MinEmisi = g.Min(x => (double?)x.Bcd4digit),
                        MaxEmisi = g.Max(x => (double?)x.Bcd4digit),
                        MinCoatingPressure = g.Min(x => (double?)x.CoatingPressureKpa),
                        MaxCoatingPressure = g.Max(x => (double?)x.CoatingPressureKpa),
                    })
                    .FirstOrDefaultAsync();

                // Jika tabel kosong
                double min(double? val, double fallback) => val ?? fallback;
                double max(double? val, double fallback) => val ?? fallback;

                decimal _minSolidity = request.Solidity.HasValue
                            ? decimal.Min(minMax?.MinSolidity ?? request.Solidity.Value,
                                          request.Solidity.Value)
                            : minMax?.MinSolidity ?? 0;

                decimal _maxSolidity = request.Solidity.HasValue
                    ? decimal.Max(minMax?.MaxSolidity ?? request.Solidity.Value,
                                  request.Solidity.Value)
                    : minMax?.MaxSolidity ?? 0;


                int _minVis100 = request.Vis100rpm.HasValue
                    ? Math.Min(minMax?.MinVis100 ?? request.Vis100rpm.Value,
                               request.Vis100rpm.Value)
                    : minMax?.MinVis100 ?? 0;

                int _maxVis100 = request.Vis100rpm.HasValue
                    ? Math.Max(minMax?.MaxVis100 ?? request.Vis100rpm.Value,
                               request.Vis100rpm.Value)
                    : minMax?.MaxVis100 ?? 0;


                int _minVis1 = request.Vis1rpm.HasValue
                     ? Math.Min(minMax?.MinVis1 ?? request.Vis1rpm.Value,
                                request.Vis1rpm.Value)
                     : minMax?.MinVis1 ?? 0;

                int _maxVis1 = request.Vis1rpm.HasValue
                    ? Math.Max(minMax?.MaxVis1 ?? request.Vis1rpm.Value,
                               request.Vis1rpm.Value)
                    : minMax?.MaxVis1 ?? 0;


                double _minEmisi = request.Emisi.HasValue
                    ? Math.Min(minMax?.MinEmisi ?? request.Emisi.Value,
                               request.Emisi.Value)
                    : minMax?.MinEmisi ?? 0;

                double _maxEmisi = request.Emisi.HasValue
                    ? Math.Max(minMax?.MaxEmisi ?? request.Emisi.Value,
                               request.Emisi.Value)
                    : minMax?.MaxEmisi ?? 0;


                double _minPressure = request.CoatingPressureKpa.HasValue
                    ? Math.Min(minMax?.MinCoatingPressure ?? request.CoatingPressureKpa.Value,
                               request.CoatingPressureKpa.Value)
                    : minMax?.MinCoatingPressure ?? 0;

                double _maxPressure = request.CoatingPressureKpa.HasValue
                    ? Math.Max(minMax?.MaxCoatingPressure ?? request.CoatingPressureKpa.Value,
                               request.CoatingPressureKpa.Value)
                    : minMax?.MaxCoatingPressure ?? 0;


                // ✅ Perhitungan diff & counter
                double GetCounter(double val, double minVal, double maxVal)
                {
                    double diff = (maxVal - minVal) / 29;
                    if (Math.Round(diff, 2) == 0.0) return 29;
                    return Math.Floor((val - minVal) / diff);
                }

                double counterSolidity = GetCounter((double)request.Solidity, (double)_minSolidity, (double)_maxSolidity);
                double counterVis1 = GetCounter(request.Vis1rpm ?? 0, _minVis1, _maxVis1);
                double counterVis100 = GetCounter(request.Vis100rpm ?? 0, _minVis100, _maxVis100);
                double counterEmisi = GetCounter(request.Emisi ?? 0, _minEmisi, _maxEmisi);

                double diffPressure = (_maxPressure - _minPressure) / 29;

                double fieldSolidity = _minPressure + counterSolidity * diffPressure;
                double fieldVis1 = _minPressure + counterVis1 * diffPressure;
                double fieldVis100 = _minPressure + counterVis100 * diffPressure;
                double fieldEmisi = _minPressure + counterEmisi * diffPressure;

                // ✅ Ambil data card_no_master
                var cardNoMaster = await _context.CardNoMasters
                    .FirstOrDefaultAsync(x =>
                        x.Id == lineMasterId &&
                        x.ProductName == request.SubProductName);

                double kpaRecommendation = 0;

                double coatWidthMin = 0;
                var coatWidthAvg = request.CoatWidthAvg ?? 0;

                if ((cardNoMaster != null &&
                    double.TryParse(cardNoMaster.CoatWidthMin, out coatWidthMin) &&
                    coatWidthAvg < coatWidthMin + 2) || request.Emisi <= 10)
                {
                    kpaRecommendation = (fieldSolidity + fieldVis1 + fieldVis100) / 3;
                }
                else if (request.Emisi > 10 && request.Emisi <= 20)
                {
                    kpaRecommendation = ((fieldSolidity + fieldVis1 + fieldVis100) / 3) - 0.5;
                }
                else if (request.Emisi > 20)
                {
                    kpaRecommendation = ((fieldSolidity + fieldVis1 + fieldVis100) / 3) - 1;
                }

                kpaRecommendation = Math.Round(kpaRecommendation, 2);


                return (true, "Calculated KPA successfully", kpaRecommendation);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CalculateKpa failed");
                return (false, ex.Message, null);
            }
        }

        public async Task<ApiResponse<CoatWidthControlDto?>> GetLatestBySubProductNameAsync(
    string subProductName)
        {
            var res = new ApiResponse<CoatWidthControlDto?>();

            try
            {
                var data = await _context.CoatWidthControls
                    .AsNoTracking()
                    .Where(c => c.SubProductName == subProductName)
                    .OrderByDescending(c => c.Id)
                    .Select(c => new CoatWidthControlDto
                    {
                        Id = c.Id,
                        LineMasterId = c.LineMasterId,
                        SubProductName = c.SubProductName,
                        CoatingNo = c.CoatingNo ?? 0,
                        RecordDate = c.RecordDate ?? default,
                        KpaRecommend = c.KpaRecommend.HasValue ? (decimal?)c.KpaRecommend.Value : null,
                        Solidity = c.Solidity,
                        Vis100rpm = c.Vis100rpm,
                        Vis1rpm = c.Vis1rpm,
                        Bcd4digit = c.Bcd4digit,
                        CoatingPressureKpa = c.CoatingPressureKpa.HasValue ? (decimal?)c.CoatingPressureKpa.Value : null,
                        CoatWidthAvg = c.CoatWidthAvg.HasValue ? (decimal?)c.CoatWidthAvg.Value : null,
                        ProdMemberId = c.ProdMemberId ?? 0,
                        ProdStaffId = c.ProdStaffId ?? 0,
                        Emisi = c.Emisi,
                        Remark = c.Remark,
                        KpaAccuracy = c.KpaAccuracy.HasValue ? (decimal?)c.KpaAccuracy.Value : null,
                        CreatedAt = c.CreatedAt,

                        // enrich
                        LineName = c.LineMaster != null ? (c.LineMaster.LineName ?? "Unknown") : "Unknown",
                        ProductName = "Unknown"
                    })
                    .FirstOrDefaultAsync();

                res.Success = true;
                res.Message = "Get latest coat width control success";
                res.Data = data;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetLatestBySubProductNameAsync failed");
                res.Success = false;
                res.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                res.Data = null;
            }

            return res;
        }

    }
}
