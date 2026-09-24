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

/// <summary>
/// 空间任务总览DTO - 按页面分组
/// </summary>
public class PageTaskGroupDto
{
    public long PageId { get; set; }
    public string PageTitle { get; set; } = string.Empty;
    public List<PageTaskDto> Tasks { get; set; } = [];
}
