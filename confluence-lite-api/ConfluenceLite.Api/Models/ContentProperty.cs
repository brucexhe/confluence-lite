using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 内容属性实体 (通用键值对元数据)
/// </summary>
[SugarTable("content_properties")]
public class ContentProperty
{
    /// <summary>
    /// 属性ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 实体类型: 0-页面, 1-附件, 2-评论
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int EntityType { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long EntityId { get; set; }

    /// <summary>
    /// 属性键
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 属性值 (可能为JSON)
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Value { get; set; }

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
