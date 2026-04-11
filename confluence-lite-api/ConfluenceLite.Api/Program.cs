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

// ========== 注册 JwtOptions 到 DI 容器 ==========
builder.Services.AddSingleton(appConfig.Jwt);

// ========== 数据库配置 - Native AOT 兼容 ==========
var dbType = (DbType)appConfig.Database.DbType;
var db = new SqlSugarClient(new ConnectionConfig
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
});

// 单例注册数据库客户端
builder.Services.AddSingleton<ISqlSugarClient>(db);
builder.Services.AddSingleton<AppDbContext>();

// 初始化数据库表
if (appConfig.Database.AutoCreateTables)
{
    try
    {
        db.CodeFirst.InitTables(
            typeof(User),
            typeof(Workspace),
            typeof(Page),
            typeof(PageComment),
            typeof(UserGroup),
            typeof(UserGroupMember),
            typeof(WorkspacePermission),
            typeof(WorkspaceCategory),
            typeof(PageVersion),
            typeof(PageRestriction),
            typeof(PageLabel),
            typeof(PageTemplate),
            typeof(Attachment),
            typeof(Draft),
            typeof(ContentProperty),
            typeof(Notification),
            typeof(Watcher),
            typeof(Mention),
            typeof(Share),
            typeof(SearchHistory),
            typeof(ActivityEvent),
            typeof(AuditLog),
            typeof(UserFavorite)
        );
        Console.WriteLine("[Database] Tables initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database] Warning: Failed to initialize tables: {ex.Message}");
        Console.WriteLine("[Database] Application will start but database operations may fail");
        Console.WriteLine("[Database] Please ensure PostgreSQL is running and the connection string is correct");
    }
}

// ========== 服务注册 - Native AOT 兼容 ==========
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddScoped<PageService>();
builder.Services.AddScoped<CommentService>();

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
