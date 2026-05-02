namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 定时任务 DTO
/// </summary>
public class ScheduledJobDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Cron { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime? LastRun { get; set; }
    public DateTime? NextRun { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 任务执行历史 DTO
/// </summary>
public class JobExecutionDto
{
    public long Id { get; set; }
    public long JobId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? DurationMs { get; set; }
    public string? OutputMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TriggeredBy { get; set; }
}

/// <summary>
/// 创建任务请求
/// </summary>
public class CreateJobRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string? JobData { get; set; }
}

/// <summary>
/// 更新任务请求
/// </summary>
public class UpdateJobRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CronExpression { get; set; }
    public bool? Enabled { get; set; }
}

/// <summary>
/// 任务统计 DTO
/// </summary>
public class JobStatsDto
{
    public int Running { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int Pending { get; set; }
}
