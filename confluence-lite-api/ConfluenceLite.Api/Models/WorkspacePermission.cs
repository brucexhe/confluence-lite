using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 工作空间权限实体
/// </summary>
[SugarTable("workspace_permissions")]
public class WorkspacePermission
{
    /// <summary>
    /// 权限ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 工作空间ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long WorkspaceId { get; set; }

    /// <summary>
    /// 目标类型: 0-匿名, 1-用户组, 2-用户
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int TargetType { get; set; }

    /// <summary>
    /// 目标ID (用户组ID或用户ID, 匿名类型为null)
    /// </summary>
    public long? TargetId { get; set; }

    /// <summary>
    /// 查看空间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool ViewSpace { get; set; }

    /// <summary>
    /// 创建页面
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool CreatePage { get; set; }

    /// <summary>
    /// 删除任意页面
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool DeletePage { get; set; }

    /// <summary>
    /// 删除自己的页面
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool DeleteOwnPage { get; set; }

    /// <summary>
    /// 编辑页面
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool EditPage { get; set; }

    /// <summary>
    /// 导出页面
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool ExportPage { get; set; }

    /// <summary>
    /// 添加评论
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool AddComment { get; set; }

    /// <summary>
    /// 删除评论
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool DeleteComment { get; set; }

    /// <summary>
    /// 空间管理
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool AdminSpace { get; set; }

    /// <summary>
    /// 设置权限
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool SetPermissions { get; set; }

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
    /// 导航属性 - 工作空间
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public Workspace? Workspace { get; set; }
}
