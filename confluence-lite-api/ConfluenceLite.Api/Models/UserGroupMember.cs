using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 用户组成员实体
/// </summary>
[SugarTable("user_group_members")]
public class UserGroupMember
{
    /// <summary>
    /// 记录ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户组ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long GroupId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 角色: 0-成员, 1-组长
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Role { get; set; }

    /// <summary>
    /// 加入时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime JoinedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 导航属性 - 用户组
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public UserGroup? Group { get; set; }

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? User { get; set; }
}
