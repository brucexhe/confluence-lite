using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面任务实体 - 从页面内容的任务列表(li[data-task-id])中提取的投影索引
/// </summary>
[SugarTable("page_tasks")]
public class PageTask
{
    /// <summary>
    /// 任务记录ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

    /// <summary>
    /// 工作空间ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long WorkspaceId { get; set; }

    /// <summary>
    /// 任务唯一标识 (对应内容中 li 的 data-task-id)
    /// </summary>
    [SugarColumn(Length = 40, IsNullable = false)]
    public string TaskUid { get; set; } = string.Empty;

    /// <summary>
    /// 任务内容 (纯文本，用于列表展示)
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Content { get; set; }

    /// <summary>
    /// 是否已完成
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsCompleted { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 完成者用户ID
    /// </summary>
    public long? CompletedById { get; set; }

    /// <summary>
    /// 在页面中的出现顺序
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Position { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatorId { get; set; }

    /// <summary>
    /// 是否已删除 (内容中任务被移除时软删)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsDeleted { get; set; }

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
