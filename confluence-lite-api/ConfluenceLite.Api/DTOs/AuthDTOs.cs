using System.Text.Json.Serialization;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// OIDC 登录请求
/// </summary>
public class OidcLoginRequest
{
    /// <summary>
    /// 登录后的回调地址
    /// </summary>
    public string? RedirectUrl { get; set; }
}

/// <summary>
/// OIDC 回调请求
/// </summary>
public class OidcCallbackRequest
{
    /// <summary>
    /// 授权码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 状态参数
    /// </summary>
    public string? State { get; set; }
}

/// <summary>
/// OIDC 用户信息
/// </summary>
public class OidcUserInfo
{
    /// <summary>
    /// 用户唯一标识
    /// </summary>
    [JsonPropertyName("sub")]
    public string Sub { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    [JsonPropertyName("preferred_username")]
    public string? PreferredUsername { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// 邮箱是否已验证
    /// </summary>
    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 名字
    /// </summary>
    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    /// <summary>
    /// 姓氏
    /// </summary>
    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }

    /// <summary>
    /// 用户组
    /// </summary>
    [JsonPropertyName("groups")]
    public List<string>? Groups { get; set; }
}
