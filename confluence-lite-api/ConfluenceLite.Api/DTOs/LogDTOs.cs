namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 日志条目 DTO
/// </summary>
public class LogEntryDto
{
    public long Id { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? Source { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 日志查询请求
/// </summary>
public class LogQueryRequest : PagedRequest
{
    public string? Level { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchText { get; set; }
}

/// <summary>
/// 日志导出请求
/// </summary>
public class LogExportRequest
{
    public string? Level { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Format { get; set; } = "csv"; // csv, json
}
