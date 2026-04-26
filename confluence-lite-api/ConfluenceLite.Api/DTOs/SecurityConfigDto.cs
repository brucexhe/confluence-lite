namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 安全配置
/// </summary>
public class SecurityConfigDto
{
    // 用户注册
    public bool AllowPublicRegistration { get; set; } = true;
    public bool RequireEmailVerification { get; set; }
    public string DefaultUserRole { get; set; } = "user";

    // 密码策略
    public int MinPasswordLength { get; set; } = 8;
    public string PasswordComplexity { get; set; } = "medium";
    public int PasswordExpireDays { get; set; }

    // 会话管理
    public int SessionTimeout { get; set; } = 60;
    public bool AllowConcurrentSessions { get; set; } = true;
    public bool AllowRememberMe { get; set; } = true;

    // 访问控制
    public string IpWhitelist { get; set; } = "";
    public bool EnableTwoFactor { get; set; }
}
