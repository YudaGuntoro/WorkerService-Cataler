using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Web.API.Mappings.DTOs.HistoryList;
using Web.API.Persistence.Context;
using Web.API.Mappings.Response;
using Microsoft.EntityFrameworkCore;
using Web.API.Persistence.Services;
using MiniExcelLibs;
using Web.API.Domain.Entities;
using Web.API.Mappings.Export;
using MapsterMapper;
using Microsoft.Extensions.Hosting;

namespace Web.API.Persistence.Repository
{
    public class LogCycleService : ILogCycleService
    {
        private readonly AppDbContext _context;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly IMapper _mapper;
        private readonly IHostEnvironment _env;
        public LogCycleService(AppDbContext context, IMapper mapper, IHostEnvironment env)
        {
            _context = context;
            _mapper = mapper;
            _env = env;   // simpan ke field
        }

        public async Task<ApiResponse<List<GetHistoryListCycleTimeDto>>> GetAllAsync(
          int page = 1,
          int limit = 10,
          int? lineNo = null,
          DateTime? date = null,
          DateTime? startDate = null,
          DateTime? endDate = null)
        {
            var response = new ApiResponse<List<GetHistoryListCycleTimeDto>>();

            try
            {
                // Mulai query log
                var logQuery = _context.LogCycletimes.AsNoTracking();

                // Filter tanggal: date > range
                if (date.HasValue)
                {
                    var selectedDate = date.Value.Date;
                    logQuery = logQuery.Where(l => l.Timestamp.Date == selectedDate);
                }
                else if (startDate.HasValue && endDate.HasValue)
                {
                    var start = startDate.Value.Date;
                    var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    logQuery = logQuery.Where(l => l.Timestamp >= start && l.Timestamp <= end);
                }

                // Filter lineNo jika ada
                if (lineNo.HasValue)
                {
                    var machineIds = await _context.LineMasters
                        .Where(m => m.LineNo == lineNo.Value)
                        .Select(m => m.Id)
                        .ToListAsync();

                    logQuery = logQuery.Where(l => machineIds.Contains(l.MachineId));
                }

                // Hitung total log untuk pagination
                var totalLogs = await logQuery.CountAsync();

                // Ambil log sesuai paging
                var pagedLogs = await logQuery
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToListAsync();

                // Ambil semua MachineId unik dari log
                var machineIdsInLogs = pagedLogs.Select(l => l.MachineId).Distinct().ToList();

                // Ambil data mesin terkait
                var machines = await _context.LineMasters
                    .AsNoTracking()
                    .Where(m => machineIdsInLogs.Contains(m.Id))
                    .ToListAsync();

                // Gabungkan log dengan mesin
                var result = machines
                    .Select(machine => new GetHistoryListCycleTimeDto
                    {
                        lineNo = machine.LineNo,
                        lineName = machine.LineName ?? string.Empty,
                        Details = pagedLogs.Where(l => l.MachineId == machine.Id).ToList()
                    })
                    .ToList();

                // Response
                response.Success = true;
                response.Message = "Get history list success";
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
                response.Data = new List<GetHistoryListCycleTimeDto>();
            }

            return response;
        }


        // === NEW ===
        public async Task<(bool Success, string? Message, byte[]? Bytes, string? FileName)> ExportCycleTimeAsync(
            int page = 1,
            int? limit = null,    // default 100 bila null
            int? lineNo = null,
            DateTime? date = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var effectiveLimit = limit ?? 100;
                if (effectiveLimit <= 0) effectiveLimit = 100;

                // Template di AppStartup/Templates/LogCycleTimeRecord.xlsx
                var templatePath = Path.Combine(
                    _env.ContentRootPath, "Template", "LogCycleTimeRecord.xlsx");

                if (!File.Exists(templatePath))
                    return (false, $"Template tidak ditemukan: {templatePath}", null, null);

                // Lookup LineMaster (Id, LineNo, LineName)
                var lineLookup = await _context.LineMasters
                    .AsNoTracking()
                    .Select(l => new { l.Id, l.LineNo, l.LineName })
                    .ToListAsync();

                var nameById = lineLookup.ToDictionary(x => x.Id, x => x.LineName);
                var idsByLineNo = lineLookup
                    .GroupBy(x => x.LineNo)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.Id).ToList());

                // Base query LogCycletimes
                var q = _context.LogCycletimes.AsNoTracking().AsQueryable();

                // Filter tanggal (pakai rentang aman & index-friendly)
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

                // Filter lineNo -> MachineId (LogCycletimes.MachineId == LineMasters.Id)
                if (lineNo.HasValue && idsByLineNo.TryGetValue(lineNo.Value, out var machineIds) && machineIds.Count > 0)
                {
                    q = q.Where(l => machineIds.Contains(l.MachineId));
                }

