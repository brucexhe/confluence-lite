using System.ComponentModel.DataAnnotations;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 评论创建请求
/// </summary>
public class CreateCommentRequest
{
    /// <summary>
    /// 页面ID
    /// </summary>
    [Required]
    public long PageId { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 父评论ID (用于回复)
    /// </summary>
    public long? ParentId { get; set; }
}

/// <summary>
/// 评论更新请求
/// </summary>
public class UpdateCommentRequest
{
    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// 评论DTO
/// </summary>
public class CommentDto
{
    public long Id { get; set; }
    public long PageId { get; set; }
    public long UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public long? ParentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 评论者信息
    /// </summary>
    public UserSummaryDto? User { get; set; }

    /// <summary>
    /// 回复列表
    /// </summary>
    public List<CommentDto>? Replies { get; set; }
}
