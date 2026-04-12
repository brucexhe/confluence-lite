using System.ComponentModel.DataAnnotations;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 页面创建请求
/// </summary>
public class CreatePageRequest
{
    /// <summary>
    /// 页面标题
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 页面内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 所属工作空间ID
    /// </summary>
    [Required]
    public long WorkspaceId { get; set; }

    /// <summary>
    /// 父页面ID (可选)
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 页面状态: 0-草稿, 1-已发布
    /// </summary>
    public int Status { get; set; } = 0;
}

/// <summary>
/// 页面更新请求
/// </summary>
public class UpdatePageRequest
{
    /// <summary>
    /// 页面标题
    /// </summary>
    [StringLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// 页面内容
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// 页面状态: 0-草稿, 1-已发布, 2-已归档
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 父页面ID
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int? SortOrder { get; set; }
}

/// <summary>
/// 页面DTO
/// </summary>
public class PageDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int Status { get; set; }
    public long WorkspaceId { get; set; }
    public long CreatorId { get; set; }
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 所属工作空间
    /// </summary>
    public WorkspaceSummaryDto? Workspace { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    public UserSummaryDto? Creator { get; set; }

    /// <summary>
    /// 子页面列表
    /// </summary>
    public List<PageDto>? Children { get; set; }
}

/// <summary>
/// 工作空间简要DTO
/// </summary>
public class WorkspaceSummaryDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string? Icon { get; set; }
}

/// <summary>
/// 页面树节点DTO
/// </summary>
public class PageTreeNodeDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public int SortOrder { get; set; }
    public int Status { get; set; }
    public List<PageTreeNodeDto>? Children { get; set; }
}
