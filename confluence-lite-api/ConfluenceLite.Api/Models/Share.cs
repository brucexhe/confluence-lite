using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 页面分享实体
/// </summary>
[SugarTable("shares")]
public class Share
{
    /// <summary>
    /// 分享ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

  /// <summary>
    /// 分享码
    /// </summary>
    [SugarColumn(Length = 50)]
    public string? Code { get; set; }

    /// <summary>
    /// 页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

    /// <summary>
    /// 分享者用户ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public long SharedById { get; set; }

    /// <summary>
    /// 接收者用户ID (null = 匿名分享)
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public long? SharedWithId { get; set; }

    /// <summary>
    /// 访问密码
    /// </summary>
    [SugarColumn(Length = 50)]
    public string? VisitPassword { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsRead { get; set; }

    /// <summary>
    /// 是否可编辑
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public bool AllowEdit { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;


    /// <summary>
    /// 过期时间 (null = 永不过期)
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? ExpireAt { get; set; }

    /// <summary>
    /// 导航属性 - 页面
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Page? Page { get; set; }

    /// <summary>
    /// 导航属性 - 分享者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? SharedBy { get; set; }

    /// <summary>
    /// 导航属性 - 接收者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? SharedWith { get; set; }
}
