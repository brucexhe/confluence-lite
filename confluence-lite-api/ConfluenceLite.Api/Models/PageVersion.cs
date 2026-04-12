using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面版本历史实体
/// </summary>
[SugarTable("page_versions")]
public class PageVersion
{
    /// <summary>
    /// 版本记录ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int VersionNumber { get; set; }

    /// <summary>
    /// 标题快照
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 内容快照
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Content { get; set; }

    /// <summary>
    /// 变更摘要
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? DiffSummary { get; set; }

    /// <summary>
    /// 版本备注 (用户填写的修改说明)
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? ChangeComment { get; set; }

    /// <summary>
    /// 编辑者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long EditorId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 导航属性 - 页面
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Page? Page { get; set; }

    /// <summary>
    /// 导航属性 - 编辑者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Editor { get; set; }
}
