using System.ComponentModel.DataAnnotations;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 创建分享请求
/// </summary>
public class CreateShareRequest
{
    /// <summary>
    /// 页面ID
    /// </summary>
    [Required]
    public long PageId { get; set; }

    /// <summary>
    /// 接收者用户ID (null = 匿名分享)
    /// </summary>
    public long? SharedWithId { get; set; }

    /// <summary>
    /// 访问密码 (可选)
    /// </summary>
    [StringLength(50)]
    public string? VisitPassword { get; set; }

    /// <summary>
    /// 过期时间 (null = 永不过期)
    /// </summary>
    public DateTime? ExpireAt { get; set; }

    /// <summary>
    /// 是否允许编辑
    /// </summary>
    public bool AllowEdit { get; set; }
}

/// <summary>
/// 分享DTO
/// </summary>
public class ShareDto
{
    public long Id { get; set; }
    public string? Code { get; set; }
    public long PageId { get; set; }
    public long SharedById { get; set; }
    public long? SharedWithId { get; set; }
    public bool HasPassword { get; set; }
    public bool AllowEdit { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpireAt { get; set; }
    public UserSummaryDto? SharedBy { get; set; }
    public UserSummaryDto? SharedWith { get; set; }
    public SharePageInfoDto? Page { get; set; }
}

/// <summary>
/// 分享页面简要信息
/// </summary>
public class SharePageInfoDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// 公开分享信息DTO (无需认证即可查看)
/// </summary>
public class PublicShareInfoDto
{
    public bool HasPassword { get; set; }
    public bool AllowEdit { get; set; }
    public bool IsExpired { get; set; }
    public bool IsUserSpecific { get; set; }
    public string? SharedByDisplayName { get; set; }
    public string? PageTitle { get; set; }
}

/// <summary>
/// 通过分享更新页面请求
/// </summary>
public class UpdateSharePageRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }
}

/// <summary>
/// 更新分享设置请求
/// </summary>
public class UpdateShareRequest
{
    public DateTime? ExpireAt { get; set; }
    public bool? AllowEdit { get; set; }
    public string? VisitPassword { get; set; }
}
