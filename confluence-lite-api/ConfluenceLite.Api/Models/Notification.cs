using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 通知实体
/// </summary>
[SugarTable("notifications")]
public class Notification
{
    /// <summary>
    /// 通知ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 接收者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long RecipientId { get; set; }

    /// <summary>
    /// 触发者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long ActorId { get; set; }

    /// <summary>
    /// 事件类型: 0-页面创建, 1-页面更新, 2-评论添加, 3-提及, 4-分享, 5-页面删除
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int EventType { get; set; }

    /// <summary>
    /// 实体类型: 0-页面, 1-空间, 2-评论
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int EntityType { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long EntityId { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [SugarColumn(Length = 300)]
    public string? Title { get; set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Message { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsRead { get; set; }

    /// <summary>
    /// 已读时间
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 导航属性 - 接收者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Recipient { get; set; }

    /// <summary>
    /// 导航属性 - 触发者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Actor { get; set; }
}
