using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;

namespace ConfluenceLite.Api.Routes;

/// <summary>
/// 活动记录路由
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
    }
}