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
        group.MapGet("/suggestions", async (string q, SearchService searchService) =>
        {
            var results = await searchService.GetSuggestionsAsync(q);
            return Results.Ok(ApiResponse<List<SearchSuggestionDto>>.Ok(results));
        });

        // 全局搜索
        group.MapGet("/", async (string q, SearchService searchService) =>
        {
            var results = await searchService.SearchAllAsync(q);
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
