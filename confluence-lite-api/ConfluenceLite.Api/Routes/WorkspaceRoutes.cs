using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Routes;

public static class WorkspaceRoutes
{
    public static void MapWorkspaceRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/workspace")
            .WithTags("Workspaces");

        group.MapPost("/", async (
            CreateWorkspaceRequest request,
            HttpContext context,
            WorkspaceService workspaceService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (workspace, error) = await workspaceService.CreateWorkspaceAsync(currentUser.UserId, request);
            if (workspace == null || error != null)
                return Results.BadRequest(ApiResponse<WorkspaceDto>.Fail(error ?? "创建工作空间失败"));

            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace, "工作空间创建成功"));
        });

        group.MapGet("/{id}", async (long id, WorkspaceService workspaceService) =>
        {
            var workspace = await workspaceService.GetWorkspaceByIdAsync(id);
            if (workspace == null)
                return Results.NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));
            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
        });

        group.MapGet("/key/{key}", async (string key, WorkspaceService workspaceService) =>
        {
            var workspace = await workspaceService.GetWorkspaceByKeyAsync(key);
            if (workspace == null)
                return Results.NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));
            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
        });

        group.MapGet("/list", async (
            int page,
            int pageSize,
            WorkspaceService workspaceService) =>
        {
            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var result = await workspaceService.GetWorkspaceListAsync(pagedRequest);
            return Results.Ok(ApiResponse<PagedResponse<WorkspaceDto>>.Ok(result));
        });

        group.MapGet("/my", async (HttpContext context, WorkspaceService workspaceService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var workspaces = await workspaceService.GetUserWorkspacesAsync(currentUser.UserId);
            return Results.Ok(ApiResponse<List<WorkspaceDto>>.Ok(workspaces));
        });

        group.MapPut("/{id}", async (
            long id,
            UpdateWorkspaceRequest request,
            HttpContext context,
            WorkspaceService workspaceService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (workspace, error) = await workspaceService.UpdateWorkspaceAsync(id, currentUser.UserId, request);
            if (workspace == null || error != null)
                return Results.BadRequest(ApiResponse<WorkspaceDto>.Fail(error ?? "更新工作空间失败"));

            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace, "更新成功"));
        });

        group.MapDelete("/{id}", async (
            long id,
            HttpContext context,
            WorkspaceService workspaceService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await workspaceService.DeleteWorkspaceAsync(id, currentUser.UserId);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除工作空间失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "工作空间已删除"));
        });
    }
}
