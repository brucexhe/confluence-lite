namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 公开的认证配置（供登录页面使用，无需认证，不包含敏感信息）
/// </summary>
public class PublicAuthConfigDto
{
    /// <summary>
    /// 密码登录是否启用
    /// </summary>
    public bool PasswordEnabled { get; set; } = true;

    /// <summary>
    /// 邮箱登录是否启用
    /// </summary>
    public bool EmailLoginEnabled { get; set; }

    /// <summary>
    /// OpenID Connect 是否启用
    /// </summary>
    public bool OidcEnabled { get; set; }

    /// <summary>
    /// OpenID Connect 提供商名称（显示在登录按钮上）
    /// </summary>
    public string OidcProviderName { get; set; } = "";

    /// <summary>
    /// LDAP 是否启用
    /// </summary>
    public bool LdapEnabled { get; set; }
}
