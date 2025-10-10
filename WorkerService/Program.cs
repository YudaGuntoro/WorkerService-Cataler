using Mapster;
using Serilog;
using StackExchange.Redis;
using WorkerService.Domains.Mappings;
using WorkerService.Domains.Models;
using WorkerService.MQTT.Clients;
using WorkerService.MQTT.Handlers;
using WorkerService.MQTT.Handlers.SQL;
using WorkerService.MQTT.Interfaces;
using Microsoft.Extensions.Hosting;
using WorkerService.MQTT.Handlers.Service;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cfg = WorkerService.Singletone.Config.Instance;

        var mqttHost = cfg.Read("Host", "MQTT") ?? "broker.emqx.io";
        var mqttPort = cfg.ReadInt("Port", "MQTT", 1883);
        var clientId = cfg.Read("ClientId", "MQTT") ?? "WorkerService";
        var topicM1 = cfg.Read("TopicMachine1", "MQTT") ?? "/Act_Machine_1";
        var topicCount = cfg.Read("TopicCounting", "MQTT") ?? "/counting";

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
        
        // Setup log folder
        string logFolderPath = Path.Combine(AppContext.BaseDirectory, "Logs");
        if (!Directory.Exists(logFolderPath))
        {
            Directory.CreateDirectory(logFolderPath);
            Console.WriteLine($"[INFO] Log folder created at: {logFolderPath}");
        }

        // Konfigurasi Serilog
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


            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "Cataler Worker Service"; // bebas kamu ganti
            });

            builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();
            builder.Services.AddMapster(); // auto-scan semua IRegister

            builder.Services.Configure<HostOptions>(opt =>
            {
                opt.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
            });

            // Registrasi MqttClientService via interface
            builder.Services.AddSingleton<IMqttClientService, MqttClientService>();
            builder.Services.AddHostedService(sp => (MqttClientService)sp.GetRequiredService<IMqttClientService>());
            // MQTT Client Service
            builder.Services.AddSingleton<IMqttPublisher>(sp =>(IMqttPublisher)sp.GetRequiredService<IMqttClientService>());
            // Hosted Service untuk MQTT client
            builder.Services.AddHostedService<MqttClientService>();

            // Handler Machine1 → cukup hosted service
            builder.Services.AddHostedService<Machine1Handler>();
            builder.Services.AddHostedService<Machine2Handler>();

            // Query / helper class
            builder.Services.AddSingleton<MachineQuery>();
            builder.Services.AddSingleton<ICounterRedisService, CounterRedisService>();

            var host = builder.Build();

            Log.Information("Host built successfully.");
            await host.RunAsync();
            Log.Information("Application terminated normally.");
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application encountered a fatal error and is terminating.");
            Console.WriteLine("[FATAL] Unhandled exception, check Logs.");
        }
        finally
        {
            Log.Information("Closing and flushing log...");
            Log.CloseAndFlush();
        }
    }
}