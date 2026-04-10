using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ConfluenceLite.Api.Data;

namespace ConfluenceLite.Api.Middleware;

/// <summary>
/// 当前用户上下文
/// </summary>
public class CurrentUser
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool IsAuthenticated { get; set; }
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

        // 从 Authorization header 获取 token
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Convert.FromBase64String(_jwtOptions.Secret);

                var validationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
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
                }
            }
            catch
            {
                // Token 无效，忽略
            }
        }

        // 将当前用户信息存储到 HttpContext 中
        context.Items["CurrentUser"] = currentUser;

        await _next(context);
    }
}
