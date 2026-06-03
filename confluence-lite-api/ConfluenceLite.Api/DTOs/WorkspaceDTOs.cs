using System.ComponentModel.DataAnnotations;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 工作空间创建请求
/// </summary>
public class CreateWorkspaceRequest
{
    /// <summary>
    /// 工作空间名称
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作空间描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 工作空间标识 (唯一键)
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 2)]
    [RegularExpression("^[a-zA-Z0-9-_]+$", ErrorMessage = "Key只能包含字母、数字、横线和下划线")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 图标（颜色渐变或图片URL）
    /// </summary>
    [StringLength(500)]
    public string? Icon { get; set; }

    /// <summary>
    /// 是否设置为默认空间
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// 工作空间更新请求
/// </summary>
public class UpdateWorkspaceRequest
{
    /// <summary>
    /// 工作空间名称
    /// </summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// 工作空间描述
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// 工作空间状态: 0-禁用, 1-正常
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// 图标（颜色渐变或图片URL）
    /// </summary>
    [StringLength(500)]
    public string? Icon { get; set; }

    /// <summary>
    /// 是否设置为默认空间
    /// </summary>
    public bool? IsDefault { get; set; }
}

/// <summary>
/// 工作空间DTO
/// </summary>
public class WorkspaceDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public long OwnerId { get; set; }
    public int Status { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 所有者信息 (简单)
    /// </summary>
    public UserSummaryDto? Owner { get; set; }

    /// <summary>
    /// 页面数量
    /// </summary>
    public int PageCount { get; set; }
}

/// <summary>
/// 用户简要DTO
/// </summary>
public class UserSummaryDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}
