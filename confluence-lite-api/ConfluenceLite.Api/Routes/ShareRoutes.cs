using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Routes;

public static class ShareRoutes
{
    public static void MapShareRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/share").WithTags("Shares");

        // 创建分享 (需要认证)
        group.MapPost("/", async (
            CreateShareRequest request,
            HttpContext context,
            ShareService shareService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (share, error) = await shareService.CreateShareAsync(currentUser.UserId, request);
            if (share == null || error != null)
                return Results.BadRequest(ApiResponse<ShareDto>.Fail(error ?? "创建分享失败"));

            return Results.Ok(ApiResponse<ShareDto>.Ok(share, "分享创建成功"));
        });

        // 获取页面的分享列表 (需要认证) - 必须在 /{code} 之前注册
        group.MapGet("/page/{pageId}/list", async (
            long pageId,
            HttpContext context,
            ShareService shareService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var shares = await shareService.GetSharesByPageAsync(pageId, currentUser.UserId);
            return Results.Ok(ApiResponse<List<ShareDto>>.Ok(shares));
        });

        // 获取公开分享信息 (无需认证, 按 code)
        group.MapGet("/{code}", async (string code, ShareService shareService) =>
        {
            var info = await shareService.GetPublicShareInfoAsync(code);
            if (info == null)
                return Results.NotFound(ApiResponse<PublicShareInfoDto>.Fail("分享不存在"));
            return Results.Ok(ApiResponse<PublicShareInfoDto>.Ok(info));
        });

        // 获取分享的页面内容 (无需认证, 按 code)
        group.MapGet("/{code}/page", async (
            string code,
            string? password,
            HttpContext context,
            ShareService shareService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            var (page, error) = await shareService.GetSharePageContentAsync(code, password, currentUser);
            if (page == null || error != null)
                return Results.BadRequest(ApiResponse<PageDto>.Fail(error ?? "获取页面失败"));
            return Results.Ok(ApiResponse<PageDto>.Ok(page));
        });

        // 通过分享更新页面内容 (无需认证, 按 code)
        group.MapPut("/{code}/page", async (
            string code,
            string? password,
            UpdateSharePageRequest request,
            HttpContext context,
            ShareService shareService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            var (page, error) = await shareService.UpdatePageViaShareAsync(code, password, currentUser, request);
            if (page == null || error != null)
                return Results.BadRequest(ApiResponse<PageDto>.Fail(error ?? "更新页面失败"));
            return Results.Ok(ApiResponse<PageDto>.Ok(page, "更新成功"));
        });

        // 删除分享 (需要认证, 按 id)
        group.MapDelete("/{id}", async (
            long id,
            HttpContext context,
            ShareService shareService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await shareService.DeleteShareAsync(id, currentUser.UserId);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "删除分享失败"));
            return Results.Ok(ApiResponse<bool>.Ok(true, "分享已删除"));
        });
    }
}
