using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面实体
/// </summary>
[SugarTable("pages")]
public class Page
{
    /// <summary>
    /// 页面ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 页面标题
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 页面内容 (存储格式化内容，如HTML、存储格式等)
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Content { get; set; }

    /// <summary>
    /// 页面状态: 0-草稿, 1-已发布, 2-已归档
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Status { get; set; } = 0;

    /// <summary>
    /// 所属工作空间ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long WorkspaceId { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatorId { get; set; }

    /// <summary>
    /// 父页面ID (用于层级结构)
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 当前版本号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Version { get; set; } = 1;

    /// <summary>
    /// 最后修改者用户ID
    /// </summary>
    public long? LastModifierId { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 删除者用户ID
    /// </summary>
    public long? DeletedById { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
