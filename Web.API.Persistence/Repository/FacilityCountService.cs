using Microsoft.EntityFrameworkCore;
using MiniExcelLibs;
using MySqlConnector;
using Newtonsoft.Json;
using StackExchange.Redis;
using Web.API.Domain.Entities;
using Web.API.Mappings.DTOs.FacilityCount;
using Web.API.Mappings.DTOs.Gateway;
using Web.API.Mappings.Export;
using Web.API.Mappings.Response;
using Web.API.Persistence.Context;
using Web.API.Persistence.Helper;

namespace Web.API.Persistence.Services
{
    public class FacilityCountService : IFacilityCountService
    {
        private readonly AppDbContext _context;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDictionary<string, IDeviceMapProvider> _providers;
        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();
        
        // Tipe kecil utk device yang diizinkan (eliminasi List<dynamic> error)
        private sealed record AllowedDevice(int Id, string Device, long LimitValue);

        public FacilityCountService(
            AppDbContext context,
            IConnectionMultiplexer redis,
            IEnumerable<IDeviceMapProvider> providers)
        {
            _context = context;
            _redis = redis;
            _providers = providers.ToDictionary(p => p.MachineKey, p => p, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<ApiResponse<List<FacilityCountRealtimeDto>>> GetLiveAsync
        (
            string machineKey,
            string topic,
            int lineMasterId,
            int page = 1,
            int limit = 10,
            DateTime? startDate = null,
            DateTime? endDate = null,
            CancellationToken ct = default
        )
        {
            var resp = new ApiResponse<List<FacilityCountRealtimeDto>> { Data = new() };

            try
            {
                // 1) Validasi provider (machineKey)
                if (!_providers.TryGetValue(machineKey, out var provider))
                    return FailResponse(page, limit, "Unknown machineKey.", new());

                // 2) Validasi line
                var line = await _context.LineMasters
                    .AsNoTracking()
                    .Where(l => l.Id == lineMasterId)
                    .Select(l => new { l.Id, l.LineName })
                    .FirstOrDefaultAsync(ct);

                if (line is null)
                    return FailResponse(page, limit, $"LineMasterId {lineMasterId} not found.", new());

                // 3) Ambil snapshot device dari DB (semua kolom yang kita butuhkan)
                var allowed = await _context.FacilityCounts
                    .AsNoTracking()
                    .Where(d => d.LineMasterId == lineMasterId)
                    .Select(d => new
                    {
                        d.Id,
                        d.Device,
                        d.Category,
                        d.LimitValue,
                        d.Result,          // BIGINT NULL
                        d.CollectDate,
                        d.Status
                    })
                    .ToListAsync(ct);

                if (allowed.Count == 0)
                    return OkResponse(page, limit, "No mapped devices for this line.", new());

                // 4) Coba ambil payload dari Redis (boleh kosong)
                var db = _redis.GetDatabase();
                var key = $"cataler:mqtt:data:{topic}";
                var payloadStr = await db.StringGetAsync(key);

                ToolPayload? payload = null;
                if (!payloadStr.IsNullOrEmpty)
                {
                    try
                    {
                        payload = JsonConvert.DeserializeObject<ToolPayload>(payloadStr!);
                        if (payload?.TOOL == null || payload.TOOL.Count == 0)
                            payload = null; // anggap invalid → perlakuan sama seperti tidak ada
                    }
                    catch
                    {
                        payload = null;
                    }
                }

                var liveStamp = DateTime.Now;

                // 5) Rakit data: selalu iterasi semua device DB; override result dari Redis jika ada
                foreach (var ad in allowed)
                {
                    // result dari Redis jika tersedia
                    long resultVal;
                    DateTime collectAt;

                    if (payload != null && payload.TOOL.TryGetValue(ad.Device, out var rawFromRedis))
                    {
                        resultVal = Math.Max(0L, rawFromRedis ?? 0L);
                        collectAt = liveStamp; // cap waktu dari payload
                    }
                    else
                    {
                        resultVal = ad.Result.HasValue ? Math.Max(0L, ad.Result.Value) : 0L;
                        collectAt = ad.CollectDate;
                    }

                    var limitVal = ad.LimitValue > 0 ? ad.LimitValue : Math.Max(1L, resultVal * 2);

                    // Prioritaskan mapping dari provider jika ada; jika tidak, pakai kategori dari DB; terakhir fallback fungsi
                    string category =
                        (provider.DeviceCategoryMap.TryGetValue(ad.Device, out var mapped) ? mapped : null)
                        ?? (string.IsNullOrWhiteSpace(ad.Category) ? null : ad.Category)
                        ?? MapCategory(ad.Device);

                    var status = ComputeStatus(resultVal, limitVal);

                    resp.Data.Add(new FacilityCountRealtimeDto
                    {
                        Id = ad.Id,
                        LineMasterId = line.Id,
                        LineName = line.LineName,
                        Category = category,
                        Device = ad.Device,
                        Result = resultVal,
                        LimitValue = limitVal,
                        CollectDate = collectAt,
                        Status = status
                    });
                }

                // 6) (Opsional) filter waktu kalau parameter dipakai
                if (startDate.HasValue)
                    resp.Data = resp.Data.Where(x => x.CollectDate >= startDate.Value).ToList();
                if (endDate.HasValue)
                    resp.Data = resp.Data.Where(x => x.CollectDate <= endDate.Value).ToList();


                // 7) Sort & paginate (prioritas: Need Maintenance > Warning > Good)
                var total = resp.Data.Count;
                var ordered = resp.Data
                    .OrderBy(x => StatusPriority(x.Status))
                    .ThenByDescending(x => x.CollectDate)
                    .ThenBy(x => x.Device, StringComparer.OrdinalIgnoreCase);

                resp.Data = ordered
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .ToList();

                resp.Pagination = new Pagination
                {
                    Curr_Page = page,
                    Limit = limit,
                    Total = total,
                    Total_Page = total == 0 ? 0 : (int)Math.Ceiling((double)total / limit)
                };

                resp.Success = true;
                resp.Message = payload == null
                    ? "Live data from DB snapshot (Redis empty/invalid)."
                    : $"Live data from Redis override (result only) for {machineKey}.";

                return resp;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetLiveAsync error");
                return FailResponse(page, limit, $"Exception: {ex.Message}", new());
            }
        }

        public async Task<ApiResponse<int>> CreateSnapshotAsync(
     string machineKey,
     string topic,
     int lineMasterId,
     CancellationToken ct = default)
        {
            var live = await GetLiveAsync(machineKey, topic, lineMasterId, 1, int.MaxValue, null, null, ct);
            if (!live.Success || live.Data.Count == 0)
                return Fail<int>(live.Message ?? "No live data to snapshot.", 0);

            try
            {
                // distinct devices (case-insensitive) untuk query IN (...)
                var devices = live.Data
                    .Select(d => d.Device)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var existing = await _context.FacilityCounts
                    .Where(fc => fc.LineMasterId == lineMasterId && devices.Contains(fc.Device))
                    .ToListAsync(ct);

                if (existing.Count == 0)
                    return Ok(0, "No existing rows to update.");

                // ==== FIX: tahan duplikat device di DB ====
                // Pilih 1 row "terbaru" per Device (case-insensitive)
                var byDevice = existing
                    .GroupBy(x => x.Device, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderByDescending(x => x.UpdatedAt)     // terbaru dulu
                              .ThenByDescending(x => x.CollectDate)
                              .ThenByDescending(x => x.Id)
                              .First(),
                        StringComparer.OrdinalIgnoreCase
                    );

                // (opsional) log deteksi duplikat
                // var dups = existing.GroupBy(x => x.Device, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => $"{g.Key}({g.Count()})");
                // if (dups.Any()) _logger.Warn($"[FacilityCount] Duplicate devices: {string.Join(", ", dups)}");

                int touched = 0;

                foreach (var d in live.Data)
                {
                    if (string.IsNullOrWhiteSpace(d.Device)) continue;

                    if (!byDevice.TryGetValue(d.Device, out var row)) continue;

                    row.Category = d.Category;
                    row.Result = d.Result;
                    row.LimitValue = d.LimitValue;
                    row.CollectDate = d.CollectDate;
                    row.Status = d.Status;
                    touched++;
                }

                if (touched == 0)
                    return Ok(0, "No existing rows matched live devices for update.");

                var affected = await _context.SaveChangesAsync(ct);
                return Ok(affected, $"Updated {touched} device(s).");
            }
            catch (DbUpdateException ex)
            {
                var baseMsg = ex.GetBaseException().Message;
                var code = (ex.InnerException as MySqlException)?.Number; // 1062, 1406, 1452, 1048, 1292, 1265, dll
                _logger.Error(ex, "CreateSnapshotAsync SaveChanges failed. MySQLCode={Code}, BaseMsg={BaseMsg}", code, baseMsg);
                return Fail<int>($"DB error ({code}): {baseMsg}", 0);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "CreateSnapshotAsync update-only error");
                return Fail<int>($"Exception: {ex.Message}", 0);
            }
        }


        public async Task<ApiResponse<int>> UpdateDeviceConfigByIdAsync(
            int id,
            string? newDevice = null,
            string? category = null,
            long? limitValue = null,
            CancellationToken ct = default)
        {
            try
            {
                var row = await _context.FacilityCounts
                    .FirstOrDefaultAsync(fc => fc.Id == id, ct);

                if (row == null)
                    return Fail<int>($"Data dengan Id={id} tidak ditemukan.", 0);

                // Validasi rename device (hindari duplikasi di line yang sama)
                if (!string.IsNullOrWhiteSpace(newDevice))
                {
                    var existsDup = await _context.FacilityCounts
                        .AnyAsync(fc =>
                                fc.LineMasterId == row.LineMasterId &&
                                fc.Id != id &&
                                fc.Device.ToLower() == newDevice.ToLower(),
                            ct);

                    if (existsDup)
                        return Fail<int>(
                            $"Device '{newDevice}' sudah ada pada LineMasterId={row.LineMasterId}.",
                            0);

                    row.Device = newDevice;
                }

                if (!string.IsNullOrWhiteSpace(category))
                    row.Category = category;

                if (limitValue.HasValue && limitValue.Value >= 0)
                    row.LimitValue = limitValue.Value;

                // Jangan sentuh 'Result'
                row.UpdatedAt = DateTime.Now;

                var affected = await _context.SaveChangesAsync(ct);
                return Ok(affected, $"Updated config untuk Id={id}.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "UpdateDeviceConfigByIdAsync error");
                return Fail<int>($"Exception: {ex.Message}", 0);
            }
        }

        public async Task<(bool Success, string? Message, byte[]? Bytes, string? FileName)> ExportFacilityCountAsync(
        string templatePath,
        int? lineNo = null,
        CancellationToken ct = default)
        {
            try
            {
                // Ambil data dari DB + join LineMaster
                var dataDb = await (from f in _context.FacilityCounts.AsNoTracking()
                                    join l in _context.LineMasters.AsNoTracking()
                                        on f.LineMasterId equals l.Id
                                    where !lineNo.HasValue || l.LineNo == lineNo.Value
                                    orderby l.LineNo, f.Device
                                    select new
                                    {
                                        LineNo = l.LineNo,     // int (buat isi Data.LineName sesuai class kamu)
                                        LineNameStr = l.LineName,   // string (buat isi Data.CoatLine di template)
                                        f.Category,
                                        f.Device,
                                        f.Result,                   // BIGINT NULL
                                        f.LimitValue,               // BIGINT NOT NULL
                                        f.CollectDate,
                                        f.Status
                                    })
                                    .ToListAsync(ct);

                if (dataDb.Count == 0)
                {
                    var scope = lineNo.HasValue ? $"LineNo={lineNo.Value}" : "ALL";
                    return (false, $"Tidak ada data facility_count untuk {scope}.", null, null);
                }

                // Map ke shape yang cocok untuk template
                // CATATAN:
                // - Template kamu sebelumnya pakai 'CoatLine' (string). 
                // - Class kamu punya 'LineName' (int). Aku isi KEDUANYA agar fleksibel:
                //   Data.LineName  = LineNo (int)       -> match class ExportFacilityCount
                //   Data.CoatLine  = LineNameStr (string)-> match template text
                var rows = dataDb.Select((d, i) => new
                {
                    No = i + 1,
                    LineName = d.LineNo,                         // int (sesuai class ExportFacilityCount)
                    CoatLine = d.LineNameStr,                    // string (buat template)
                    Category = d.Category,
                    Device = d.Device,                         // kalau kolom Device ada di template, ini terisi
                    Result = d.Result ?? 0L,
                    LimitValue = d.LimitValue,
                    CollectDate = d.CollectDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = d.Status
                })
                .ToList();

                // Model untuk MiniExcel Template
                var model = new
                {
                    ReportDate = DateTime.Now.ToString("dd-MMM-yy HH:mm"),
                    Data = rows,
                    Ddata = rows  // antisipasi placeholder typo {{Ddata.*}}
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
                var fileName = $"FacilityCount_{(lineNo.HasValue ? $"Line{lineNo.Value}_" : "ALL_")}{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                return (true, null, bytes, fileName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ExportFacilityCountAsync failed");
                return (false, ex.Message, null, null);
            }
        }

        // === Helper ===
        private static ApiResponse<T> Ok<T>(T data, string msg) => new() { Success = true, Message = msg, Data = data };
        private static ApiResponse<T> Fail<T>(string msg, T data) => new() { Success = false, Message = msg, Data = data };

        private static ApiResponse<List<FacilityCountRealtimeDto>> FailResponse(int page, int limit, string msg, List<FacilityCountRealtimeDto> data)
            => new()
            {
                Success = false,
                Message = msg,
                Data = data,
                Pagination = new Pagination { Curr_Page = page, Limit = limit, Total = data.Count, Total_Page = 0 }
            };

        private static ApiResponse<List<FacilityCountRealtimeDto>> OkResponse(int page, int limit, string msg, List<FacilityCountRealtimeDto> data)
            => new()
            {
                Success = true,
                Message = msg,
                Data = data,
                Pagination = new Pagination { Curr_Page = page, Limit = limit, Total = data.Count, Total_Page = 0 }
            };

        private static string MapCategory(string device)
        {
            var d = device.ToUpperInvariant();
            if (d.StartsWith("AD")) return "VALVE";
            if (d.StartsWith("M")) return "ROBOT";
            if (d.Contains("CY")) return "CY";
            return "OTHER";
        }

        private static string ComputeStatus(long result, long limit)
        {
            if (result >= limit) return "Need Maintenance";
            if (result >= (long)(0.8 * limit)) return "Warning";
            return "Good";
        }

        private static int StatusPriority(string status) => status switch
        {
            "Need Maintenance" => 0,
            "Warning" => 1,
            _ => 2 // "Good" atau lainnya di paling bawah
        };

    }
}