using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 工作空间实体
/// </summary>
[SugarTable("workspaces")]
public class Workspace
{
    /// <summary>
    /// 工作空间ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 工作空间名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作空间描述
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Description { get; set; }

    /// <summary>
    /// 工作空间标识 (唯一键)
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 所有者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long OwnerId { get; set; }

    /// <summary>
    /// 工作空间状态: 0-禁用, 1-正常
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
    /// 导航属性 - 所有者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Owner { get; set; }
}