                // Paging
                var logs = await q
                    .OrderByDescending(l => l.Timestamp)
                    .Skip((page - 1) * effectiveLimit)
                    .Take(effectiveLimit)
                    .ToListAsync();

                if (logs.Count == 0)
                    return (false, "Data tidak ditemukan untuk filter yang diberikan.", null, null);

                // Header LineName (kalau lineNo tidak diisi, pakai "-")
                string headerLineName = "-";
                if (lineNo.HasValue && idsByLineNo.TryGetValue(lineNo.Value, out var ids) && ids.Count > 0)
                {
                    var anyId = ids[0];
                    headerLineName = nameById.TryGetValue(anyId, out var nm) ? nm : $"Line {lineNo.Value}";
                }

                // Map ke Data.* sesuai placeholder di template:
                //   ReportDate, LineName, Data.No, Data.DateTime, Data.Target, Data.Message,
                //   Data.ACoat1..3, Data.BCoat1..3, Data.Judgement
                var dataRows = logs.Select((log, idx) => new
                {
                    No = (page - 1) * effectiveLimit + idx + 1,
                    DateTime = log.Timestamp.ToString("dd-MMM-yy HH:mm"),
                    Target = log.Target,
                    Result = log.Result,
                    ACoat1 = log.ACoat1,
                    ACoat2 = log.ACoat2,
                    ACoat3 = log.ACoat3,
                    BCoat1 = log.BCoat1,
                    BCoat2 = log.BCoat2,
                    BCoat3 = log.BCoat3,
                    Judgement = log.Judgement
                }).ToList();

                var model = new
                {
                    ReportDate = DateTime.Now.ToString("dd-MMM-yy HH:mm"),
                    LineName = headerLineName,
                    Data = dataRows
                };

                using var ms = new MemoryStream();
                await MiniExcel.SaveAsByTemplateAsync(ms, templatePath, model);
                var bytes = ms.ToArray();

                var fileName = $"LogCycleTime_{DateTime.Now:yyyyMMdd_HHmm}_P{page}_L{effectiveLimit}.xlsx";
                return (true, null, bytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ExportCycleTimeAsync failed");
                return (false, ex.Message, null, null);
            }
        }

        // === DETAIL BY ID ===
        public async Task<ApiResponse<ExportAlarmLog>> GetDetailAsync(int id)
        {
            var response = new ApiResponse<ExportAlarmLog>();
            try
            {
                // Ambil log
                var log = await _context.AlarmLogHistories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (log is null)
                {
                    response.Success = false;
                    response.Message = $"Alarm log dengan Id={id} tidak ditemukan.";
                    return response;
                }

                // Ambil LineName dari LineMasters (berdasarkan LineNo)
                var lineName = await _context.LineMasters
                    .AsNoTracking()
                    .Where(m => m.LineNo == log.LineNo)
                    .Select(m => m.LineName)
                    .FirstOrDefaultAsync() ?? "Unknown";

                var dto = new ExportAlarmLog
                {
                    Id = log.Id,
                    LineNo = log.LineNo,
                    LineName = lineName,
                    Message = log.Message ?? string.Empty,
                    Timestamp = log.Timestamp
                };

                response.Success = true;
                response.Message = "Get alarm log detail success";
                response.Data = dto;
                return response;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetDetailAsync failed");
                response.Success = false;
                response.Message = $"Exception: {ex.Message} || {ex.InnerException?.Message}";
                return response;
            }
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

                var q = _context.AlarmLogHistories.AsNoTracking().AsQueryable();

                // Filter line
                if (lineNo.HasValue)
                    q = q.Where(l => l.LineNo == lineNo.Value);

                // Filter tanggal
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

                // Filter keyword
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
                    return (false, "Data tidak ditemukan.", null, null);

                // Ambil LineName
                var lineNos = logs.Select(l => l.LineNo).Distinct().ToList();
                var namesByLineNo = await _context.LineMasters
                    .AsNoTracking()
                    .Where(m => lineNos.Contains(m.LineNo))
                    .ToDictionaryAsync(m => m.LineNo, m => m.LineName);

                string headerLineName = "-";
                if (lineNo.HasValue)
                    headerLineName = namesByLineNo.TryGetValue(lineNo.Value, out var nm) ? nm : $"Line {lineNo.Value}";

                // Map ke Data.* sesuai template
                var dataRows = logs.Select((l, idx) => new
                {
                    No = (page - 1) * effectiveLimit + idx + 1,
                    LineNo = l.LineNo,
                    LineName = namesByLineNo.TryGetValue(l.LineNo, out var nm) ? nm : "Unknown",
                    Timestamp = l.Timestamp.ToString("dd-MMM-yy HH:mm"),
                    Message = l.Message ?? string.Empty
                }).ToList();

                var model = new
                {
                    ReportDate = DateTime.Now.ToString("dd-MMM-yy HH:mm"),
                    LineName = headerLineName,
                    Data = dataRows
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
