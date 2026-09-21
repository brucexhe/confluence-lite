using ConfluenceLite.Api.Middleware;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ConfluenceLite.Api.Routes;

public static class SearchRoutes
{
    public static void MapSearchRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/search");

        // 搜索建议
        group.MapGet("/suggestions", async (string q, HttpContext context, SearchService searchService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            // 只在自身拥有或加入（有 ViewSpace 权限）的空间内搜索；系统管理员不受限
            var results = await searchService.GetSuggestionsAsync(q, currentUser.UserId, currentUser.IsAdmin);
            return Results.Ok(ApiResponse<List<SearchSuggestionDto>>.Ok(results));
        });

        // 全局搜索
        group.MapGet("/", async (string q, HttpContext context, SearchService searchService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            // 只在自身拥有或加入（有 ViewSpace 权限）的空间内搜索；系统管理员不受限
            var results = await searchService.SearchAllAsync(q, currentUser.UserId, currentUser.IsAdmin);
            return Results.Ok(ApiResponse<List<SearchResultDto>>.Ok(results));
        });
        
        // 确保索引已创建
        group.MapPost("/init-index", async (SearchService searchService) =>
        {
            await searchService.EnsureIndexesAsync();
            return Results.Ok(new ApiResponse<string> { Data = "Index initialized" });
        });
    }
}
