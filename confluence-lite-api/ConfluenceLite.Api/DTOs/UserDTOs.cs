using System.ComponentModel.DataAnnotations;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 用户登录请求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 用户登录响应
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token类型 (Bearer)
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// 过期时间(秒)
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserDto User { get; set; } = new();

    /// <summary>
    /// 用户的空间列表
    /// </summary>
    public List<WorkspaceSummaryDto> Workspaces { get; set; } = new();
}

/// <summary>
/// 用户创建请求
/// </summary>
public class CreateUserRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [StringLength(100)]
    public string? DisplayName { get; set; }
}

/// <summary>
/// 用户更新请求
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// 邮箱
    /// </summary>
    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [StringLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// 用户状态: 0-禁用, 1-正常
    /// </summary>
    public int? Status { get; set; }
}

/// <summary>
/// 修改密码请求
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// 旧密码
    /// </summary>
    [Required]
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密码
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// 用户DTO
/// </summary>
public class UserDto
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public int Status { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
