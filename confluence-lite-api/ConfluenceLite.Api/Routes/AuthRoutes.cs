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

            var redirectUrl = context.Request.Query["redirect_url"].FirstOrDefault() ?? "/";

            return Results.Redirect($"{redirectUrl}#token={Uri.EscapeDataString(token)}");
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
