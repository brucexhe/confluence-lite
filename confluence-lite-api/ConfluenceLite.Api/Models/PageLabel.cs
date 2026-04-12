using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面标签实体
/// </summary>
[SugarTable("page_labels")]
public class PageLabel
{
    /// <summary>
    /// 标签记录ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

    /// <summary>
    /// 标签名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatedById { get; set; }

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
    /// 导航属性 - 创建者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? CreatedBy { get; set; }
}
