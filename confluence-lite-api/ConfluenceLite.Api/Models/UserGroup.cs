using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 用户组实体
/// </summary>
[SugarTable("user_groups")]
public class UserGroup
{
    /// <summary>
    /// 用户组ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 组名
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// 是否默认组 (新用户自动加入)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否系统组 (不可删除)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsSystem { get; set; }

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
