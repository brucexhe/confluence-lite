using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Routes;

public static class PageRoutes
{
    public static void MapPageRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/page")
            .WithTags("Pages");

        // 创建页面
        group.MapPost("/", async (
            CreatePageRequest request,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (page, error) = await pageService.CreatePageAsync(currentUser.UserId, request);
            if (page == null || error != null)
                return Results.BadRequest(ApiResponse<PageDto>.Fail(error ?? "创建页面失败"));

            return Results.Ok(ApiResponse<PageDto>.Ok(page, "页面创建成功"));
        });

        // 获取页面详情
        group.MapGet("/{id}", async (long id, PageService pageService) =>
        {
            var page = await pageService.GetPageByIdAsync(id);
            if (page == null)
                return Results.NotFound(ApiResponse<PageDto>.Fail("页面不存在"));
            return Results.Ok(ApiResponse<PageDto>.Ok(page));
        });

        // 获取所有页面列表（管理后台）
        group.MapGet("/all", async (
            int page,
            int pageSize,
            string? search,
            long? workspaceId,
            int? status,
            PageService pageService) =>
        {
            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var result = await pageService.GetAllPagesAsync(pagedRequest, search, workspaceId, status);
            return Results.Ok(ApiResponse<PagedResponse<PageDto>>.Ok(result));
        });

        // 获取工作空间的页面列表
        group.MapGet("/workspace/{workspaceId}", async (
            long workspaceId,
            int page,
            int pageSize,
            PageService pageService) =>
        {
            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var result = await pageService.GetPagesByWorkspaceAsync(workspaceId, pagedRequest);
            return Results.Ok(ApiResponse<PagedResponse<PageDto>>.Ok(result));
        });

        // 获取页面树
        group.MapGet("/workspace/{workspaceId}/tree", async (
            long workspaceId,
            PageService pageService) =>
        {
            var tree = await pageService.GetPageTreeAsync(workspaceId);
            return Results.Ok(ApiResponse<List<PageTreeNodeDto>>.Ok(tree));
        });

        // 获取子页面
        group.MapGet("/{parentId}/children", async (
            long parentId,
            PageService pageService) =>
        {
            var children = await pageService.GetChildPagesAsync(parentId);
            return Results.Ok(ApiResponse<List<PageDto>>.Ok(children));
        });

        // 批量更新页面排序和层级
        group.MapPut("/batch-sort", async (
            BatchSortPageRequest request,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await pageService.BatchSortPagesAsync(request);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "批量排序失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "页面排序已更新"));
        });

        // 移动页面（即时保存，不产生版本快照）
        group.MapPut("/{id}/move", async (
            long id,
            MovePageRequest request,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await pageService.MovePageAsync(id, request);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "移动页面失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "页面已移动"));
        });

        // 更新页面
        group.MapPut("/{id}", async (
            long id,
            UpdatePageRequest request,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (page, error) = await pageService.UpdatePageAsync(id, currentUser.UserId, request);
            if (page == null || error != null)
                return Results.BadRequest(ApiResponse<PageDto>.Fail(error ?? "更新页面失败"));

            return Results.Ok(ApiResponse<PageDto>.Ok(page, "更新成功"));
        });

        // 删除页面
        group.MapDelete("/{id}", async (
            long id,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await pageService.DeletePageAsync(id, currentUser.UserId);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除页面失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "页面已删除"));
        });

        // ========== 版本历史 ==========
        group.MapGet("/{pageId}/versions", async (
            long pageId,
            PageService pageService) =>
        {
            var versions = await pageService.GetPageVersionsAsync(pageId);
            return Results.Ok(ApiResponse<List<PageVersionListDto>>.Ok(versions));
        });

        group.MapGet("/versions/{versionId}", async (
            long versionId,
            PageService pageService) =>
        {
            var version = await pageService.GetPageVersionAsync(versionId);
            if (version == null)
                return Results.NotFound(ApiResponse<PageVersionDto>.Fail("版本不存在"));
            return Results.Ok(ApiResponse<PageVersionDto>.Ok(version));
        });

        group.MapDelete("/versions/{versionId}", async (
            long versionId,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await pageService.DeletePageVersionAsync(versionId);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除版本失败"));
            return Results.Ok(ApiResponse<bool>.Ok(true, "版本已删除"));
        });

        // ========== 评论 ==========
        group.MapGet("/{pageId}/comments", async (long pageId, CommentService commentService) =>
        {
            var comments = await commentService.GetCommentsByPageAsync(pageId);
            return Results.Ok(ApiResponse<List<CommentDto>>.Ok(comments));
        });

        group.MapPost("/{pageId}/comments", async (
            long pageId,
            CreateCommentRequest request,
            HttpContext context,
            CommentService commentService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            request.PageId = pageId;

            var (comment, error) = await commentService.CreateCommentAsync(currentUser.UserId, request);
            if (comment == null || error != null)
                return Results.BadRequest(ApiResponse<CommentDto>.Fail(error ?? "创建评论失败"));

            return Results.Ok(ApiResponse<CommentDto>.Ok(comment, "评论创建成功"));
        });

        group.MapPut("/comments/{id}", async (
            long id,
            UpdateCommentRequest request,
            HttpContext context,
            CommentService commentService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (comment, error) = await commentService.UpdateCommentAsync(id, currentUser.UserId, request);
            if (comment == null || error != null)
                return Results.BadRequest(ApiResponse<CommentDto>.Fail(error ?? "更新评论失败"));

            return Results.Ok(ApiResponse<CommentDto>.Ok(comment, "更新成功"));
        });

        group.MapDelete("/comments/{id}", async (
            long id,
            HttpContext context,
            CommentService commentService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await commentService.DeleteCommentAsync(id, currentUser.UserId);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除评论失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "评论已删除"));
        });
    }
}
