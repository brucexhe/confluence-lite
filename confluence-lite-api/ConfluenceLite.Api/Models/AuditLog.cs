using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 审计日志实体
/// </summary>
[SugarTable("audit_log")]
public class AuditLog
{
    /// <summary>
    /// 日志ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 操作者用户ID (null表示系统操作)
    /// </summary>
    public long? ActorId { get; set; }

    /// <summary>
    /// 操作类型 (如 "page.create", "user.login", "space.permission.change")
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型 (如 "page", "user", "space")
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体ID
    /// </summary>
    public long? EntityId { get; set; }

    /// <summary>
    /// 详细信息 (JSON格式)
    /// </summary>
    [SugarColumn(ColumnDataType = "jsonb")]
    public string? Details { get; set; }

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    [SugarColumn(Length = 45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 导航属性 - 操作者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Actor { get; set; }
}
