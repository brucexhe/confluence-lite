namespace ConfluenceLite.Api.Data;

/// <summary>
/// 应用配置
/// </summary>
public class AppConfiguration
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    public DatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// JWT配置
    /// </summary>
    public JwtOptions Jwt { get; set; } = new();

    /// <summary>
    /// CORS配置
    /// </summary>
    public CorsOptions Cors { get; set; } = new();

    /// <summary>
    /// 附件存储配置
    /// </summary>
    public AttachmentOptions Attachment { get; set; } = new();

    /// <summary>
    /// Gotenberg 配置
    /// </summary>
    public GotenbergOptions Gotenberg { get; set; } = new();

    /// <summary>
    /// 站点设置
    /// </summary>
    public SiteSettings SiteSettings { get; set; } = new();

    /// <summary>
    /// 显示设置
    /// </summary>
    public DisplaySettings DisplaySettings { get; set; } = new();

    /// <summary>
    /// 安全设置
    /// </summary>
    public SecuritySettings SecuritySettings { get; set; } = new();
}

/// <summary>
/// 数据库配置选项
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// PostgreSQL连接字符串
    /// </summary>
    public string ConnectionString { get; set; } = "Host=localhost;Port=5432;Database=confluence_lite;Username=postgres;Password=postgres";

    /// <summary>
    /// 数据库类型 (PostgreSQL = 1)
    /// </summary>
    public int DbType { get; set; } = 1;

    /// <summary>
    /// 是否自动创建数据库表
    /// </summary>
    public bool AutoCreateTables { get; set; } = true;

    /// <summary>
    /// 是否启用SQL日志
    /// </summary>
    public bool EnableSqlLog { get; set; } = true;
}

/// <summary>
/// JWT配置选项
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// 密钥
    /// </summary>
    public string Secret { get; set; } = "YourSuperSecretKeyHereThatIsAtLeast32CharactersLong!";

    /// <summary>
    /// 发行者
    /// </summary>
    public string Issuer { get; set; } = "ConfluenceLite";

    /// <summary>
    /// 受众
    /// </summary>
    public string Audience { get; set; } = "ConfluenceLiteUsers";

    /// <summary>
    /// 过期时间(分钟)
    /// </summary>
    public int ExpirationMinutes { get; set; } = 1440; // 24小时
}

/// <summary>
/// CORS配置选项
/// </summary>
public class CorsOptions
{
    /// <summary>
    /// 允许的来源
    /// </summary>
    public string[] AllowedOrigins { get; set; } = new[] { "http://localhost:5173", "http://localhost:3000" };

    /// <summary>
    /// 允许的方法
    /// </summary>
    public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

    /// <summary>
    /// 允许的头部
    /// </summary>
    public string[] AllowedHeaders { get; set; } = new[] { "*" };

    /// <summary>
    /// 是否允许凭证
    /// </summary>
    public bool AllowCredentials { get; set; } = true;
}

/// <summary>
/// 附件存储配置选项
/// </summary>
public class AttachmentOptions
{
    /// <summary>
    /// 最大文件大小 (字节)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 52428800; // 50MB

    /// <summary>
    /// 允许的文件扩展名
    /// </summary>
    public string[] AllowedExtensions { get; set; } = new[]
    {
        // 图片
        ".jpg", ".jpeg", ".bmp", ".png", ".gif",
        // 文档
        ".docx", ".pptx", ".xlsx", ".pdf",
        // 视频
        ".mp4",
        // 压缩
        ".zip",
        // 文本/代码
        ".txt", ".md", ".json", ".xml"
    };

    /// <summary>
    /// 上传文件存储路径 (相对于 wwwroot)
    /// </summary>
    public string UploadPath { get; set; } = "uploads";
}

/// <summary>
/// Gotenberg 文档转换服务配置
/// </summary>
public class GotenbergOptions
{
    /// <summary>
    /// 是否启用 Office 文档预览
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Gotenberg 服务地址
    /// </summary>
    public string BaseUrl { get; set; } = "http://gotenberg:3000";

    /// <summary>
    /// 请求超时时间(秒)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}

/// <summary>
/// 站点设置
/// </summary>
public class SiteSettings
{
    public string SiteName { get; set; } = "Confluence Lite";
    public string SiteDescription { get; set; } = "";
    public string SiteLogo { get; set; } = "";
    public string SiteDomain { get; set; } = "";
    public string DefaultLanguage { get; set; } = "zh-CN";
    public string DefaultHomePage { get; set; } = "";
    public string Timezone { get; set; } = "Asia/Shanghai";
    public bool AllowRegistration { get; set; } = true;
}

/// <summary>
/// 显示设置
/// </summary>
public class DisplaySettings
{
    public string DefaultTheme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#0052cc";
    public bool CompactMode { get; set; }
    public int PageSize { get; set; } = 20;
    public string PageTreeExpandMode { get; set; } = "first";
    public bool ShowPageViews { get; set; } = true;
    public bool ShowAuthorInfo { get; set; } = true;
    public bool ShowLastModified { get; set; } = true;
    public string DefaultEditorMode { get; set; } = "visual";
    public int AutoSaveInterval { get; set; } = 60;
    public bool EnableSpellCheck { get; set; } = true;
    public int DefaultSidebarWidth { get; set; } = 260;
    public bool ShowSpaceIcon { get; set; } = true;
    public bool AllowCollapseSidebar { get; set; } = true;
}

/// <summary>
/// 安全设置
/// </summary>
public class SecuritySettings
{
    // 用户注册
    public bool AllowPublicRegistration { get; set; } = false;
    public bool RequireEmailVerification { get; set; }
    public string DefaultUserRole { get; set; } = "user";

    // 密码策略
    public int MinPasswordLength { get; set; } = 8;
    public string PasswordComplexity { get; set; } = "medium";
    public int PasswordExpireDays { get; set; }

    // 会话管理
    public int SessionTimeout { get; set; } = 60;
    public bool AllowConcurrentSessions { get; set; } = true;
    public bool AllowRememberMe { get; set; } = true;

    // 访问控制
    public string IpWhitelist { get; set; } = "";
    public bool EnableTwoFactor { get; set; }
}
