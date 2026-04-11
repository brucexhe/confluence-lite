using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面模板实体
/// </summary>
[SugarTable("page_templates")]
public class PageTemplate
{
    /// <summary>
    /// 模板ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 模板名称
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// 模板内容
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Content { get; set; }

    /// <summary>
    /// 所属工作空间ID (null表示全局模板)
    /// </summary>
    public long? WorkspaceId { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatorId { get; set; }

    /// <summary>
    /// 是否默认模板
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int SortOrder { get; set; }

    /// <summary>
    /// 状态: 0-禁用, 1-启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Status { get; set; } = 1;

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

    /// <summary>
    /// 导航属性 - 工作空间
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Workspace? Workspace { get; set; }

    /// <summary>
    /// 导航属性 - 创建者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Creator { get; set; }
}
