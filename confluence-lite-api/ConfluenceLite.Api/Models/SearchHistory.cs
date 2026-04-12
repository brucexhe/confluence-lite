using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 搜索历史实体
/// </summary>
[SugarTable("search_history")]
public class SearchHistory
{
    /// <summary>
    /// 记录ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = false)]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 结果数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int ResultCount { get; set; }

    /// <summary>
    /// 搜索时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime SearchedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 导航属性 - 用户
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? User { get; set; }
}
