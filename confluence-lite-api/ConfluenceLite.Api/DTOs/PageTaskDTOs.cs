namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 页面任务DTO
/// </summary>
public class PageTaskDto
{
    public long Id { get; set; }
    public long PageId { get; set; }
    public string TaskUid { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 页面任务勾选请求
/// </summary>
public class UpdatePageTaskRequest
{
    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; set; }
}
