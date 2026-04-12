using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 附件实体
/// </summary>
[SugarTable("attachments")]
public class Attachment
{
    /// <summary>
    /// 附件ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 所属页面ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long PageId { get; set; }

    /// <summary>
    /// 原始文件名
    /// </summary>
    [SugarColumn(Length = 255, IsNullable = false)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小 (字节)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long FileSize { get; set; }

    /// <summary>
    /// MIME类型
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// 存储路径
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = false)]
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件哈希 (SHA-256, 用于去重)
    /// </summary>
    [SugarColumn(Length = 64)]
    public string? FileHash { get; set; }

    /// <summary>
    /// 上传者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatorId { get; set; }

    /// <summary>
    /// 附件说明
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Comment { get; set; }

    /// <summary>
    /// 版本号 (同一附件更新上传时递增)
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Version { get; set; } = 1;

    /// <summary>
    /// 是否已删除
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsDeleted { get; set; }

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

    /// <summary>
    /// 导航属性 - 页面
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Page? Page { get; set; }

    /// <summary>
    /// 导航属性 - 上传者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Creator { get; set; }
}
