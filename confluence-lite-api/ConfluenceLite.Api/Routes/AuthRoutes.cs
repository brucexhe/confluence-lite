using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Data;

namespace ConfluenceLite.Api.Routes;

public static class AuthRoutes
{
    public static void MapAuthRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapGet("/oidc/login", async (
            OidcService oidcService) =>
        {
            var (authUrl, error) = await oidcService.GetAuthorizationUrlAsync();
            if (authUrl == null || error != null)
            {
                return Results.Redirect($"/login?error={Uri.EscapeDataString(error ?? "OIDC 配置错误")}");
            }

            return Results.Redirect(authUrl);
        });

        group.MapGet("/oidc/callback", async (
            string code,
            string? state,
            HttpContext context,
            OidcService oidcService,
            TokenService tokenService,
            WorkspaceService workspaceService) =>
        {
            var (userInfo, error) = await oidcService.HandleCallbackAsync(code, state);
            if (userInfo == null || error != null)
            {
                return Results.Redirect($"/login?error={Uri.EscapeDataString(error ?? "OIDC 认证失败")}");
            }

            var (user, userError) = await oidcService.FindOrCreateUserAsync(userInfo);
            if (user == null || userError != null)
            {
                return Results.Redirect($"/login?error={Uri.EscapeDataString(userError ?? "用户创建失败")}");
            }

            var workspaces = await workspaceService.GetUserWorkspacesAsync(user.Id);

            var token = tokenService.GenerateToken(user.Id, user.Username);

            var response = new LoginResponse
            {
                Token = token,
                TokenType = "Bearer",
                ExpiresIn = 1440,
                User = user,
                Workspaces = workspaces.Select(w => new WorkspaceSummaryDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Key = w.Key,
                    Icon = w.Icon,
                    IsDefault = w.IsDefault
                }).ToList()
            };

            var redirectUrl = context.Request.Query["redirect_url"].FirstOrDefault() ?? "/";

            // 返回 HTML 页面，将登录数据传递给前端
            var html = $"""
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8">
                    <title>登录成功</title>
                </head>
                <body>
                    <script>
                        // 存储登录数据到 localStorage
                        localStorage.setItem('auth_login_response', {System.Text.Json.JsonSerializer.Serialize(response)});
                        // 重定向到目标页面
                        window.location.href = '{redirectUrl}';
                    </script>
                </body>
                </html>
                """;

            return Results.Text(html, "text/html");
        });

        group.MapGet("/config", (AppConfiguration config) =>
        {
            var authConfig = new AuthConfigDto
            {
                PasswordEnabled = config.AuthSettings.PasswordEnabled,
                EmailLoginEnabled = config.AuthSettings.EmailLoginEnabled,
                OidcEnabled = config.AuthSettings.OidcEnabled,
                OidcProviderName = config.AuthSettings.OidcProviderName,
                OidcDiscoveryUrl = config.AuthSettings.OidcDiscoveryUrl,
                OidcClientId = config.AuthSettings.OidcClientId,
                OidcClientSecret = config.AuthSettings.OidcClientSecret,
                OidcScopes = config.AuthSettings.OidcScopes,
                OidcAutoCreateUser = config.AuthSettings.OidcAutoCreateUser,
                OidcDefaultRole = config.AuthSettings.OidcDefaultRole,
                LdapEnabled = config.AuthSettings.LdapEnabled,
                LdapUrl = config.AuthSettings.LdapUrl,
                LdapBindDn = config.AuthSettings.LdapBindDn,
                LdapBindPassword = config.AuthSettings.LdapBindPassword,
                LdapBaseDn = config.AuthSettings.LdapBaseDn,
                LdapUserFilter = config.AuthSettings.LdapUserFilter
            };

            return Results.Ok(ApiResponse<AuthConfigDto>.Ok(authConfig));
        });
    }
}
