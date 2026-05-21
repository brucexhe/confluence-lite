using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConfluenceLite.Api.Routes;

public static class RecentRoutes
{
    public static void RegisterRoutes(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/recent");

        // 获取最近访问列表
        group.MapGet("/", async (RecentService recentService, HttpContext context) =>
        {
            var currentUser = context.Items["CurrentUser"] as Middleware.CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var recents = await recentService.GetRecentPagesAsync(currentUser.UserId);
            return Results.Ok(ApiResponse<List<RecentDto>>.Ok(recents));
        });

        // 记录访问
        group.MapPost("/{pageId}", async (long pageId, RecentService recentService, HttpContext context) =>
        {
            var currentUser = context.Items["CurrentUser"] as Middleware.CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            await recentService.AddRecentAsync(currentUser.UserId, pageId);
            return Results.Ok(ApiResponse<bool>.Ok(true));
        });
    }
}
