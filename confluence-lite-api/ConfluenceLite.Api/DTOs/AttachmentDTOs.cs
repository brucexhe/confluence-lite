using System.ComponentModel.DataAnnotations;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 附件上传响应DTO
/// </summary>
public class AttachmentDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long PageId { get; set; }
    public long CreatorId { get; set; }
    public DateTime CreatedAt { get; set; }
    public UserSummaryDto? Creator { get; set; }
}

/// <summary>
/// 附件列表项DTO
/// </summary>
public class AttachmentListItemDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
