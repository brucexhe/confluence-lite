using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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

// 获取数据目录路径
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");

// 确保数据目录存在
if (!Directory.Exists(dataDir))
{
    Directory.CreateDirectory(dataDir);
}

// 加载运行时配置（如果存在）
var runtimeConfigPath = Path.Combine(dataDir, "appsettings.runtime.json");
if (File.Exists(runtimeConfigPath))
{
    builder.Configuration.AddJsonFile(runtimeConfigPath, optional: false, reloadOnChange: true);
    Console.WriteLine($"[Config] Loaded runtime config from: {runtimeConfigPath}");
}

builder.Services.Configure<AppConfiguration>(
    builder.Configuration.GetSection("App")
);

var appConfig = new AppConfiguration();
builder.Configuration.GetSection("App").Bind(appConfig);

// ========== 检查安装状态 ==========
var installedPath = Path.Combine(dataDir, "INSTALLED");
var isInstalled = File.Exists(installedPath);
Console.WriteLine($"[Setup] INSTALLED file check: {(isInstalled ? "Found" : "Not found")} at {installedPath}");

// ========== 注册配置到 DI 容器 ==========
builder.Services.AddSingleton(appConfig.Jwt);
builder.Services.AddSingleton(appConfig);

// ========== 数据库配置 - Native AOT 兼容 ==========
StaticConfig.EnableAot = false;
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<PageService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<SetupService>();
builder.Services.AddScoped<UploadService>();
builder.Services.AddHttpClient("Gotenberg");
builder.Services.AddHttpClient();
builder.Services.AddScoped<OfficePreviewService>();
builder.Services.AddScoped<OidcService>();
builder.Services.AddScoped<UserGroupService>();
builder.Services.AddScoped<SystemInfoService>();
builder.Services.AddScoped<LogService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<JobSchedulerService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<ConfluenceLite.Api.Services.Confluence.ConfluenceXmlParser>();
builder.Services.AddScoped<ConfluenceLite.Api.Services.Confluence.ConfluenceImportService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<RecentService>();
builder.Services.AddScoped<ShareService>();

// ========== 审计日志服务 ==========
builder.Services.Configure<AuditLogOptions>(
    builder.Configuration.GetSection("App:AuditLog"));
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
builder.Services.AddHostedService(sp => (AuditLogService)sp.GetRequiredService<IAuditLogService>());
builder.Services.AddSingleton<IAuditDiffGenerator, AuditDiffGenerator>();

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

// ========== JWT 配置 ==========
builder.Services.AddAuthentication()
    .AddJwtBearer();

// ========== 速率限制 ==========
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// ========== 配置 Kestrel 服务器限制 ==========
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 1073741824; // 1GB (默认是 30MB)
});

// 如果使用 IIS，也需要配置
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 1073741824; // 1GB
});

// ========== 应用构建 ==========
var app = builder.Build();

// ========== 安全校验：JWT Secret ==========
var defaultJwtSecret = "YourSuperSecretKeyHereThatIsAtLeast32CharactersLong!";
if (string.Equals(appConfig.Jwt.Secret, defaultJwtSecret, StringComparison.Ordinal))
{
    if (app.Environment.IsProduction())
    {
        Console.WriteLine("[Security][FATAL] 检测到默认 JWT Secret，生产环境拒绝启动。");
        Console.WriteLine("[Security] 请通过安装向导初始化，或在 data/appsettings.runtime.json 中配置 App:Jwt:Secret。");
        return;
    }
    Console.WriteLine("[Security][WARNING] 检测到默认 JWT Secret，存在 token 伪造风险，生产环境将拒绝启动。");
}

// ========== 中间件配置 ==========

// 静态文件服务（用于附件下载）
var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwroot))
{
    Directory.CreateDirectory(wwwroot);
}

// 确保上传附件目录存在
var attachmentUploadPath = Path.Combine(wwwroot, appConfig.Attachment.UploadPath);
if (!Directory.Exists(attachmentUploadPath))
{
    Directory.CreateDirectory(attachmentUploadPath);
}

// 附件静态资源鉴权：/uploads 下的附件/图片要求登录，匿名访问返回 401
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
    {
        var jwtOptions = context.RequestServices.GetRequiredService<JwtOptions>();
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var user = JwtAuthMiddleware.ResolveUser(context, jwtOptions, configuration);
        if (!user.IsAuthenticated)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"success\":false,\"message\":\"未授权，请先登录\",\"errorCode\":401}");
            return;
        }
    }
    await next();
});

// 默认文件（index.html）- 必须在 UseStaticFiles 之前
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = new PhysicalFileProvider(wwwroot),
    RequestPath = ""
});

// // 静态文件服务（用于前端资源和附件下载）
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwroot),
    RequestPath = ""
});

// 附件单独映射到 /uploads 路径
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwroot),
    RequestPath = "/uploads"
});

// ForwardedHeaders - 用于获取 nginx 反向代理后的真实 Scheme/Host
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
    // 只允许来自本地网络的代理转发（可根据实际情况调整）
    // KnownNetworks = { }, // 允许所有网络
    // KnownProxies = { }  // 允许所有代理
});

// CORS
app.UseCors("AllowSpecificOrigins");

// 速率限制
app.UseRateLimiter();

// JWT 认证中间件
app.UseMiddleware<JwtAuthMiddleware>();

// 审计日志中间件
app.UseMiddleware<AuditLoggingMiddleware>();

// ========== 注册 Minimal API 路由 ==========
ApiRoutes.RegisterRoutes(app);

// ========== SPA 前端路由支持 ==========
// 对于所有未被 API 路由匹配的请求，返回 index.html
// 这使得 Vue Router 可以处理前端路由（如 /、/about、/dashboard 等）
app.MapFallback(async context =>
{
    var indexPath = Path.Combine(wwwroot, "index.html");

    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("index.html not found. Please ensure wwwroot/index.html exists.");
    }
});

app.Run();
