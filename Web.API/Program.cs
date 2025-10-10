using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using Web.API;
using Web.API.Persistence.Context;
using Web.API.Persistence.Shared;
using Web.API.Mappings.Mappings;
using Web.API.Mappings.DTOs.Config;
using NLog;
using NLog.Web;

var logger = LogManager
    .Setup()
    .LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

// ===== Builder (IIS) =====
var builder = WebApplication.CreateBuilder(args);

// Logging: pakai NLog
builder.Logging.ClearProviders();
builder.Host.UseNLog();

// =====================
// 0) Settings.ini via Config (Win32) — tetap boleh
// =====================
var cfg = Config.Instance;
var iniPath = Environment.GetEnvironmentVariable("SETTINGS__PATH");
if (!string.IsNullOrWhiteSpace(iniPath))
    cfg.SetPath(iniPath);

// =====================
// 1) JWT
// =====================
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key tidak ditemukan di appsettings.json");

builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// =====================
// 2) Redis
// =====================
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    const string S = "Redis";
    var host = cfg.Read("Host", S) ?? "localhost";
    var port = cfg.ReadInt("Port", S, 6379);
    var opt = ConfigurationOptions.Parse($"{host}:{port}");
    opt.AllowAdmin = cfg.ReadBool("AllowAdmin", S, false);
    opt.AbortOnConnectFail = cfg.ReadBool("AbortOnConnectFail", S, false);
    opt.ConnectRetry = cfg.ReadInt("ConnectRetry", S, 3);
    opt.ConnectTimeout = cfg.ReadInt("ConnectTimeout", S, 5000);
    opt.SyncTimeout = cfg.ReadInt("SyncTimeout", S, 5000);
    opt.KeepAlive = cfg.ReadInt("KeepAlive", S, 60);
    opt.Ssl = cfg.ReadBool("Ssl", S, false);
    var pwd = cfg.Read("Password", S);
    if (!string.IsNullOrWhiteSpace(pwd)) opt.Password = pwd;
    var retryMs = cfg.ReadInt("ReconnectRetryMs", S, 5000);
    opt.ReconnectRetryPolicy = new ExponentialRetry(retryMs);

    var mux = ConnectionMultiplexer.Connect(opt);

    var lg = sp.GetService<ILoggerFactory>()?.CreateLogger("Redis");
    if (lg != null)
    {
        mux.ConnectionFailed += (_, e) => lg.LogError(e.Exception, "Redis ConnectionFailed: {FailureType} @ {Endpoint}", e.FailureType, e.EndPoint);
        mux.ConnectionRestored += (_, e) => lg.LogInformation("Redis ConnectionRestored: {Endpoint}", e.EndPoint);
        mux.ErrorMessage += (_, e) => lg.LogError("Redis ErrorMessage: {Message}", e.Message);
        mux.ConfigurationChanged += (_, e) => lg.LogInformation("Redis ConfigurationChanged: {Endpoint}", e.EndPoint);
    }

    return mux;
});

builder.Services.AddSingleton(new RedisAppOptions
{
    Db = cfg.ReadInt("Db", "Redis", 0),
    KeyPrefix = cfg.Read("KeyPrefix", "Redis") ?? "cataler"
});

// =====================
// 3) MQTT Settings
// =====================
var mqttSettings = new MqttSettings
{
    Host = cfg.Read("Host", "MQTT") ?? "broker.emqx.io",
    Port = cfg.ReadInt("Port", "MQTT", 1883),
    ClientId = cfg.Read("ClientId", "MQTT") ?? "WorkerService",
    TopicMachine1 = cfg.Read("TopicMachine1", "MQTT") ?? "/Act_Machine_1",
    TopicCounting = cfg.Read("TopicCounting", "MQTT") ?? "/counting",
    Username = cfg.Read("Username", "MQTT"),
    Password = cfg.Read("Password", "MQTT"),
    UseTls = cfg.ReadBool("UseTls", "MQTT", false)
};
builder.Services.AddSingleton(mqttSettings);

// =====================
// 4) CORS + Services + Mapster + Swagger + DB
// =====================
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddInfrastructure();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

MappingConfig.RegisterMappings();
builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
builder.Services.AddScoped<IMapper, ServiceMapper>();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Web API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Masukkan JWT token: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// === Database MySQL ===
var mysqlVersion = new MySqlServerVersion(new Version(8, 0, 36));
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseMySql(ConfigGlobal.MysqlConnString, mysqlVersion)
       .EnableSensitiveDataLogging()
       .EnableDetailedErrors());

builder.WebHost.UseUrls("http://0.0.0.0:5023");

var app = builder.Build();
var isDev = app.Environment.IsDevelopment();

// === Middleware ===
if (isDev) app.UseDeveloperExceptionPage();

// (Opsional, kalau punya HTTPS di IIS)
// app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// Di IIS biasanya production ⇒ aktifkan auth
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
// Kalau semua controller butuh auth, hapus AllowAnonymous di atas
app.MapControllers(); // .RequireAuthorization(); // aktifkan kalau mau paksa auth default

app.Run();
