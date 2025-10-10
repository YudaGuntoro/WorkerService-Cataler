using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NLog;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.ProductionCount;
using Web.API.Mappings.Request;
using Web.API.Mappings.Response;
using Web.API.Persistence.Context;
using Web.API.Persistence.Services;

namespace Web.API.Persistence.Repository
{
    public class ProductionCountService : IProductionCountService
    {
        private readonly AppDbContext _context;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ProductionCountService(AppDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// List Production Count (group by LineMaster), filter by lineNo & shift 08:00-08:00 rule (pakai DateTime.Now).
        /// </summary>
        public async Task<ApiResponse<List<GetHistoryListProductionCountDto>>> GetAllAsync(
            int? lineNo = null)
        {
            var response = new ApiResponse<List<GetHistoryListProductionCountDto>> { Data = new() };

            try
            {
                var q = _context.Set<ProductionCountHistory>().AsNoTracking().AsQueryable();

                // === Filter tanggal dengan aturan shift 08:00 → 08:00, otomatis pakai DateTime.Now ===
                DateTime start, end;
                var now = DateTime.Now;
                var today = now.Date;

                if (now.Hour < 8)
                {
                    // Kalau masih sebelum jam 8, maka pakai kemarin 08:00 s/d hari ini 08:00
                    start = today.AddDays(-1).AddHours(8);
                    end = today.AddHours(8);
                }
                else
                {
                    // Kalau sudah lewat jam 8, maka pakai hari ini 08:00 s/d besok 08:00
                    start = today.AddHours(8);
                    end = today.AddDays(1).AddHours(8);
                }

                q = q.Where(x => x.Timestamp >= start && x.Timestamp < end);

                // Filter lineNo -> map ke LineMasterId
                if (lineNo.HasValue)
                {
                    var lineIds = await _context.LineMasters
                        .Where(l => l.LineNo == lineNo.Value)
                        .Select(l => l.Id)
                        .ToListAsync();

                    q = q.Where(x => lineIds.Contains(x.LineMasterId));
                }

                var rows = await q.OrderByDescending(x => x.Timestamp).ToListAsync();

                // Ambil metadata LineMaster
                var lineIdsInResult = rows.Select(x => x.LineMasterId).Distinct().ToList();
                var lineMap = await _context.LineMasters
                    .AsNoTracking()
                    .Where(l => lineIdsInResult.Contains(l.Id))
                    .ToDictionaryAsync(l => l.Id, l => new { l.LineNo, l.LineName });

                // Grouping by LineMasterId
                var grouped = rows
                    .GroupBy(x => x.LineMasterId)
                    .Select(g =>
                    {
                        var meta = lineMap.TryGetValue(g.Key, out var v)
                            ? v
                            : new { LineNo = 0, LineName = "Unknown" };

                        return new GetHistoryListProductionCountDto
                        {
                            lineNo = meta.LineNo,
                            lineName = meta.LineName ?? string.Empty,
                            Details = g.Select(row => new ProductionCountItemDto
                            {
                                Id = row.Id,
                                LineMasterId = row.LineMasterId,
                                CardNo = row.CardNo,
                                Target = row.Target,
                                Actual = row.Actual,
                                Timestamp = row.Timestamp
                            }).ToList()
                        };
                    })
                    .OrderByDescending(x => x.Details.Max(d => d.Timestamp))
                    .ToList();

                response.Success = true;
                response.Message = "Get production count history success";
                response.Data = grouped;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetAll ProductionCountHistory failed");
                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                response.Data = new();
            }

            return response;
        }

        // ====== MASTER: GET list ======
        public async Task<ApiResponse<List<ProductionCountMasterDto>>> GetMasterAsync(
            int page = 1,
            int limit = 10,
            int? lineNo = null,
            int? lineMasterId = null)
        {
            var resp = new ApiResponse<List<ProductionCountMasterDto>> { Data = new() };
            try
            {
                var q = _context.Set<ProductionCountMaster>().AsNoTracking().AsQueryable();

                // filter by lineMasterId langsung
                if (lineMasterId.HasValue)
                    q = q.Where(m => m.LineMasterId == lineMasterId.Value);

                // filter by lineNo -> map ke LineMaster.Id
                if (lineNo.HasValue)
                {
                    var idList = await _context.LineMasters
                        .Where(l => l.LineNo == lineNo.Value)
                        .Select(l => l.Id)
                        .ToListAsync();

                    q = q.Where(m => m.LineMasterId != null && idList.Contains(m.LineMasterId.Value));
                }

                var total = await q.CountAsync();

                var data = await q
                    .OrderBy(m => m.DispOrder)        // urut default
                    .ThenBy(m => m.DataDate)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                // ambil metadata line
                var lineIds = data.Where(x => x.LineMasterId.HasValue).Select(x => x.LineMasterId!.Value).Distinct().ToList();
                var lineMap = await _context.LineMasters
                    .AsNoTracking()
                    .Where(l => lineIds.Contains(l.Id))
                    .ToDictionaryAsync(l => l.Id, l => new { l.LineNo, l.LineName });

                // map ke DTO
                var list = data.Select(m =>
                {
                    int? lnNo = null;
                    string? lnName = null;
                    if (m.LineMasterId.HasValue && lineMap.TryGetValue(m.LineMasterId.Value, out var meta))
                    {
                        lnNo = meta.LineNo;
                        lnName = meta.LineName;
                    }

                    return new ProductionCountMasterDto
                    {
                        Id = m.Id,
                        LineMasterId = m.LineMasterId,
                        LineNo = lnNo,
                        LineName = lnName,
                        DispOrder = m.DispOrder,
                        DataDate = m.DataDate,               // TIME -> TimeSpan
                        OperationHour = m.OperationHour,
                        PathControl = m.PathControl,
                        Target = m.Target
                    };
                }).ToList();

                resp.Success = true;
                resp.Message = "Get production count master success";
                resp.Data = list;
                resp.Pagination = new Pagination
                {
                    Curr_Page = page,
                    Limit = limit,
                    Total_Page = (int)Math.Ceiling((double)total / limit)
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetMaster ProductionCount failed");
                resp.Success = false;
                resp.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                resp.Data = new();
            }

            return resp;
        }

        // ====== MASTER: PUT by Id ======
        // ====== TAMBAHKAN INI: implementasi persis seperti interface ======
        public async Task<(bool Success, string? Message)> UpdateMasterAsync(int id, ProductionCountMasterUpdateRequest request)
        {
            try
            {
                var entity = await _context.Set<ProductionCountMaster>()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (entity is null)
                    return (false, "Data tidak ditemukan.");

                // (Opsional) validasi LineMasterId jika diberikan
                if (request.LineMasterId.HasValue)
                {
                    var exists = await _context.LineMasters.AnyAsync(l => l.Id == request.LineMasterId.Value);
                    if (!exists)
                        return (false, $"LineMasterId {request.LineMasterId} tidak ditemukan.");
                }

                entity.LineMasterId = request.LineMasterId;
                entity.DispOrder = request.DispOrder ?? entity.DispOrder;
                entity.DataDate = request.DataDate;        // TimeSpan
                entity.OperationHour = request.OperationHour;
                entity.PathControl = request.PathControl;
                entity.Target = request.Target;

                await _context.SaveChangesAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Update ProductionCountMaster failed");
                return (false, ex.Message);
            }
        }
    }
}

