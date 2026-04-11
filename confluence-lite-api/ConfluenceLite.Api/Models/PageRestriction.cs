using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面限制实体 (查看/编辑权限控制)
/// </summary>
[SugarTable("page_restrictions")]
public class PageRestriction
{
    /// <summary>
    /// 限制ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

    /// <summary>
    /// 限制类型: 0-查看, 1-编辑
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int RestrictionType { get; set; }

    /// <summary>
    /// 目标类型: 0-用户组, 1-用户
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int TargetType { get; set; }

    /// <summary>
    /// 目标ID (用户组ID或用户ID)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long TargetId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 页面
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Page? Page { get; set; }
}
