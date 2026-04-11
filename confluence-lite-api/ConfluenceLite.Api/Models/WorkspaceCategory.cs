using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 工作空间分类实体
/// </summary>
[SugarTable("workspace_categories")]
public class WorkspaceCategory
{
    /// <summary>
    /// 分类ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 工作空间ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long WorkspaceId { get; set; }

    /// <summary>
    /// 分类名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 工作空间
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Workspace? Workspace { get; set; }
}
