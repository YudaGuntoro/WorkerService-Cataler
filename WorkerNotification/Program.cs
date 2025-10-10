using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting.WindowsServices; 
using StackExchange.Redis;
using WorkerNotification;
using WorkerNotification.Helper;
using WorkerNotification.Singletone;

// Pastikan relative path (Settings.ini, dll.) pakai folder EXE service
Directory.SetCurrentDirectory(AppContext.BaseDirectory); // ⬅️

var builder = Host.CreateApplicationBuilder(args);

// Jalankan sebagai Windows Service (nama di Services.msc)
builder.Services.AddWindowsService(o =>
{
    o.ServiceName = "Worker Notification"; // ⬅️
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Saat benar2 jalan sbg service, tulis juga ke Windows Event Log (optional tapi berguna)
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Logging.AddEventLog(); // ⬅️ lihat di Event Viewer → Windows Logs → Application
}

// Siapkan Config singleton + optional override path via env
var cfg = Config.Instance;
var iniPath = Environment.GetEnvironmentVariable("SETTINGS__PATH");
if (!string.IsNullOrWhiteSpace(iniPath)) cfg.SetPath(iniPath);

// Redis dari INI (fallback default jika null)
var redisHost = cfg.Read("Host", "Redis") ?? "127.0.0.1";
var redisPort = cfg.ReadInt("Port", "Redis", 6379);
var redisPwd = cfg.Read("Password", "Redis");
var redisDb = cfg.ReadInt("Db", "Redis", 0);
var keyPrefix = cfg.Read("KeyPrefix", "Redis") ?? "cataler";

// Koneksi Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var ro = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectRetry = 3,
        ConnectTimeout = 5000,
        SyncTimeout = 5000
    };
    ro.EndPoints.Add(redisHost, redisPort);
    if (!string.IsNullOrWhiteSpace(redisPwd)) ro.Password = redisPwd;
    return ConnectionMultiplexer.Connect(ro);
});
builder.Services.AddSingleton(new RedisAppOptions { Db = redisDb, KeyPrefix = keyPrefix });

// Hosted worker (konfigurasi lain dibaca dari Settings.ini)
builder.Services.AddHostedService<WorkerFacilityCount>();

await builder.Build().RunAsync();
