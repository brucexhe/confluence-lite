using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 活动事件实体 (动态流)
/// </summary>
[SugarTable("activity_events")]
public class ActivityEvent
{
    /// <summary>
    /// 事件ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 操作者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long ActorId { get; set; }

    /// <summary>
    /// 事件类型: 0-页面创建, 1-页面更新, 2-评论添加, 3-空间创建, 4-页面删除, 5-附件添加
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int EventType { get; set; }

    /// <summary>
    /// 实体类型: 0-页面, 1-空间, 2-评论, 3-附件
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int EntityType { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long EntityId { get; set; }

    /// <summary>
    /// 工作空间ID
    /// </summary>
    public long? WorkspaceId { get; set; }

    /// <summary>
    /// 事件标题
    /// </summary>
    [SugarColumn(Length = 300)]
    public string? Title { get; set; }

    /// <summary>
    /// 事件摘要
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Summary { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 操作者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Actor { get; set; }

    /// <summary>
    /// 导航属性 - 工作空间
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Workspace? Workspace { get; set; }
}
