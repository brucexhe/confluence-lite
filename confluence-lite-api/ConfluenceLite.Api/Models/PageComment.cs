using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面评论实体
/// </summary>
[SugarTable("page_comments")]
public class PageComment
{
    /// <summary>
    /// 评论ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 所属页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

    /// <summary>
    /// 评论者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = false)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 父评论ID (用于回复)
    /// </summary>
    public long? ParentId { get; set; }

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
    /// 导航属性 - 所属页面
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Page? Page { get; set; }

    /// <summary>
    /// 导航属性 - 评论者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? User { get; set; }
}
