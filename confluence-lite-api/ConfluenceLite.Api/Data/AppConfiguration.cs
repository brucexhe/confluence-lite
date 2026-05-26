using ConfluenceLite.Api.Attributes;

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

    /// <summary>
    /// 邮件设置
    /// </summary>
    public MailSettings MailSettings { get; set; } = new();

    /// <summary>
    /// 身份验证设置
    /// </summary>
    public AuthSettings AuthSettings { get; set; } = new();

    /// <summary>
    /// 审计日志设置
    /// </summary>
    public AuditLogOptions AuditLog { get; set; } = new();

    /// <summary>
    /// 备份设置
    /// </summary>
    public BackupOptions Backup { get; set; } = new();
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
    /// 数据库类型 (PostgreSQL = 4)
    /// </summary>
    public int DbType { get; set; } = 4;

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

/// <summary>
/// 邮件设置
/// </summary>
public class MailSettings
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Encryption { get; set; } = "tls";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
    public string Username { get; set; } = "";
    [SensitiveData]
    public string Password { get; set; } = "";
    public bool NotifyOnRegister { get; set; }
    public string AdminEmail { get; set; } = "";
    public string EmailSignature { get; set; } = "";
}

/// <summary>
/// 身份验证设置
/// </summary>
public class AuthSettings
{
    // 密码认证
    public bool PasswordEnabled { get; set; } = true;
    public bool EmailLoginEnabled { get; set; } = false;
    public bool OidcEnabled { get; set; }
    public string OidcProviderName { get; set; } = "";
    public string OidcDiscoveryUrl { get; set; } = "";
    public string OidcClientId { get; set; } = "";
    [SensitiveData]
    public string OidcClientSecret { get; set; } = "";
    public string OidcScopes { get; set; } = "openid profile email";
    public bool OidcAutoCreateUser { get; set; } = true;
    public string OidcDefaultRole { get; set; } = "user";

    // LDAP
    public bool LdapEnabled { get; set; }
    public string LdapUrl { get; set; } = "";
    public string LdapBindDn { get; set; } = "";
    [SensitiveData]
    public string LdapBindPassword { get; set; } = "";
    public string LdapBaseDn { get; set; } = "";
    public string LdapUserFilter { get; set; } = "(uid={username})";
}

/// <summary>
/// 审计日志配置选项
/// </summary>
public class AuditLogOptions
{
    /// <summary>
    /// 是否启用审计日志
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 批处理大小
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// 刷新间隔（秒）
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 最大队列长度
    /// </summary>
    public int MaxQueueSize { get; set; } = 10000;
}

/// <summary>
/// 备份配置选项
/// </summary>
public class BackupOptions
{
    /// <summary>
    /// 是否启用自动备份
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 备份间隔天数
    /// </summary>
    public int IntervalDays { get; set; } = 1;

    /// <summary>
    /// 备份内容: database, attachments, config
    /// </summary>
    public string[] Content { get; set; } = new[] { "database", "attachments", "config" };

    /// <summary>
    /// 备份保留天数
    /// </summary>
    public int RetentionDays { get; set; } = 30;
}
