using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 系统备份实体
/// </summary>
[SugarTable("system_backups")]
public class SystemBackup
{
    /// <summary>
    /// 备份ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 备份名称
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 备份描述
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? Description { get; set; }

    /// <summary>
    /// 备份类型: full, database, files, config
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Type { get; set; } = "full";

    /// <summary>
    /// 备份选项 (JSON)
    /// </summary>
    [SugarColumn(ColumnDataType = "jsonb")]
    public string? Options { get; set; }

    /// <summary>
    /// 备份文件路径
    /// </summary>
    [SugarColumn(Length = 500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 备份状态: pending, processing, completed, failed
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 错误消息
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建者ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatedById { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 导航属性 - 创建者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? CreatedBy { get; set; }
}
