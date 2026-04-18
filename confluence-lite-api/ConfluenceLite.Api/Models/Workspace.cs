using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 工作空间实体
/// </summary>
[SugarTable("workspaces")]
public class Workspace
{
    /// <summary>
    /// 工作空间ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 工作空间名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作空间描述
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? Description { get; set; }

    /// <summary>
    /// 工作空间标识 (唯一键)
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 所有者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long OwnerId { get; set; }

    /// <summary>
    /// 工作空间状态: 0-禁用, 1-正常
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Status { get; set; } = 1;

    /// <summary>
    /// 图标
    /// </summary>
    [SugarColumn(Length = 50)]
    public string? Icon { get; set; }

    /// <summary>
    /// 主题色 (十六进制颜色码)
    /// </summary>
    [SugarColumn(Length = 7)]
    public string? Color { get; set; }

    /// <summary>
    /// 空间主页ID
    /// </summary>
    public long? HomePageId { get; set; }

    /// <summary>
    /// 是否个人空间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsPersonal { get; set; }

    /// <summary>
    /// 是否为用户的默认空间（每个用户只能有一个默认空间）
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 是否已删除
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }

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
    /// 导航属性 - 所有者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? Owner { get; set; }
}
