using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// @提及实体
/// </summary>
[SugarTable("mentions")]
public class Mention
{
    /// <summary>
    /// 提及ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 被提及用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long MentionedUserId { get; set; }

    /// <summary>
    /// 来源类型: 0-页面, 1-评论
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int SourceType { get; set; }

    /// <summary>
    /// 来源ID (页面ID或评论ID)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long SourceId { get; set; }

    /// <summary>
    /// 提及者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long MentioningUserId { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 导航属性 - 被提及用户
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? MentionedUser { get; set; }

    /// <summary>
    /// 导航属性 - 提及者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? MentioningUser { get; set; }
}
