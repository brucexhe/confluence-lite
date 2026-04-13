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
        ".docx", ".pptx", ".xlsx",
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
    public string UploadPath { get; set; } = "uploads/attachments";
}
