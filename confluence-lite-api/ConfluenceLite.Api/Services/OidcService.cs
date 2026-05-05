using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;
using ConfluenceLite.Api.Models;
using SqlSugar;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// OIDC 认证服务 - Native AOT 兼容
/// </summary>
public class OidcService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OidcService(
        IHttpClientFactory httpClientFactory,
        AppConfiguration config,
        AppDbContext db,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _config = config;
        _db = db;
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// 获取授权 URL
    /// </summary>
    public async Task<(string? url, string? error)> GetAuthorizationUrlAsync()
    {
        if (!_config.AuthSettings.OidcEnabled)
        {
            return (null, "OIDC 认证未启用");
        }

        if (string.IsNullOrEmpty(_config.AuthSettings.OidcDiscoveryUrl))
        {
            return (null, "OIDC Discovery URL 未配置");
        }

        try
        {
            var (config, configError) = await GetDiscoveryConfigAsync();
            if (config == null || configError != null)
            {
                return (null, configError ?? "无法获取 OIDC 配置");
            }

            var state = GenerateState();
            var codeVerifier = GenerateCodeVerifier();
            var codeChallenge = GenerateCodeChallenge(codeVerifier);
            var callbackUrl = GetCallbackUrl();

            var authUrl = $"{config.AuthorizationEndpoint}?" +
                $"client_id={Uri.EscapeDataString(_config.AuthSettings.OidcClientId)}&" +
                $"response_type=code&" +
                $"scope={Uri.EscapeDataString(_config.AuthSettings.OidcScopes)}&" +
                $"redirect_uri={Uri.EscapeDataString(callbackUrl)}&" +
                $"state={Uri.EscapeDataString(state)}&" +
                $"code_challenge={Uri.EscapeDataString(codeChallenge)}&" +
                $"code_challenge_method=S256";

            Console.WriteLine($"[OIDC] Callback URL: {callbackUrl}");

            return (authUrl, null);
        }
        catch (Exception ex)
        {
            return (null, $"获取授权 URL 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理回调，交换 token 并获取用户信息
    /// </summary>
    public async Task<(OidcUserInfo? userInfo, string? error)> HandleCallbackAsync(string code, string? state)
    {
        if (!_config.AuthSettings.OidcEnabled)
        {
            return (null, "OIDC 认证未启用");
        }

        try
        {
            var (config, configError) = await GetDiscoveryConfigAsync();
            if (config == null || configError != null)
            {
                return (null, configError ?? "无法获取 OIDC 配置");
            }

            var tokenResponse = await ExchangeCodeForTokenAsync(code, config.TokenEndpoint);
            if (tokenResponse == null)
            {
                return (null, "交换 Token 失败");
            }

            var userInfo = await GetUserInfoAsync(tokenResponse.IdToken, config.UserInfoEndpoint);
            if (userInfo == null)
            {
                return (null, "获取用户信息失败");
            }

            return (userInfo, null);
        }
        catch (Exception ex)
        {
            return (null, $"处理回调失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 根据 OIDC 用户信息查找或创建用户
    /// </summary>
    public async Task<(UserDto? user, string? error)> FindOrCreateUserAsync(OidcUserInfo userInfo)
    {
        var username = userInfo.PreferredUsername ?? userInfo.Email?.Split('@')[0] ?? $"oidc_{userInfo.Sub[..8]}";

        var user = await _db.Db.Queryable<User>()
            .Where(u => u.Username == username || (userInfo.Email != null && u.Email == userInfo.Email))
            .FirstAsync();

        if (user != null)
        {
            if (user.Status == 0)
            {
                return (null, "用户已被禁用");
            }

            user.UpdatedAt = DateTime.Now;
            await _db.Db.Updateable(user).ExecuteCommandAsync();

            return (MapToDto(user), null);
        }

        if (!_config.AuthSettings.OidcAutoCreateUser)
        {
            return (null, "用户不存在且未启用自动创建");
        }

        var newUser = new User
        {
            Username = username,
            Email = userInfo.Email,
            DisplayName = userInfo.Name ?? userInfo.GivenName ?? username,
            Status = 1,
            IsAdmin = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        int userId;
        try
        {
            userId = await _db.Db.Insertable(newUser).ExecuteReturnIdentityAsync();
        }
        catch (Exception ex)
        {
            return (null, $"创建用户失败: {ex.Message}");
        }

        newUser.Id = userId;
        return (MapToDto(newUser), null);
    }

    private async Task<(OidcDiscoveryConfig? config, string? error)> GetDiscoveryConfigAsync()
    {
        try
        {
            Console.WriteLine($"[OIDC] Fetching discovery config from: {_config.AuthSettings.OidcDiscoveryUrl}");
            var response = await _httpClient.GetAsync(_config.AuthSettings.OidcDiscoveryUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var config = JsonSerializer.Deserialize(json, AppJsonContext.Default.OidcDiscoveryConfig);

            if (config == null)
            {
                return (null, "解析 OIDC 配置失败");
            }

            Console.WriteLine($"[OIDC] Discovery config fetched successfully");
            Console.WriteLine($"[OIDC] AuthorizationEndpoint: {config.AuthorizationEndpoint}");
            Console.WriteLine($"[OIDC] TokenEndpoint: {config.TokenEndpoint}");

            return (config, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OIDC] Failed to fetch discovery config: {ex.Message}");
            return (null, $"获取 OIDC 配置失败: {ex.Message}");
        }
    }

    private async Task<OidcTokenResponse?> ExchangeCodeForTokenAsync(string code, string tokenEndpoint)
    {
        try
        {
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = GetCallbackUrl(),
                ["client_id"] = _config.AuthSettings.OidcClientId,
                ["client_secret"] = _config.AuthSettings.OidcClientSecret
            };

            var response = await _httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(parameters));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.OidcTokenResponse);
        }
        catch
        {
            return null;
        }
    }

    private async Task<OidcUserInfo?> GetUserInfoAsync(string idToken, string userInfoEndpoint)
    {
        try
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {idToken}");

            var response = await _httpClient.GetAsync(userInfoEndpoint);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OidcUserInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    private string GetCallbackUrl()
    {
        var domain = _config.SiteSettings.SiteDomain;
        if (!string.IsNullOrEmpty(domain))
        {
            // 如果配置的域名不包含 scheme，默认使用 https
            var fullDomain = domain.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                             domain.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? domain
                : $"https://{domain}";
            return $"{fullDomain}/api/auth/oidc/callback";
        }

        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            var request = context.Request;
            var scheme = request.Scheme;
            var host = request.Host.Value;
            var pathBase = request.PathBase.Value;
            return $"{scheme}://{host}{pathBase}/api/auth/oidc/callback";
        }

        return $"/api/auth/oidc/callback";
    }

    private static string GenerateState()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hash = sha256.ComputeHash(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Status = user.Status,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}

/// <summary>
/// OIDC 发现配置
/// </summary>
public class OidcDiscoveryConfig
{
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("userinfo_endpoint")]
    public string UserInfoEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; } = string.Empty;
}

/// <summary>
/// OIDC Token 响应
/// </summary>
public class OidcTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}
