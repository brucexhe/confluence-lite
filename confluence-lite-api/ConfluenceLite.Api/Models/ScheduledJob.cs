using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 定时任务实体
/// </summary>
[SugarTable("scheduled_jobs")]
public class ScheduledJob
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 任务名称
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务描述
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// 任务类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string JobType { get; set; } = string.Empty;

    /// <summary>
    /// Cron 表达式
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>
    /// 任务数据 (JSON)
    /// </summary>
    [SugarColumn(ColumnDataType = "jsonb")]
    public string? JobData { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 最后执行时间
    /// </summary>
    public DateTime? LastRunAt { get; set; }

    /// <summary>
    /// 下次执行时间
    /// </summary>
    public DateTime? NextRunAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
