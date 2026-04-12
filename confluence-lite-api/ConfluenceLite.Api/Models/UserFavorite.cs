using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 用户收藏实体
/// </summary>
[SugarTable("user_favorites")]
public class UserFavorite
{
    /// <summary>
    /// 收藏ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

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

    /// <summary>
    /// 导航属性 - 页面
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Page? Page { get; set; }
}
