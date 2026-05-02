using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 任务执行历史实体
/// </summary>
[SugarTable("job_executions")]
public class JobExecution
{
    /// <summary>
    /// 执行ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 任务ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long JobId { get; set; }

    /// <summary>
    /// 执行状态: running, success, failed
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Status { get; set; } = "running";

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime StartedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// 输出消息
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? OutputMessage { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 触发方式: scheduler, manual
    /// </summary>
    [SugarColumn(Length = 50)]
    public string? TriggeredBy { get; set; }

    /// <summary>
    /// 导航属性 - 任务
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public ScheduledJob? Job { get; set; }
}
