using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 草稿实体 (自动保存)
/// </summary>
[SugarTable("drafts")]
public class Draft
{
    /// <summary>
    /// 草稿ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 标题
    /// </summary>
    [SugarColumn(Length = 200)]
    public string? Title { get; set; }

    /// <summary>
    /// 内容
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Content { get; set; }

    /// <summary>
    /// 关联页面ID (null表示新页面草稿)
    /// </summary>
    public long? PageId { get; set; }

    /// <summary>
    /// 目标工作空间ID
    /// </summary>
    public long? WorkspaceId { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatorId { get; set; }

    /// <summary>
    /// 父页面ID (新建子页面时使用)
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 草稿类型: 0-新建页面, 1-编辑已有页面
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int DraftType { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间 (跟踪最后自动保存)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 导航属性 - 关联页面
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Page? Page { get; set; }

    /// <summary>
    /// 导航属性 - 创建者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Creator { get; set; }
}
