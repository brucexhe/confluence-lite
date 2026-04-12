using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 关注/观察实体 (用户关注页面或空间)
/// </summary>
[SugarTable("watchers")]
public class Watcher
{
    /// <summary>
    /// 关注ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 实体类型: 0-页面, 1-空间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int EntityType { get; set; }

    /// <summary>
    /// 实体ID (页面ID或工作空间ID)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long EntityId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? User { get; set; }
}
