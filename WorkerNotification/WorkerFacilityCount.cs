using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using StackExchange.Redis;
using WorkerNotification.Entities;
using WorkerNotification.Helper;
using WorkerNotification.Singletone;

namespace WorkerNotification
{
    /// <summary>
    /// Worker minimalis:
    /// - Baca semua konfigurasi dari Settings.ini via Config.Instance
    /// - Poll tiap PollSeconds (default 5s)
    /// - Status "Warning"  -> webhook Warning (TTL 24h per device)
    /// - Status "Need Maintenance" -> webhook NeedMaintenance (TTL 2h per device)
    /// </summary>
    public class WorkerFacilityCount : BackgroundService
    {
        private readonly ILogger<WorkerFacilityCount> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly RedisAppOptions _redisOpt;

        // Konfigurasi dari INI (disiapkan di ctor)(default)
        private RestClient _apiClient = default!;     // untuk GET live
        private string _warningWebhook = "http://localhost:5678/webhook/warning-email";
        private string _needMaintWebhook = "http://localhost:5678/webhook/need-maintenance-email";
        private TimeSpan _pollInterval = TimeSpan.FromSeconds(5);
        private TimeSpan _warnTtl = TimeSpan.FromHours(24);
        private TimeSpan _maintTtl = TimeSpan.FromHours(2);
        private MachineEntry[] _machines = Array.Empty<MachineEntry>();

