using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;
using ConfluenceLite.Api.Mappers;

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

        group.MapGet("/{id}", async (
            long id,
            HttpContext context,
            WorkspaceService workspaceService,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            // 需要空间查看权限
            var permissions = await permissionService.GetEffectivePermissionsAsync(currentUser.UserId, currentUser.IsAdmin, id);
            if (!permissions.ViewSpace)
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            var workspace = await workspaceService.GetWorkspaceByIdAsync(id);
            if (workspace == null)
                return Results.NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));
            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
        });

        group.MapGet("/key/{key}", async (
            string key,
            HttpContext context,
            WorkspaceService workspaceService,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var workspace = await workspaceService.GetWorkspaceByKeyAsync(key);
            if (workspace == null)
                return Results.NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));

            // 需要空间查看权限
            var permissions = await permissionService.GetEffectivePermissionsAsync(currentUser.UserId, currentUser.IsAdmin, workspace.Id);
            if (!permissions.ViewSpace)
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
        });

        // 空间任务总览：按页面分组返回空间内全部任务
        group.MapGet("/key/{key}/tasks", async (
            string key,
            HttpContext context,
            WorkspaceService workspaceService,
            PageTaskService pageTaskService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

var workspace = await workspaceService.GetWorkspaceByKeyAsync(key);
            if (workspace == null)
                return Results.NotFound(ApiResponse<List<PageTaskGroupDto>>.Fail("工作空间不存在"));

var (groups, error) = await pageTaskService.GetWorkspaceTasksAsync(workspace.Id, currentUser.UserId, currentUser.IsAdmin);
if (groups == null || error != null)
                return Results.Json(ApiResponse<List<PageTaskGroupDto>>.Fail(error ?? "获取任务列表失败"), AppJsonContext.Default.ApiResponseListPageTaskGroupDto, statusCode: 403);

            return Results.Ok(ApiResponse<List<PageTaskGroupDto>>.Ok(groups));
        });

        // 全量空间列表：仅系统管理员（管理后台专用）
        group.MapGet("/list", async (
            int page,
            int pageSize,
            HttpContext context,
            WorkspaceService workspaceService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();
            if (!currentUser.IsAdmin)
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

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

        // ========== 空间目录与成员 ==========

        // 公开空间目录
        group.MapGet("/discover", async (
            int page,
            int pageSize,
            string? q,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var pagedRequest = new PagedRequest { Page = page < 1 ? 1 : page, PageSize = pageSize < 1 ? 20 : pageSize };
            var result = await permissionService.GetDiscoverAsync(currentUser.UserId, pagedRequest, q);
            return Results.Ok(ApiResponse<PagedResponse<DiscoverWorkspaceDto>>.Ok(result));
        });

        // 加入公开空间（默认仅 ViewSpace，幂等）
        group.MapPost("/{id}/join", async (
            long id,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await permissionService.JoinAsync(currentUser.UserId, id);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "加入空间失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "已加入空间"));
        });

        // 退出空间（Owner 不可退出）
        group.MapDelete("/{id}/leave", async (
            long id,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await permissionService.LeaveAsync(currentUser.UserId, id);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "退出空间失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "已退出空间"));
        });

        // 当前用户在该空间的有效权限（用于前端控制按钮显隐）
        group.MapGet("/{id}/my-permissions", async (
            long id,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var permissions = await permissionService.GetEffectivePermissionsAsync(currentUser.UserId, currentUser.IsAdmin, id);
            return Results.Ok(ApiResponse<SpacePermissions>.Ok(permissions));
        });

        // 成员列表（空间管理员）
        group.MapGet("/{id}/members", async (
            long id,
            int page,
            int pageSize,
            string? q,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();
            if (!await permissionService.IsSpaceAdminAsync(currentUser.UserId, currentUser.IsAdmin, id))
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            var pagedRequest = new PagedRequest { Page = page < 1 ? 1 : page, PageSize = pageSize < 1 ? 20 : pageSize };
            var result = await permissionService.ListMembersAsync(id, pagedRequest, q);
            return Results.Ok(ApiResponse<PagedResponse<WorkspaceMemberDto>>.Ok(result));
        });

        // 添加/邀请成员（空间管理员，幂等 upsert）
        group.MapPost("/{id}/members", async (
            long id,
            UpdateMemberPermissionsRequest request,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();
            if (!await permissionService.IsSpaceAdminAsync(currentUser.UserId, currentUser.IsAdmin, id))
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            var (success, error) = await permissionService.SetMemberAsync(id, request);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "添加成员失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "成员已添加"));
        });

        // 修改成员权限（空间管理员，不可操作 Owner）
        group.MapPut("/{id}/members/{userId}", async (
            long id,
            long userId,
            UpdateMemberPermissionsRequest request,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();
            if (!await permissionService.IsSpaceAdminAsync(currentUser.UserId, currentUser.IsAdmin, id))
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            request.UserId = userId;
            var (success, error) = await permissionService.SetMemberAsync(id, request);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "更新成员权限失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "成员权限已更新"));
        });

        // 移除成员（空间管理员，不可移除 Owner）
        group.MapDelete("/{id}/members/{userId}", async (
            long id,
            long userId,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();
            if (!await permissionService.IsSpaceAdminAsync(currentUser.UserId, currentUser.IsAdmin, id))
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            var (success, error) = await permissionService.RemoveMemberAsync(id, userId);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "移除成员失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "成员已移除"));
        });

        // 搜索可邀请的用户（空间管理员专用，限 10 条）
        group.MapGet("/{id}/members/search-users", async (
            long id,
            string? q,
            HttpContext context,
            WorkspacePermissionService permissionService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();
            if (!await permissionService.IsSpaceAdminAsync(currentUser.UserId, currentUser.IsAdmin, id))
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            var users = await permissionService.SearchUsersAsync(id, q);
            return Results.Ok(ApiResponse<List<UserSummaryDto>>.Ok(users));
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
