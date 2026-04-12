using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;
using ConfluenceLite.Api.Mappers;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Routes;

var builder = WebApplication.CreateBuilder(args);

// ========== 配置加载 ==========
builder.Services.Configure<AppConfiguration>(
    builder.Configuration.GetSection("App")
);

var appConfig = new AppConfiguration();
builder.Configuration.GetSection("App").Bind(appConfig);

// ========== 检查安装状态 ==========
var installedPath = Path.Combine(builder.Environment.ContentRootPath, "INSTALLED");
var isInstalled = File.Exists(installedPath);
Console.WriteLine($"[Setup] INSTALLED file check: {(isInstalled ? "Found" : "Not found")} at {installedPath}");

// ========== 注册 JwtOptions 到 DI 容器 ==========
builder.Services.AddSingleton(appConfig.Jwt);

// ========== 数据库配置 - Native AOT 兼容 ==========
StaticConfig.EnableAot = true;
var dbType = (DbType)appConfig.Database.DbType;
var connectionConfig = new ConnectionConfig
{
    ConnectionString = appConfig.Database.ConnectionString,
    DbType = dbType,
    IsAutoCloseConnection = true,
    InitKeyType = InitKeyType.Attribute,
    AopEvents = new AopEvents
    {
        OnLogExecuting = (sql, p) =>
        {
            if (appConfig.Database.EnableSqlLog)
            {
                Console.WriteLine($"[SQL] {sql}");
                if (p != null && p.Any())
                {
                    Console.WriteLine($"[Parameters] {string.Join(", ", p.Select(a => $"{a.ParameterName}={a.Value}"))}");
                }
            }
        }
    }
};

// AOT 模式下 SqlSugarClient 不能用单例，每次 new
builder.Services.AddSingleton(connectionConfig);
builder.Services.AddScoped<ISqlSugarClient>(sp =>
{
    var config = sp.GetRequiredService<ConnectionConfig>();
    return new SqlSugarClient(config);
});
builder.Services.AddScoped<AppDbContext>();

// 启动时用临时 client 初始化数据库表
var db = new SqlSugarClient(connectionConfig);

// 仅在已安装状态下初始化数据库
if (isInstalled && appConfig.Database.AutoCreateTables)
{
    try
    {
        DatabaseInitializer.Initialize(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database] Warning: Failed to initialize tables: {ex.Message}");
        Console.WriteLine("[Database] Application will start but database operations may fail");
    }
}
else if (!isInstalled)
{
    Console.WriteLine("[Setup] First-time setup mode — database will be initialized during setup wizard");
}

// ========== 服务注册 - Native AOT 兼容 ==========
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<PageService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<SetupService>();

// ========== JSON 配置 - Native AOT 使用源生成器 ==========
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.WriteIndented = false;
    options.SerializerOptions.TypeInfoResolver = AppJsonContext.Default;
});

// ========== CORS 配置 ==========
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(appConfig.Cors.AllowedOrigins)
              .WithMethods(appConfig.Cors.AllowedMethods)
              .WithHeaders(appConfig.Cors.AllowedHeaders)
              .AllowCredentials();
    });
});

// ========== 添加 OpenAPI 支持 (开发环境) ==========
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
}

// ========== JWT 配置 ==========
builder.Services.AddAuthentication()
    .AddJwtBearer();

// ========== 应用构建 ==========
var app = builder.Build();

// ========== 中间件配置 ==========

// CORS
app.UseCors("AllowSpecificOrigins");

// JWT 认证中间件
app.UseMiddleware<JwtAuthMiddleware>();

// ========== 注册 Minimal API 路由 ==========
ApiRoutes.RegisterRoutes(app);

// ========== 健康检查 ==========
app.MapGet("/health", () =>
{
    return Results.Json(new HealthResponse(), AppJsonContext.Default.HealthResponse);
});

// ========== API 根路径 ==========
app.MapGet("/", () =>
{
    return Results.Json(new ApiInfoResponse(), AppJsonContext.Default.ApiInfoResponse);
});

app.Run();
