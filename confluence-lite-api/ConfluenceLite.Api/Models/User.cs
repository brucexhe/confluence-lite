using SqlSugar;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 用户实体
/// </summary>
[SugarTable("users")]
public class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [SugarColumn(Length = 100)]
    public string? Email { get; set; }

    /// <summary>
    /// 密码哈希
    /// </summary>
    [SugarColumn(Length = 255, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    [SugarColumn(Length = 100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 用户状态: 0-禁用, 1-正常
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int Status { get; set; } = 1;

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
}
