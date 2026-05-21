using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 最近访问记录
/// </summary>
[SugarTable("recents")]
public class Recent
{
    /// <summary>
    /// ID
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
    /// 最后访问时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime VisitedAt { get; set; } = DateTime.Now;
}
