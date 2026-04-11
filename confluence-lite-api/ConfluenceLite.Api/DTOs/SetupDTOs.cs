using System.ComponentModel.DataAnnotations;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 安装状态响应
/// </summary>
public class SetupStatusResponse
{
    /// <summary>
    /// 是否已安装
    /// </summary>
    public bool Installed { get; set; }
}

/// <summary>
/// 数据库配置请求
/// </summary>
public class DatabaseConfigRequest
{
    /// <summary>
    /// 数据库类型: PostgreSQL, MySQL, SqlServer, Oracle (目前仅支持 PostgreSQL)
    /// </summary>
    [Required]
    public string DbType { get; set; } = "PostgreSQL";

    /// <summary>
    /// 主机地址
    /// </summary>
    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 端口号
    /// </summary>
    [Required]
    public int Port { get; set; } = 5432;

    /// <summary>
    /// 数据库名称
    /// </summary>
    [Required]
    public string Database { get; set; } = "confluencelite";

    /// <summary>
    /// 数据库用户名
    /// </summary>
    [Required]
    public string Username { get; set; } = "postgres";

    /// <summary>
    /// 数据库密码
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 数据库连接测试响应
/// </summary>
public class TestConnectionResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// PostgreSQL 版本
    /// </summary>
    public string? Version { get; set; }
}

/// <summary>
/// 安装请求
/// </summary>
public class SetupRequest
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    [Required]
    public DatabaseConfigRequest Database { get; set; } = new();

    /// <summary>
    /// 管理员用户名
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string AdminUsername { get; set; } = string.Empty;

    /// <summary>
    /// 管理员密码
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string AdminPassword { get; set; } = string.Empty;

    /// <summary>
    /// 管理员邮箱
    /// </summary>
    [EmailAddress]
    public string? AdminEmail { get; set; }

    /// <summary>
    /// 管理员显示名称
    /// </summary>
    [StringLength(100)]
    public string? AdminDisplayName { get; set; }

    /// <summary>
    /// 默认空间名称
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string SpaceName { get; set; } = string.Empty;

    /// <summary>
    /// 默认空间标识 (仅英文、数字、-、_)
    /// </summary>
    [Required]
    [RegularExpression(@"^[a-zA-Z0-9\-_]+$")]
    [StringLength(50, MinimumLength = 1)]
    public string SpaceKey { get; set; } = string.Empty;
}

/// <summary>
/// 安装响应
/// </summary>
public class SetupResponse
{
    /// <summary>
    /// JWT Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token 类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 管理员用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 默认工作空间ID
    /// </summary>
    public long WorkspaceId { get; set; }

    /// <summary>
    /// Overview 页面ID
    /// </summary>
    public long PageId { get; set; }
}
