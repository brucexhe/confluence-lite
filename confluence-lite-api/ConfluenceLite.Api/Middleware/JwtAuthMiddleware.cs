using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Mappers;

namespace ConfluenceLite.Api.Middleware;

/// <summary>
/// 当前用户上下文
/// </summary>
public class CurrentUser
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
    public bool IsAdmin { get; set; }
}

/// <summary>
/// 未授权错误响应
/// </summary>
public class UnauthorizedResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "未授权，请先登录";
    public int ErrorCode { get; set; } = 401;
}

/// <summary>
/// 禁止访问错误响应
/// </summary>
public class ForbiddenResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "需要管理员权限";
    public int ErrorCode { get; set; } = 403;
}

/// <summary>
/// JWT 认证中间件 - Native AOT 兼容
/// </summary>
public class JwtAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly JwtOptions _jwtOptions;
    private readonly string _issuer;
    private readonly string _audience;

    // 公开路径白名单（无需认证）
    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/config",
        "/api/auth/oidc/login",
        "/api/auth/oidc/callback",
        "/api/user/login",
        "/api/user/register",
        "/api/setup/status",
        "/api/setup/test-connection",
        "/api/setup/install",
        "/api/siteinfo",
        "/api/share/",
        "/health",
        "/alive"
    };

    public JwtAuthMiddleware(RequestDelegate next, JwtOptions jwtOptions, IConfiguration configuration)
    {
        _next = next;
        _jwtOptions = jwtOptions;
        _issuer = configuration["Jwt:Issuer"] ?? "ConfluenceLite";
        _audience = configuration["Jwt:Audience"] ?? "ConfluenceLiteUsers";
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var currentUser = new CurrentUser();

        // 从 Cookie 获取 token
        var token = context.Request.Cookies["Authorization"];
        if (!string.IsNullOrEmpty(token))
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = System.Text.Encoding.UTF8.GetBytes(_jwtOptions.Secret);

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);

                var nameIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                var nameClaim = principal.FindFirst(ClaimTypes.Name);

                if (nameIdClaim != null && long.TryParse(nameIdClaim.Value, out var userId))
                {
                    currentUser.UserId = userId;
                    currentUser.Username = nameClaim?.Value ?? string.Empty;
                    currentUser.IsAuthenticated = true;

                    var isAdminClaim = principal.FindFirst("IsAdmin");
                    currentUser.IsAdmin = isAdminClaim?.Value == "True";
                }
            }
            catch
            {
                // Token 无效，忽略
            }
        }

        // 将当前用户信息存储到 HttpContext 中
        context.Items["CurrentUser"] = currentUser;

        // 检查是否需要认证
        var path = context.Request.Path.Value ?? "";

        // 跳过非 API 路径的认证检查（前端路由、静态文件等）
        // 只有 /api/ 开头的路径才需要认证检查
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var isPublicPath = PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        if (!isPublicPath && !currentUser.IsAuthenticated)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var response = new UnauthorizedResponse();
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, AppJsonContext.Default.UnauthorizedResponse));
            return;
        }

        // 管理接口需要管理员权限
        if (path.StartsWith("/api/system/", StringComparison.OrdinalIgnoreCase)
            && !currentUser.IsAdmin)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            var forbiddenResponse = new ForbiddenResponse();
            await context.Response.WriteAsync(JsonSerializer.Serialize(forbiddenResponse, AppJsonContext.Default.ForbiddenResponse));
            return;
        }

        await _next(context);
    }
}
