namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 活动记录 DTO
/// </summary>
public class ActivityDto
{
    /// <summary>
    /// 活动ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 活动类型: page_created, page_updated, page_deleted, comment_added, etc.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 活动描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 执行用户
    /// </summary>
    public UserSummaryDto? User { get; set; }

    /// <summary>
    /// 关联的工作空间ID
    /// </summary>
    public long? WorkspaceId { get; set; }

    /// <summary>
    /// 关联的工作空间名称
    /// </summary>
    public string? WorkspaceName { get; set; }

    /// <summary>
    /// 关联的工作空间Key
    /// </summary>
    public string? WorkspaceKey { get; set; }

    /// <summary>
    /// 关联的页面ID
    /// </summary>
    public long? PageId { get; set; }

    /// <summary>
    /// 关联的页面标题
    /// </summary>
    public string? PageTitle { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 活动列表请求参数
/// </summary>
public class ActivityListRequest
{
    /// <summary>
    /// 工作空间ID（可选，为空则查询所有工作空间）
    /// </summary>
    public long? WorkspaceId { get; set; }

    /// <summary>
    /// 活动类型过滤（可选）
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 返回数量（默认20，最大100）
    /// </summary>
    public int Count { get; set; } = 20;

    /// <summary>
    /// 偏移量（用于分页）
    /// </summary>
    public int Offset { get; set; } = 0;
}
