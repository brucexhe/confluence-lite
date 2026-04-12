using System.Text;
using System.Text.Json;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;
using ConfluenceLite.Api.Models;
using Npgsql;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 安装服务 - 处理首次安装向导
/// </summary>
public class SetupService
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _env;

    public SetupService(
        AppDbContext db,
        TokenService tokenService,
        IConfiguration configuration,
        IHostEnvironment env)
    {
        _db = db;
        _tokenService = tokenService;
        _configuration = configuration;
        _env = env;
    }

    /// <summary>
    /// 检查是否已安装
    /// </summary>
    public bool IsInstalled()
    {
        var installedPath = GetInstalledFilePath();
        return File.Exists(installedPath);
    }

    /// <summary>
    /// 测试数据库连接
    /// </summary>
    public async Task<(TestConnectionResponse? result, string? error)> TestConnectionAsync(DatabaseConfigRequest config)
    {
        // 目前仅支持 PostgreSQL
        if (!string.Equals(config.DbType, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            return (new TestConnectionResponse
            {
                Success = false,
                Error = $"暂不支持 {config.DbType}，目前仅支持 PostgreSQL"
            }, null);
        }

        var connectionString = BuildConnectionString(config);

        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT version()";
            var versionResult = await cmd.ExecuteScalarAsync();
            var version = versionResult?.ToString() ?? "Unknown";

            return (new TestConnectionResponse
            {
                Success = true,
                Version = version.Split(',')[0] // 只取主版本信息
            }, null);
        }
        catch (NpgsqlException ex)
        {
            return (new TestConnectionResponse
            {
                Success = false,
                Error = $"连接失败: {ex.Message}"
            }, null);
        }
        catch (Exception ex)
        {
            return (null, $"连接异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 执行完整安装
    /// </summary>
    public async Task<(SetupResponse? result, string? error)> InstallAsync(SetupRequest request)
    {
        // 1. 再次确认未安装
        if (IsInstalled())
        {
            return (null, "系统已安装，不能重复执行安装");
        }

        var connectionString = BuildConnectionString(request.Database);

        // 2. 测试连接
        try
        {
            using var testConn = new NpgsqlConnection(connectionString);
            await testConn.OpenAsync();
        }
        catch (Exception ex)
        {
            return (null, $"数据库连接失败: {ex.Message}");
        }

        // 3. 保存配置到 appsettings.json
        try
        {
            await SaveConfigAsync(request.Database);
        }
        catch (Exception ex)
        {
            return (null, $"保存配置失败: {ex.Message}");
        }

        // 4. 创建新的数据库连接执行安装
        var installDb = AppDbContext.CreateClient(connectionString);

        // 5. 初始化数据库表
        try
        {
            DatabaseInitializer.Initialize(installDb);
        }
        catch (Exception ex)
        {
            return (null, $"数据库初始化失败: {ex.Message}");
        }

        // 6. 创建管理员用户
        long adminUserId;
        try
        {
            var adminUser = new User
            {
                Username = request.AdminUsername,
                Email = request.AdminEmail,
                PasswordHash = PasswordService.HashPassword(request.AdminPassword),
                DisplayName = string.IsNullOrEmpty(request.AdminDisplayName)
                    ? request.AdminUsername
                    : request.AdminDisplayName,
                Status = 1,
                IsAdmin = true,
                TimeZone = "UTC",
                Locale = "zh-CN"
            };

            adminUserId = await installDb.Insertable(adminUser)
                .ExecuteReturnBigIdentityAsync();
        }
        catch (NpgsqlException ex) when (ex.SqlState == "23505")
        {
            return (null, $"用户名 \"{request.AdminUsername}\" 已存在，请更换用户名后重试");
        }
        catch (Exception ex)
        {
            return (null, $"创建管理员失败: {ex.Message}");
        }

        // 7. 创建默认空间
        long workspaceId;
        try
        {
            var workspace = new Workspace
            {
                Name = request.SpaceName,
                Key = request.SpaceKey.ToUpperInvariant(),
                OwnerId = adminUserId,
                Status = 1,
                Icon = "📚",
                Color = "#0049b0"
            };

            workspaceId = await installDb.Insertable(workspace)
                .ExecuteReturnBigIdentityAsync();
        }
        catch (NpgsqlException ex) when (ex.SqlState == "23505")
        {
            return (null, $"空间标识 \"{request.SpaceKey}\" 已存在，请更换后重试");
        }
        catch (Exception ex)
        {
            return (null, $"创建默认空间失败: {ex.Message}");
        }

        // 8. 创建 Overview 示例页面
        long pageId;
        try
        {
            var overviewPage = new Page
            {
                Title = "Overview",
                Content = GetDefaultOverviewContent(request.SpaceName),
                Status = 1, // 已发布
                WorkspaceId = workspaceId,
                CreatorId = adminUserId,
                SortOrder = 0,
                Version = 1
            };

            pageId = await installDb.Insertable(overviewPage)
                .ExecuteReturnBigIdentityAsync();

            // 更新空间首页ID
            await installDb.Updateable<Workspace>()
                .SetColumns(w => w.HomePageId == pageId)
                .Where(w => w.Id == workspaceId)
                .ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            return (null, $"创建默认页面失败: {ex.Message}");
        }

        // 9. 创建 INSTALLED 文件
        try
        {
            CreateInstalledFile(request.AdminUsername);
        }
        catch (Exception ex)
        {
            return (null, $"创建安装标记文件失败: {ex.Message}");
        }

        // 10. 生成 JWT Token
        var token = _tokenService.GenerateToken(adminUserId, request.AdminUsername);

        return (new SetupResponse
        {
            Token = token,
            TokenType = "Bearer",
            UserId = adminUserId,
            WorkspaceId = workspaceId,
            PageId = pageId
        }, null);
    }

    /// <summary>
    /// 构建数据库连接字符串
    /// </summary>
    private static string BuildConnectionString(DatabaseConfigRequest config)
    {
        return $"Host={config.Host};Port={config.Port};Database={config.Database};Username={config.Username};Password={config.Password}";
    }

    /// <summary>
    /// 保存数据库配置到 appsettings.json
    /// </summary>
    private async Task SaveConfigAsync(DatabaseConfigRequest config)
    {
        // 找到 appsettings.json 文件
        var configFileName = _env.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";
        var configPath = Path.Combine(_env.ContentRootPath, configFileName);

        string jsonContent;
        if (File.Exists(configPath))
        {
            jsonContent = await File.ReadAllTextAsync(configPath);
        }
        else
        {
            // fallback 到 appsettings.json
            configPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            jsonContent = await File.ReadAllTextAsync(configPath);
        }

        var jsonOptions = new JsonDocumentOptions { AllowTrailingCommas = true };

        using var doc = JsonDocument.Parse(jsonContent, jsonOptions);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        foreach (JsonProperty property in doc.RootElement.EnumerateObject())
        {
            if (property.Name == "App")
            {
                writer.WritePropertyName("App");
                writer.WriteStartObject();

                foreach (JsonProperty appProp in property.Value.EnumerateObject())
                {
                    if (appProp.Name == "Database")
                    {
                        writer.WritePropertyName("Database");
                        writer.WriteStartObject();
                        writer.WriteString("ConnectionString", BuildConnectionString(config));
                        writer.WriteNumber("DbType", 4);
                        writer.WriteBoolean("AutoCreateTables", true);
                        writer.WriteBoolean("EnableSqlLog", appProp.Value.TryGetProperty("EnableSqlLog", out var sqlLogProp)
                            ? sqlLogProp.GetBoolean()
                            : true);
                        writer.WriteEndObject();
                    }
                    else
                    {
                        // 保留其他 App 配置
                        property.Value.WriteTo(writer);
                        break; // App 下除了 Database 的其他属性在同一个层级
                    }
                }

                writer.WriteEndObject();
            }
            else
            {
                property.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        await writer.FlushAsync();

        var newJson = Encoding.UTF8.GetString(stream.ToArray());
        await File.WriteAllTextAsync(configPath, newJson);
    }

    /// <summary>
    /// 获取 INSTALLED 文件路径
    /// </summary>
    private string GetInstalledFilePath()
    {
        return Path.Combine(_env.ContentRootPath, "INSTALLED");
    }

    /// <summary>
    /// 创建 INSTALLED 标记文件
    /// </summary>
    private void CreateInstalledFile(string adminUser)
    {
        var info = new InstalledInfo
        {
            InstalledAt = DateTime.UtcNow.ToString("O"),
            Version = "1.0.0",
            AdminUser = adminUser
        };
        var json = JsonSerializer.Serialize(info, AppJsonContext.Default.InstalledInfo);
        File.WriteAllText(GetInstalledFilePath(), json);
    }

    /// <summary>
    /// 获取默认 Overview 页面内容
    /// </summary>
    private static string GetDefaultOverviewContent(string spaceName)
    {
        return $@"<h1>欢迎使用 Confluence Lite！</h1>
<p>这是您<strong>{spaceName}</strong>空间的首页。</p>
<h2>快速开始</h2>
<ul>
<li>点击左侧页面树浏览和创建页面</li>
<li>使用顶部搜索栏快速查找内容</li>
<li>在页面中使用富文本编辑器编写文档</li>
</ul>
<h2>主要功能</h2>
<ul>
<li><strong>知识管理</strong> — 创建、编辑和组织文档</li>
<li><strong>团队协作</strong> — 评论、@提及和分享</li>
<li><strong>权限控制</strong> — 空间和页面级别的访问控制</li>
<li><strong>版本历史</strong> — 页面修改记录和回滚</li>
</ul>
<p>从左侧导航栏创建您的第一个页面，或者编辑此页面来自定义您的空间首页。</p>";
    }
}
