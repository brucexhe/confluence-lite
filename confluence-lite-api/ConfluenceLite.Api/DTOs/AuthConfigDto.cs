namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 身份验证配置
/// </summary>
public class AuthConfigDto
{
    // 密码认证
    public bool PasswordEnabled { get; set; } = true;
    public bool EmailLoginEnabled { get; set; } = false;

    // OpenID Connect
    public bool OidcEnabled { get; set; }
    public string OidcProviderName { get; set; } = "";
    public string OidcDiscoveryUrl { get; set; } = "";
    public string OidcClientId { get; set; } = "";
    public string OidcClientSecret { get; set; } = "";
    public string OidcScopes { get; set; } = "openid profile email";
    public bool OidcAutoCreateUser { get; set; } = true;
    public string OidcDefaultRole { get; set; } = "user";

    // LDAP
    public bool LdapEnabled { get; set; }
    public string LdapUrl { get; set; } = "";
    public string LdapBindDn { get; set; } = "";
    public string LdapBindPassword { get; set; } = "";
    public string LdapBaseDn { get; set; } = "";
    public string LdapUserFilter { get; set; } = "(uid={username})";
}
