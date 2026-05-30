namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 安全配置
/// </summary>
public class SecurityConfigDto
{
    // 用户注册
    public bool AllowPublicRegistration { get; set; } = false;
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

    // JWT Token 配置
    public string JwtSecret { get; set; } = "";
    public string JwtIssuer { get; set; } = "ConfluenceLite";
    public string JwtAudience { get; set; } = "ConfluenceLiteUsers";
    public int JwtExpirationMinutes { get; set; } = 10080;
}
