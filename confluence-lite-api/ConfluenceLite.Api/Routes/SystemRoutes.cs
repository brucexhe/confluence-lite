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
        var appNode = root.TryGetPropertyValue("App", out var appVal)
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
}
