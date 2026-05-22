using System.Text.Json.Serialization;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 审计日志条目
/// </summary>
public class AuditLogEntry
{
    /// <summary>
    /// 操作者用户ID
    /// </summary>
    public long? ActorId { get; set; }

    /// <summary>
    /// 操作者用户名
    /// </summary>
    public string? ActorUsername { get; set; }

    /// <summary>
    /// 操作类型 (如 "security.config.update", "backup.create")
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型 (如 "security", "auth", "backup")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 实体ID
    /// </summary>
    public long? EntityId { get; set; }

    /// <summary>
    /// 详细信息 (JSON字符串)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// 客户端IP地址
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 审计变更详情
/// </summary>
public class AuditChangeDetails
{
    /// <summary>
    /// 变更的字段字典
    /// </summary>
    [JsonPropertyName("changes")]
    public Dictionary<string, ChangeDetail>? Changes { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    [JsonPropertyName("requestPath")]
    public string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [JsonPropertyName("requestMethod")]
    public string? RequestMethod { get; set; }
}

/// <summary>
/// 单个字段变更详情
/// </summary>
public class ChangeDetail
{
    /// <summary>
    /// 旧值
    /// </summary>
    [JsonPropertyName("old")]
    public string? OldValue { get; set; }

    /// <summary>
    /// 新值
    /// </summary>
    [JsonPropertyName("new")]
    public string? NewValue { get; set; }
}
