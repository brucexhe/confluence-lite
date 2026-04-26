using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;
using ConfluenceLite.Api.Services;

namespace ConfluenceLite.Api.Routes;

/// <summary>
/// 系统路由
/// </summary>
public static class SystemRoutes
{
    public static void MapSystemRoutes(this WebApplication app)
    {
        // ========== 健康检查 ==========
        app.MapGet("/health", () =>
        {
            return Results.Json(new HealthResponse(), AppJsonContext.Default.HealthResponse);
        });

        var group = app.MapGroup("/api/system")
            .WithTags("System");

        // ========== Office 预览配置 ==========
        group.MapGet("/office-preview-config", (AppConfiguration appConfig) =>
        {
            var dto = new OfficePreviewConfigDto
            {
                Enabled = appConfig.Gotenberg.Enabled,
                BaseUrl = appConfig.Gotenberg.BaseUrl,
                TimeoutSeconds = appConfig.Gotenberg.TimeoutSeconds
            };
            return Results.Ok(ApiResponse<OfficePreviewConfigDto>.Ok(dto));
        });

        group.MapPut("/office-preview-config", (
            OfficePreviewConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env) =>
        {
            // 更新内存中的配置（单例）
            appConfig.Gotenberg.Enabled = dto.Enabled;
            appConfig.Gotenberg.BaseUrl = dto.BaseUrl;
            appConfig.Gotenberg.TimeoutSeconds = dto.TimeoutSeconds;

            // 持久化到 appsettings.runtime.json
            SaveGotenbergConfig(env, dto);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        group.MapPost("/office-preview-config/test", async (
            OfficePreviewConfigDto dto,
            IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(dto.BaseUrl))
                return Results.Ok(ApiResponse<bool>.Fail("请先配置服务地址"));

            try
            {
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var baseUrl = dto.BaseUrl.TrimEnd('/');
                var response = await client.GetAsync($"{baseUrl}/health");
                if (response.IsSuccessStatusCode)
                    return Results.Ok(ApiResponse<bool>.Ok(true, "连接成功"));
                return Results.Ok(ApiResponse<bool>.Fail($"连接失败 (HTTP {(int)response.StatusCode})"));
            }
            catch (Exception ex)
            {
                return Results.Ok(ApiResponse<bool>.Fail($"连接失败: {ex.Message}"));
            }
        });

        // ========== 站点配置 ==========

        group.MapGet("/site-config", (AppConfiguration appConfig) =>
        {
            var dto = new SiteConfigDto
            {
                SiteName = appConfig.SiteSettings.SiteName,
                SiteDescription = appConfig.SiteSettings.SiteDescription,
                SiteLogo = appConfig.SiteSettings.SiteLogo,
                SiteDomain = appConfig.SiteSettings.SiteDomain,
                DefaultLanguage = appConfig.SiteSettings.DefaultLanguage,
                DefaultHomePage = appConfig.SiteSettings.DefaultHomePage,
                Timezone = appConfig.SiteSettings.Timezone,
                AllowRegistration = appConfig.SiteSettings.AllowRegistration
            };
            return Results.Ok(ApiResponse<SiteConfigDto>.Ok(dto));
        });

        group.MapPut("/site-config", (
            SiteConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env) =>
        {
            appConfig.SiteSettings.SiteName = dto.SiteName;
            appConfig.SiteSettings.SiteDescription = dto.SiteDescription;
            appConfig.SiteSettings.SiteLogo = dto.SiteLogo;
            appConfig.SiteSettings.SiteDomain = dto.SiteDomain;
            appConfig.SiteSettings.DefaultLanguage = dto.DefaultLanguage;
            appConfig.SiteSettings.DefaultHomePage = dto.DefaultHomePage;
            appConfig.SiteSettings.Timezone = dto.Timezone;
            appConfig.SiteSettings.AllowRegistration = dto.AllowRegistration;

            SaveSiteSettingsConfig(env, dto);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        // ========== 显示配置 ==========

        group.MapGet("/display-config", (AppConfiguration appConfig) =>
        {
            var dto = new DisplayConfigDto
            {
                DefaultTheme = appConfig.DisplaySettings.DefaultTheme,
                PrimaryColor = appConfig.DisplaySettings.PrimaryColor,
                CompactMode = appConfig.DisplaySettings.CompactMode,
                PageSize = appConfig.DisplaySettings.PageSize,
                PageTreeExpandMode = appConfig.DisplaySettings.PageTreeExpandMode,
                ShowPageViews = appConfig.DisplaySettings.ShowPageViews,
                ShowAuthorInfo = appConfig.DisplaySettings.ShowAuthorInfo,
                ShowLastModified = appConfig.DisplaySettings.ShowLastModified,
                DefaultEditorMode = appConfig.DisplaySettings.DefaultEditorMode,
                AutoSaveInterval = appConfig.DisplaySettings.AutoSaveInterval,
                EnableSpellCheck = appConfig.DisplaySettings.EnableSpellCheck,
                DefaultSidebarWidth = appConfig.DisplaySettings.DefaultSidebarWidth,
                ShowSpaceIcon = appConfig.DisplaySettings.ShowSpaceIcon,
                AllowCollapseSidebar = appConfig.DisplaySettings.AllowCollapseSidebar
            };
            return Results.Ok(ApiResponse<DisplayConfigDto>.Ok(dto));
        });

        group.MapPut("/display-config", (
            DisplayConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env) =>
        {
            appConfig.DisplaySettings.DefaultTheme = dto.DefaultTheme;
            appConfig.DisplaySettings.PrimaryColor = dto.PrimaryColor;
            appConfig.DisplaySettings.CompactMode = dto.CompactMode;
            appConfig.DisplaySettings.PageSize = dto.PageSize;
            appConfig.DisplaySettings.PageTreeExpandMode = dto.PageTreeExpandMode;
            appConfig.DisplaySettings.ShowPageViews = dto.ShowPageViews;
            appConfig.DisplaySettings.ShowAuthorInfo = dto.ShowAuthorInfo;
            appConfig.DisplaySettings.ShowLastModified = dto.ShowLastModified;
            appConfig.DisplaySettings.DefaultEditorMode = dto.DefaultEditorMode;
            appConfig.DisplaySettings.AutoSaveInterval = dto.AutoSaveInterval;
            appConfig.DisplaySettings.EnableSpellCheck = dto.EnableSpellCheck;
            appConfig.DisplaySettings.DefaultSidebarWidth = dto.DefaultSidebarWidth;
            appConfig.DisplaySettings.ShowSpaceIcon = dto.ShowSpaceIcon;
            appConfig.DisplaySettings.AllowCollapseSidebar = dto.AllowCollapseSidebar;

            SaveDisplaySettingsConfig(env, dto);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        // ========== 公开端点（无需认证） ==========

        app.MapGet("/api/siteinfo", (AppConfiguration appConfig, SetupService setupService) =>
        {
            var dto = new SiteInfoDto
            {
                Installed = setupService.IsInstalled(),
                SiteName = appConfig.SiteSettings.SiteName,
                SiteLogo = appConfig.SiteSettings.SiteLogo,
                AllowRegistration = appConfig.SiteSettings.AllowRegistration
            };
            return Results.Ok(ApiResponse<SiteInfoDto>.Ok(dto));
        });
    }

    /// <summary>
    /// 保存 Gotenberg 配置到 appsettings.runtime.json（AOT 兼容）
    /// </summary>
    private static void SaveGotenbergConfig(IHostEnvironment env, OfficePreviewConfigDto dto)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        // 读取现有配置
        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        // 确保 App.Gotenberg 路径存在
        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        var gotenbergNode = new JsonObject
        {
            ["Enabled"] = dto.Enabled,
            ["BaseUrl"] = dto.BaseUrl,
            ["TimeoutSeconds"] = dto.TimeoutSeconds
        };

        appNode!["Gotenberg"] = gotenbergNode;
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    private static void SaveSiteSettingsConfig(IHostEnvironment env, SiteConfigDto dto)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        appNode!["SiteSettings"] = new JsonObject
        {
            ["SiteName"] = dto.SiteName,
            ["SiteDescription"] = dto.SiteDescription,
            ["SiteLogo"] = dto.SiteLogo,
            ["SiteDomain"] = dto.SiteDomain,
            ["DefaultLanguage"] = dto.DefaultLanguage,
            ["DefaultHomePage"] = dto.DefaultHomePage,
            ["Timezone"] = dto.Timezone,
            ["AllowRegistration"] = dto.AllowRegistration
        };
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    private static void SaveDisplaySettingsConfig(IHostEnvironment env, DisplayConfigDto dto)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        appNode!["DisplaySettings"] = new JsonObject
        {
            ["DefaultTheme"] = dto.DefaultTheme,
            ["PrimaryColor"] = dto.PrimaryColor,
            ["CompactMode"] = dto.CompactMode,
            ["PageSize"] = dto.PageSize,
            ["PageTreeExpandMode"] = dto.PageTreeExpandMode,
            ["ShowPageViews"] = dto.ShowPageViews,
            ["ShowAuthorInfo"] = dto.ShowAuthorInfo,
            ["ShowLastModified"] = dto.ShowLastModified,
            ["DefaultEditorMode"] = dto.DefaultEditorMode,
            ["AutoSaveInterval"] = dto.AutoSaveInterval,
            ["EnableSpellCheck"] = dto.EnableSpellCheck,
            ["DefaultSidebarWidth"] = dto.DefaultSidebarWidth,
            ["ShowSpaceIcon"] = dto.ShowSpaceIcon,
            ["AllowCollapseSidebar"] = dto.AllowCollapseSidebar
        };
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }
}