        public WorkerFacilityCount(
            ILogger<WorkerFacilityCount> logger,
            IConnectionMultiplexer redis,
            RedisAppOptions redisOpt)
        {
            _logger = logger;
            _redis = redis;
            _redisOpt = redisOpt;

            // ====== BACA Settings.ini LANGSUNG DI SINI ======
            var cfg = WorkerNotification.Singletone.Config.Instance;

            // Base URL API (default)
            var baseUrl = cfg.Read("BaseUrl", "FacilityApi") ?? "http://127.0.0.1:5023";
            _apiClient = new RestClient(new RestClientOptions(baseUrl)
            {
                ThrowOnAnyError = false,
                MaxTimeout = 15000
            });
            _apiClient.AddDefaultHeader("Accept", "application/json");
            _apiClient.AddDefaultHeader("User-Agent", "WorkerNotification/1.0");

            // Webhook endpoints (default jika null)
            _warningWebhook = cfg.Read("WarningUrl", "Webhook") ?? _warningWebhook;
            _needMaintWebhook = cfg.Read("NeedMaintenanceUrl", "Webhook") ?? _needMaintWebhook;

            // Interval & TTL (fallback default jika null/invalid)
            var pollSec = cfg.ReadInt("PollSeconds", "Worker", 5);
            var warnHours = cfg.ReadInt("WarningTtlHours", "Worker", 24);
            var mtHours = cfg.ReadInt("MaintenanceTtlHours", "Worker", 2);

            _pollInterval = TimeSpan.FromSeconds(Math.Max(1, pollSec));
            _warnTtl = TimeSpan.FromHours(Math.Max(1, warnHours));
            _maintTtl = TimeSpan.FromHours(Math.Max(1, mtHours));

            // Daftar mesin
            var machineCount = cfg.ReadInt("Machines", "FacilityApi", 0);
            var list = new List<MachineEntry>();
            for (int i = 1; i <= machineCount; i++)
            {
                var name = cfg.Read($"Machine{i}.Name", "FacilityApi") ?? $"Machine-{i}";
                var url = cfg.Read($"Machine{i}.Url", "FacilityApi"); // boleh null → skip
                if (!string.IsNullOrWhiteSpace(url))
                    list.Add(new MachineEntry { Name = name, Url = url });
            }
            _machines = list.ToArray();

            if (_machines.Length == 0)
                throw new InvalidOperationException("FacilityApi.Machines kosong / tidak valid di Settings.ini.");

            _logger.LogInformation("Init OK. BaseUrl={BaseUrl}, Machines={Count}, Poll={Poll}s, TTL warn={Warn}h, maint={Maint}h",
                baseUrl, _machines.Length, _pollInterval.TotalSeconds, _warnTtl.TotalHours, _maintTtl.TotalHours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)   
            {
                var started = DateTime.UtcNow;

                try
                {
                    // Panggil semua mesin paralel
                    var tasks = _machines.Select(m => FetchMachineAsync(m, stoppingToken));
                    await Task.WhenAll(tasks);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { _logger.LogError(ex, "Unhandled error on worker loop."); }

                var delay = _pollInterval - (DateTime.UtcNow - started);
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task FetchMachineAsync(MachineEntry me, CancellationToken ct)
        {
            var machineName = me.Name ?? "(unnamed)";
            if (string.IsNullOrWhiteSpace(me.Url))
            {
                _logger.LogWarning("[{Machine}] URL kosong, skip.", machineName);
                return;
            }

            // GET: support absolute / relative
            var req = (me.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                       me.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                      ? new RestRequest(new Uri(me.Url), Method.Get)
                      : new RestRequest(me.Url, Method.Get);

            var resp = await _apiClient.ExecuteAsync(req, ct);
            if (!resp.IsSuccessful)
            {
                _logger.LogWarning("[{Machine}] GET failed: {Code} {Desc}. Err: {Err}",
                    machineName, (int)resp.StatusCode, resp.StatusDescription, resp.ErrorMessage ?? "(none)");
                return;
            }

            // Parse JSON
            var json = resp.Content ?? "{}";
            FacilityCount? payload = null;
            try
            {
                payload = JsonConvert.DeserializeObject<FacilityCount>(json);
            }
            catch (JsonException jx)
            {
                _logger.LogError(jx, "[{Machine}] JSON parse error. Snippet: {Snippet}",
                    machineName, json.Length > 400 ? json[..400] + "..." : json);
                return;
            }

            var items = payload?.Data ?? new List<FacilityItem>();
            var first = items.FirstOrDefault();
            _logger.LogInformation("[{Machine}] Items={Count}. First: {Device}->{Status} ({Result})",
                machineName, items.Count, first?.Device ?? "-", first?.Status ?? "-", first?.Result ?? 0);

            // Redis
            var db = _redis.GetDatabase(_redisOpt.Db);
            var prefix = string.IsNullOrWhiteSpace(_redisOpt.KeyPrefix) ? "cataler" : _redisOpt.KeyPrefix;

            // === WARNING (TTL 24h) ===
            foreach (var it in items.Where(x => string.Equals(x.Status?.Trim(), "Warning", StringComparison.OrdinalIgnoreCase)))
            {
                var deviceKey = string.IsNullOrWhiteSpace(it.Device) ? "unknown" : it.Device.Replace(" ", "_");
                var key = $"{prefix}:facility:warn:{machineName}:{deviceKey}";

                var ok = await db.StringSetAsync(key, DateTimeOffset.UtcNow.ToString("o"), _warnTtl, When.NotExists);
                if (!ok)
                {
                    _logger.LogInformation("[{Machine}] WARNING skipped {Device} (<{H}h).", machineName, it.Device, _warnTtl.TotalHours);
                    continue;
                }

                var body = new
                {
                    machine = machineName,
                    item = new
                    {
                        it.Id,
                        it.LineMasterId,
                        it.LineName,
                        it.Category,
                        it.Device,
                        it.Result,
                        it.LimitValue,
                        CollectDate = it.CollectDate.ToString("o"),
                        it.Status
                    }
                };

                try
                {
                    var post = new RestRequest(new Uri(_warningWebhook), Method.Post)
                        .AddHeader("Content-Type", "application/json")
                        .AddJsonBody(body);
                    var postResp = await _apiClient.ExecuteAsync(post, ct);

                    if (!postResp.IsSuccessful)
                    {
                        _logger.LogWarning("[{Machine}] Webhook WARNING failed: {Code} {Desc}. Err: {Err}",
                            machineName, (int)postResp.StatusCode, postResp.StatusDescription, postResp.ErrorMessage ?? "(none)");
                        // rollback supaya dicoba lagi di loop berikut
                        await db.KeyDeleteAsync(key);
                    }
                    else
                    {
                        _logger.LogInformation("[{Machine}] Webhook WARNING sent for {Device}.", machineName, it.Device);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Machine}] Webhook WARNING exception.", machineName);
                    await db.KeyDeleteAsync(key); // rollback
                }
            }

            // === NEED MAINTENANCE (TTL 2h) ===
            foreach (var it in items.Where(x => string.Equals(x.Status?.Trim(), "Need Maintenance", StringComparison.OrdinalIgnoreCase)))
            {
                var deviceKey = string.IsNullOrWhiteSpace(it.Device) ? "unknown" : it.Device.Replace(" ", "_");
                var key = $"{prefix}:facility:maint:{machineName}:{deviceKey}";

                var ok = await db.StringSetAsync(key, DateTimeOffset.UtcNow.ToString("o"), _maintTtl, When.NotExists);
                if (!ok)
                {
                    _logger.LogInformation("[{Machine}] NEED-MAINT skipped {Device} (<{H}h).", machineName, it.Device, _maintTtl.TotalHours);
                    continue;
                }

                var body = new
                {
                    machine = machineName,
                    item = new
                    {
                        it.Id,
                        it.LineMasterId,
                        it.LineName,
                        it.Category,
                        it.Device,
                        it.Result,
                        it.LimitValue,
                        CollectDate = it.CollectDate.ToString("o"),
                        it.Status
                    }
                };

                try
                {
                    var post = new RestRequest(new Uri(_needMaintWebhook), Method.Post)
                        .AddHeader("Content-Type", "application/json")
                        .AddJsonBody(body);
                    var postResp = await _apiClient.ExecuteAsync(post, ct);

                    if (!postResp.IsSuccessful)
                    {
                        _logger.LogWarning("[{Machine}] Webhook NEED-MAINT failed: {Code} {Desc}. Err: {Err}",
                            machineName, (int)postResp.StatusCode, postResp.StatusDescription, postResp.ErrorMessage ?? "(none)");
                        await db.KeyDeleteAsync(key); // rollback
                    }
                    else
                    {
                        _logger.LogInformation("[{Machine}] Webhook NEED-MAINT sent for {Device}.", machineName, it.Device);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[{Machine}] Webhook NEED-MAINT exception.", machineName);
                    await db.KeyDeleteAsync(key); // rollback
                }
            }
        }
    }
}
