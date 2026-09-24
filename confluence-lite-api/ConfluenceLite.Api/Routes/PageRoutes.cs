using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;
using ConfluenceLite.Api.Mappers;

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

            var (page, error) = await pageService.CreatePageAsync(currentUser.UserId, currentUser.IsAdmin, request);
            if (page == null || error != null)
                return Results.BadRequest(ApiResponse<PageDto>.Fail(error ?? "创建页面失败"));

            return Results.Ok(ApiResponse<PageDto>.Ok(page, "页面创建成功"));
        });

        // 获取页面详情
        group.MapGet("/{id}", async (
            long id,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (page, error) = await pageService.GetPageByIdAsync(id, currentUser.UserId, currentUser.IsAdmin);
            if (page == null || error != null)
                return Results.Json(ApiResponse<PageDto>.Fail(error ?? "页面不存在"), AppJsonContext.Default.ApiResponsePageDto, statusCode: 404);
            return Results.Ok(ApiResponse<PageDto>.Ok(page));
        });

        // 获取所有页面列表（管理后台，仅系统管理员）
        group.MapGet("/all", async (
            int page,
            int pageSize,
            string? search,
            long? workspaceId,
            int? status,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();
            if (!currentUser.IsAdmin)
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var result = await pageService.GetAllPagesAsync(pagedRequest, search, workspaceId, status);
            return Results.Ok(ApiResponse<PagedResponse<PageDto>>.Ok(result));
        });

        // 获取工作空间的页面列表
        group.MapGet("/workspace/{workspaceId}", async (
            long workspaceId,
            int page,
            int pageSize,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var (result, error) = await pageService.GetPagesByWorkspaceAsync(workspaceId, pagedRequest, currentUser.UserId, currentUser.IsAdmin);
            if (result == null || error != null)
                return Results.Json(ApiResponse<PagedResponse<PageDto>>.Fail(error ?? "获取页面列表失败"), AppJsonContext.Default.ApiResponsePagedResponsePageDto, statusCode: 403);
            return Results.Ok(ApiResponse<PagedResponse<PageDto>>.Ok(result));
        });

        // 获取页面树
        group.MapGet("/workspace/{workspaceId}/tree", async (
            long workspaceId,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (tree, error) = await pageService.GetPageTreeAsync(workspaceId, currentUser.UserId, currentUser.IsAdmin);
            if (tree == null || error != null)
                return Results.Json(ApiResponse<List<PageTreeNodeDto>>.Fail(error ?? "获取页面树失败"), AppJsonContext.Default.ApiResponseListPageTreeNodeDto, statusCode: 403);
            return Results.Ok(ApiResponse<List<PageTreeNodeDto>>.Ok(tree));
        });

        // 获取子页面
        group.MapGet("/{parentId}/children", async (
            long parentId,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (children, error) = await pageService.GetChildPagesAsync(parentId, currentUser.UserId, currentUser.IsAdmin);
            if (children == null || error != null)
                return Results.Json(ApiResponse<List<PageDto>>.Fail(error ?? "获取子页面失败"), AppJsonContext.Default.ApiResponseListPageDto, statusCode: 403);
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

            var (success, error) = await pageService.BatchSortPagesAsync(request, currentUser.UserId, currentUser.IsAdmin);
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

            var (success, error) = await pageService.MovePageAsync(id, currentUser.UserId, currentUser.IsAdmin, request);
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

            var (page, error) = await pageService.UpdatePageAsync(id, currentUser.UserId, currentUser.IsAdmin, request);
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

            var (success, error) = await pageService.DeletePageAsync(id, currentUser.UserId, currentUser.IsAdmin);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除页面失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "页面已删除"));
        });

        // ========== 版本历史 ==========
        group.MapGet("/{pageId}/versions", async (
            long pageId,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (versions, error) = await pageService.GetPageVersionsAsync(pageId, currentUser.UserId, currentUser.IsAdmin);
            if (versions == null || error != null)
                return Results.Json(ApiResponse<List<PageVersionListDto>>.Fail(error ?? "获取版本列表失败"), AppJsonContext.Default.ApiResponseListPageVersionListDto, statusCode: 403);
            return Results.Ok(ApiResponse<List<PageVersionListDto>>.Ok(versions));
        });

        group.MapGet("/versions/{versionId}", async (
            long versionId,
            HttpContext context,
            PageService pageService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (version, error) = await pageService.GetPageVersionAsync(versionId, currentUser.UserId, currentUser.IsAdmin);
            if (version == null || error != null)
                return Results.NotFound(ApiResponse<PageVersionDto>.Fail(error ?? "版本不存在"));
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

            var (success, error) = await pageService.DeletePageVersionAsync(versionId, currentUser.UserId, currentUser.IsAdmin);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除版本失败"));
            return Results.Ok(ApiResponse<bool>.Ok(true, "版本已删除"));
        });

        // ========== 任务列表 ==========
        group.MapGet("/{pageId}/tasks", async (
            long pageId,
            HttpContext context,
            PageTaskService pageTaskService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (tasks, error) = await pageTaskService.GetPageTasksAsync(pageId, currentUser.UserId, currentUser.IsAdmin);
            if (tasks == null || error != null)
                return Results.Json(ApiResponse<List<PageTaskDto>>.Fail(error ?? "获取任务列表失败"), AppJsonContext.Default.ApiResponseListPageTaskDto, statusCode: 403);
            return Results.Ok(ApiResponse<List<PageTaskDto>>.Ok(tasks));
        });

        // 勾选/取消任务（即时保存，不产生版本快照）
        group.MapPut("/{pageId}/tasks/{taskUid}", async (
            long pageId,
            string taskUid,
            UpdatePageTaskRequest request,
            HttpContext context,
            PageTaskService pageTaskService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await pageTaskService.ToggleTaskAsync(pageId, taskUid, currentUser.UserId, currentUser.IsAdmin, request.IsCompleted);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "更新任务状态失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "任务状态已更新"));
        });

        // ========== 评论 ==========
        group.MapGet("/{pageId}/comments", async (
            long pageId,
            HttpContext context,
            CommentService commentService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (comments, error) = await commentService.GetCommentsByPageAsync(pageId, currentUser.UserId, currentUser.IsAdmin);
            if (comments == null || error != null)
                return Results.Json(ApiResponse<List<CommentDto>>.Fail(error ?? "获取评论失败"), AppJsonContext.Default.ApiResponseListCommentDto, statusCode: 403);
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

            var (comment, error) = await commentService.CreateCommentAsync(currentUser.UserId, currentUser.IsAdmin, request);
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

            var (comment, error) = await commentService.UpdateCommentAsync(id, currentUser.UserId, currentUser.IsAdmin, request);
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

            var (success, error) = await commentService.DeleteCommentAsync(id, currentUser.UserId, currentUser.IsAdmin);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除评论失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "评论已删除"));
        });
    }
}
