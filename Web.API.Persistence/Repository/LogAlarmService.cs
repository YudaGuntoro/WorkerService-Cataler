using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Web.API.Mappings.DTOs.HistoryList;
using Web.API.Mappings.Response;
using Web.API.Persistence.Context;
using Web.API.Persistence.Services;
using MapsterMapper;
using MiniExcelLibs;

namespace Web.API.Persistence.Repository
{
    public class LogAlarmService : ILogAlarmService
    {
        private readonly AppDbContext _context;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IMapper _mapper;
        public LogAlarmService(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ApiResponse<List<GetAlarmLogDto>>> GetAllAsync(
                    int page = 1,
                    int limit = 10,
                    int? lineNo = null,
                    DateTime? startDate = null,
                    DateTime? endDate = null)
        {
            var response = new ApiResponse<List<GetAlarmLogDto>>();
            try
            {
                var query = _context.AlarmLogHistories
                    .AsNoTracking()
                    .AsQueryable();

                // Filtering by lineNo
                if (lineNo.HasValue)
                {
                    query = query.Where(l => l.LineNo == lineNo.Value);
                }
                // Filtering by date range (range-berbasis-hari)
                if (startDate.HasValue && endDate.HasValue)
                {
                    var start = startDate.Value.Date;
                    var endExcl = endDate.Value.Date.AddDays(1); // eksklusif
                    query = query.Where(l => l.Timestamp >= start && l.Timestamp < endExcl);
                }
                else if (startDate.HasValue)
                {
                    // HANYA satu tanggal -> ambil semua data pada hari tsb
                    var day = startDate.Value.Date;
                    var next = day.AddDays(1);
                    query = query.Where(l => l.Timestamp >= day && l.Timestamp < next);
                }
                else if (endDate.HasValue)
                {
                    // HANYA satu tanggal -> ambil semua data pada hari tsb
                    var day = endDate.Value.Date;
                    var next = day.AddDays(1);
                    query = query.Where(l => l.Timestamp >= day && l.Timestamp < next);
                }


                var totalLogs = await query.CountAsync();

                var pagedLogs = await query
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                var lineNos = pagedLogs
                    .Select(l => l.LineNo)
                    .Distinct()
                    .ToList();


                var machines = await _context.LineMasters
                    .AsNoTracking()
                    .Where(m => lineNos.Contains(m.LineNo))
                    .ToListAsync();

                var result = _mapper.Map<List<GetAlarmLogDto>>(pagedLogs);

                foreach (var item in result)
                {
                    item.LineName = machines.FirstOrDefault(m => m.LineNo == item.LineNo)?.LineName ?? "Unknown";
                }

                response.Success = true;
                response.Message = "Get alarm log success";
                response.Data = result;
                response.Pagination = new Pagination
                {
                    Curr_Page = page,
                    Limit = limit,
                    Total_Page = (int)Math.Ceiling((double)totalLogs / limit)
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                response.Data = new List<GetAlarmLogDto>();
            }
            return response;
        }

        public async Task<(bool Success, string? Message, byte[]? Bytes, string? FileName)> ExportFailureDetailsAsync(
        string templatePath,
        int page = 1,
        int? limit = null,
        int? lineNo = null,
        DateTime? date = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? keyword = null)
        {
            try
            {
                if (!File.Exists(templatePath))
                    return (false, $"Template tidak ditemukan: {templatePath}", null, null);

                var effectiveLimit = limit ?? 100;
                if (effectiveLimit <= 0) effectiveLimit = 100;

                // Base query
                var q = _context.AlarmLogHistories.AsNoTracking().AsQueryable();

                // Filter line
                if (lineNo.HasValue)
                    q = q.Where(l => l.LineNo == lineNo.Value);

                // Filter tanggal (aman & index-friendly)
                if (date.HasValue)
                {
                    var s = date.Value.Date;
                    var e = s.AddDays(1);
                    q = q.Where(l => l.Timestamp >= s && l.Timestamp < e);
                }
                else
                {
                    if (startDate.HasValue)
                    {
                        var s = startDate.Value.Date;
                        q = q.Where(l => l.Timestamp >= s);
                    }
                    if (endDate.HasValue)
                    {
                        var e = endDate.Value.Date.AddDays(1);
                        q = q.Where(l => l.Timestamp < e);
                    }
                }

                // Filter keyword di Message
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var kw = keyword.Trim();
                    q = q.Where(l => l.Message != null && EF.Functions.Like(l.Message, $"%{kw}%"));
                }

                // Paging
                var logs = await q
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * effectiveLimit)
                    .Take(effectiveLimit)
                    .ToListAsync();

                if (logs.Count == 0)
                    return (false, "Data tidak ditemukan untuk filter yang diberikan.", null, null);

                // Ambil CoatLine (LineName) dari LineMasters berdasarkan LineNo
                var lineNos = logs.Select(l => l.LineNo).Distinct().ToList();
                var namesByLineNo = await _context.LineMasters
                    .AsNoTracking()
                    .Where(m => lineNos.Contains(m.LineNo))
                    .ToDictionaryAsync(m => m.LineNo, m => m.LineName);

                // Kalau difilter lineNo → header pakai nama line tsb, jika tidak → "-"
                string headerLineName = "-";
                if (lineNo.HasValue)
                    headerLineName = namesByLineNo.TryGetValue(lineNo.Value, out var nm) ? nm : $"Line {lineNo.Value}";

                // ====== IMPORTANT: mapping ke alias "Result" agar match template ======
                var rows = logs.Select((l, idx) => new
                {
                    No = (page - 1) * effectiveLimit + idx + 1,
                    CoatLine = namesByLineNo.TryGetValue(l.LineNo, out var nm) ? nm : $"Line {l.LineNo}",
                    DateTime = l.Timestamp.ToString("dd-MMM-yy HH:mm"),
                    Message = l.Message ?? string.Empty
                }).ToList();

                var model = new
                {
                    ReportDate = DateTime.Now.ToString("dd-MMM-yy HH:mm"),
                    Result = rows
                };

                using var ms = new MemoryStream();
                await MiniExcel.SaveAsByTemplateAsync(ms, templatePath, model);
                var bytes = ms.ToArray();

                var fileName = $"FailureDetails_{DateTime.Now:yyyyMMdd_HHmm}_P{page}_L{effectiveLimit}.xlsx";
                return (true, null, bytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ExportFailureDetailsAsync failed");
                return (false, ex.Message, null, null);
            }
        }
    }
}