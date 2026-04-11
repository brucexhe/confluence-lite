using Microsoft.AspNetCore.Mvc;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;
using ConfluenceLite.Api.Mappers;

namespace ConfluenceLite.Api.Routes;

/// <summary>
/// Minimal API 路由定义 - Native AOT 兼容
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// 注册所有 API 路由
    /// </summary>
    public static void RegisterRoutes(WebApplication app)
    {
        // ========== 安装向导路由 (无需认证) ==========
        var setupGroup = app.MapGroup("/api/setup")
            .WithTags("Setup");

        // 检查安装状态
        setupGroup.MapGet("/status", (SetupService setupService) =>
        {
            var installed = setupService.IsInstalled();
            return Results.Ok(ApiResponse<SetupStatusResponse>.Ok(
                new SetupStatusResponse { Installed = installed }));
        });

        // 测试数据库连接
        setupGroup.MapPost("/test-connection", async (
            DatabaseConfigRequest request,
            SetupService setupService) =>
        {
            if (setupService.IsInstalled())
                return Results.BadRequest(ApiResponse<TestConnectionResponse>.Fail("系统已安装"));

            var (result, error) = await setupService.TestConnectionAsync(request);
            if (error != null)
                return Results.BadRequest(ApiResponse<TestConnectionResponse>.Fail(error));

            return Results.Ok(ApiResponse<TestConnectionResponse>.Ok(result!));
        });

        // 执行安装
        setupGroup.MapPost("/install", async (
            SetupRequest request,
            SetupService setupService) =>
        {
            if (setupService.IsInstalled())
                return Results.BadRequest(ApiResponse<SetupResponse>.Fail("系统已安装"));

            var (result, error) = await setupService.InstallAsync(request);
            if (error != null || result == null)
                return Results.BadRequest(ApiResponse<SetupResponse>.Fail(error ?? "安装失败"));

            return Results.Ok(ApiResponse<SetupResponse>.Ok(result, "安装成功"));
        });

        // ========== 用户相关路由 ==========
        var userGroup = app.MapGroup("/api/user")
            .WithTags("Users");

        // 用户登录
        userGroup.MapPost("/login", async (
            LoginRequest request,
            UserService userService,
            TokenService tokenService) =>
        {
            if (request == null)
                return Results.BadRequest("Invalid request");

            var (user, error) = await userService.LoginAsync(request);
            if (user == null || error != null)
                return Results.Unauthorized();

            var response = new LoginResponse
            {
                Token = tokenService.GenerateToken(user.Id, user.Username),
                TokenType = "Bearer",
                ExpiresIn = 1440,
                User = user
            };

            return Results.Ok(ApiResponse<LoginResponse>.Ok(response, "登录成功"));
        });

        // 注册用户
        userGroup.MapPost("/register", async (
            CreateUserRequest request,
            UserService userService) =>
        {
            var (user, error) = await userService.CreateUserAsync(request);
            if (user == null || error != null)
                return Results.BadRequest(ApiResponse<UserDto>.Fail(error ?? "创建用户失败"));

            return Results.Ok(ApiResponse<UserDto>.Ok(user, "用户创建成功"));
        });

        // 获取用户列表
        userGroup.MapGet("/list", async (
            int page,
            int pageSize,
            UserService userService) =>
        {
            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var result = await userService.GetUserListAsync(pagedRequest);
            return Results.Ok(ApiResponse<PagedResponse<UserDto>>.Ok(result));
        });

        // 获取用户详情
        userGroup.MapGet("/{id}", async (long id, UserService userService) =>
        {
            var user = await userService.GetUserByIdAsync(id);
            if (user == null)
                return Results.NotFound(ApiResponse<UserDto>.Fail("用户不存在"));
            return Results.Ok(ApiResponse<UserDto>.Ok(user));
        });

        // 修改密码
        userGroup.MapPost("/change-password", async (
            ChangePasswordRequest request,
            HttpContext context,
            UserService userService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await userService.ChangePasswordAsync(currentUser.UserId, request);
            if (!success || error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "修改密码失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "密码修改成功"));
        });

        // ========== 工作空间相关路由 ==========
        var workspaceGroup = app.MapGroup("/api/workspace")
            .WithTags("Workspaces");

        // 创建工作空间
        workspaceGroup.MapPost("/", async (
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

        // 获取工作空间详情
        workspaceGroup.MapGet("/{id}", async (long id, WorkspaceService workspaceService) =>
        {
            var workspace = await workspaceService.GetWorkspaceByIdAsync(id);
            if (workspace == null)
                return Results.NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));
            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
        });

        // 根据 Key 获取工作空间
        workspaceGroup.MapGet("/key/{key}", async (string key, WorkspaceService workspaceService) =>
        {
            var workspace = await workspaceService.GetWorkspaceByKeyAsync(key);
            if (workspace == null)
                return Results.NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));
            return Results.Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
        });

        // 获取工作空间列表
        workspaceGroup.MapGet("/list", async (
            int page,
            int pageSize,
            WorkspaceService workspaceService) =>
        {
            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var result = await workspaceService.GetWorkspaceListAsync(pagedRequest);
            return Results.Ok(ApiResponse<PagedResponse<WorkspaceDto>>.Ok(result));
        });

        // 获取我的工作空间
        workspaceGroup.MapGet("/my", async (HttpContext context, WorkspaceService workspaceService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var workspaces = await workspaceService.GetUserWorkspacesAsync(currentUser.UserId);
            return Results.Ok(ApiResponse<List<WorkspaceDto>>.Ok(workspaces));
        });

        // 更新工作空间
        workspaceGroup.MapPut("/{id}", async (
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

        // 删除工作空间
        workspaceGroup.MapDelete("/{id}", async (
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

        // ========== 页面相关路由 ==========
        var pageGroup = app.MapGroup("/api/page")
            .WithTags("Pages");

        // 创建页面
        pageGroup.MapPost("/", async (
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
        pageGroup.MapGet("/{id}", async (long id, PageService pageService) =>
        {
            var page = await pageService.GetPageByIdAsync(id);
            if (page == null)
                return Results.NotFound(ApiResponse<PageDto>.Fail("页面不存在"));
            return Results.Ok(ApiResponse<PageDto>.Ok(page));
        });

        // 获取工作空间的页面列表
        pageGroup.MapGet("/workspace/{workspaceId}", async (
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
        pageGroup.MapGet("/workspace/{workspaceId}/tree", async (
            long workspaceId,
            PageService pageService) =>
        {
            var tree = await pageService.GetPageTreeAsync(workspaceId);
            return Results.Ok(ApiResponse<List<PageTreeNodeDto>>.Ok(tree));
        });

        // 获取子页面
        pageGroup.MapGet("/{parentId}/children", async (
            long parentId,
            PageService pageService) =>
        {
            var children = await pageService.GetChildPagesAsync(parentId);
            return Results.Ok(ApiResponse<List<PageDto>>.Ok(children));
        });

        // 更新页面
        pageGroup.MapPut("/{id}", async (
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
        pageGroup.MapDelete("/{id}", async (
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

        // ========== 评论相关路由 ==========
        var commentGroup = app.MapGroup("/api/page/{pageId}/comments")
            .WithTags("Comments");

        // 获取页面评论
        commentGroup.MapGet("/", async (long pageId, CommentService commentService) =>
        {
            var comments = await commentService.GetCommentsByPageAsync(pageId);
            return Results.Ok(ApiResponse<List<CommentDto>>.Ok(comments));
        });

        // 创建评论
        commentGroup.MapPost("/", async (
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

        // 评论管理路由（单个评论操作）
        var singleCommentGroup = app.MapGroup("/api/page/comments")
            .WithTags("Comments");

        // 更新评论
        singleCommentGroup.MapPut("/{id}", async (
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

        // 删除评论
        singleCommentGroup.MapDelete("/{id}", async (
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
