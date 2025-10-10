using Mapster;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices; // ⬅️ tambah ini
using Microsoft.Extensions.Logging;
using Serilog;
using StackExchange.Redis;
using System.IO;
using WorkerLogger;
using WorkerLogger.Domains.Mappings;
using WorkerLogger.MQTT.Clients;
using WorkerLogger.MQTT.Handlers.SQL;
using WorkerLogger.MQTT.Interfaces;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Pastikan relative path (Settings.ini, Logs, dsb.) mengacu ke folder EXE service
        Directory.SetCurrentDirectory(AppContext.BaseDirectory); // ⬅️ penting saat run sbg service

        // --- Load INI ---
        var cfg = WorkerLogger.Singletone.Config.Instance;

        // MQTT
        var mqttHost = cfg.Read("Host", "MQTT") ?? "broker.emqx.io";
        var mqttPort = cfg.ReadInt("Port", "MQTT", 1883);
        var clientId = cfg.Read("ClientId", "MQTT") ?? "WorkerService";
        var topicM1 = cfg.Read("TopicMachine1", "MQTT") ?? "/Act_Machine_1";
        var topicCount = cfg.Read("TopicCounting", "MQTT") ?? "/counting";
        var topicAlm1 = cfg.Read("TopicLogMachineAlarm1", "MQTT"); // optional


        var redisHost = cfg.Read("Host", "Redis") ?? "localhost";
        var redisPort = cfg.ReadInt("Port", "Redis", 6379);
        var redisDb = cfg.ReadInt("Db", "Redis", 0);
        var keyPrefix = cfg.Read("KeyPrefix", "Redis") ?? "cataler";

        // Redis: hormati "Connection=" dari INI, tambahkan opsi penting agar tidak fail-fast
        var redisConnBase = cfg.Read("Connection", "Redis") ?? $"{redisHost}:{redisPort}";
        var redisConn =
            $"{redisConnBase},defaultDatabase={redisDb}," +
            "abortConnect=false," +                                  // <- kunci: jangan matikan program jika belum konek
            $"connectRetry={cfg.ReadInt("ConnectRetry", "Redis", 3)}," +
            $"connectTimeout={cfg.ReadInt("ConnectTimeout", "Redis", 5000)}," +
            $"keepAlive={cfg.ReadInt("KeepAlive", "Redis", 60)}," +
            $"syncTimeout={cfg.ReadInt("SyncTimeout", "Redis", 5000)}";

        // optional flags kalau ada di INI
        var redisPassword = cfg.Read("Password", "Redis");
        if (!string.IsNullOrWhiteSpace(redisPassword))
            redisConn += $",password={redisPassword}";

        var redisSsl = cfg.Read("Ssl", "Redis");
        if (!string.IsNullOrWhiteSpace(redisSsl) && bool.TryParse(redisSsl, out var useSsl) && useSsl)
            redisConn += ",ssl=True";



        // --- Serilog sink file ---
        var logFolderPath = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logFolderPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logFolderPath, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        try
        {
            Log.Information("Application starting...");

            var builder = Host.CreateApplicationBuilder(args);

            // Jalankan sebagai Windows Service (nama tampilan di Services.msc)
            builder.Services.AddWindowsService(o =>
            {
                o.ServiceName = "Worker Logger (Toho)"; // ⬅️ ganti sesuai kebutuhan
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            // Logging
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            // Jika benar-benar berjalan sbg service, log juga ke Windows Event Log (opsional tapi berguna)
            if (WindowsServiceHelpers.IsWindowsService())
                builder.Logging.AddEventLog();

            // Mapster (scan IRegister)
            builder.Services.AddMapster();

            // Host options
            builder.Services.Configure<HostOptions>(o =>
            {
                o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            // SQL helper & MQTT background worker
            builder.Services.AddSingleton<MachineQuery>();
            builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
            builder.Services.AddHostedService(sp => (MqttClientService)sp.GetRequiredService<IMqttClientService>());

            var host = builder.Build();
            Log.Information("Host built successfully.");

            await host.RunAsync();
            Log.Information("Application terminated normally.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application encountered a fatal error and is terminating.");
        }
        finally
        {
            Log.Information("Closing and flushing log...");
            Log.CloseAndFlush();
        }
    }
}
