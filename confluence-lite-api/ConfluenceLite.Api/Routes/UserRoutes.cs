using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/user")
            .WithTags("Users");

        group.MapPost("/login", async (
            LoginRequest request,
            UserService userService,
            WorkspaceService workspaceService,
            TokenService tokenService) =>
        {
            if (request == null)
                return Results.BadRequest("Invalid request");

            var (user, error) = await userService.LoginAsync(request);
            if (user == null || error != null)
                return Results.Unauthorized();

            var workspaces = await workspaceService.GetUserWorkspacesAsync(user.Id);

            var response = new LoginResponse
            {
                Token = tokenService.GenerateToken(user.Id, user.Username),
                TokenType = "Bearer",
                ExpiresIn = 1440,
                User = user,
                Workspaces = workspaces.Select(w => new WorkspaceSummaryDto
                {
                    Id = w.Id,
                    Name = w.Name,
                    Key = w.Key,
                    Icon = w.Icon,
                    IsDefault = w.IsDefault
                }).ToList()
            };

            return Results.Ok(ApiResponse<LoginResponse>.Ok(response, "登录成功"));
        });

        group.MapPost("/register", async (
            CreateUserRequest request,
            UserService userService) =>
        {
            var (user, error) = await userService.CreateUserAsync(request);
            if (user == null || error != null)
                return Results.BadRequest(ApiResponse<UserDto>.Fail(error ?? "创建用户失败"));

            return Results.Ok(ApiResponse<UserDto>.Ok(user, "用户创建成功"));
        });

        group.MapGet("/list", async (
            int page,
            int pageSize,
            UserService userService) =>
        {
            var pagedRequest = new PagedRequest { Page = page, PageSize = pageSize };
            var result = await userService.GetUserListAsync(pagedRequest);
            return Results.Ok(ApiResponse<PagedResponse<UserDto>>.Ok(result));
        });

        group.MapGet("/{id}", async (long id, UserService userService) =>
        {
            var user = await userService.GetUserByIdAsync(id);
            if (user == null)
                return Results.NotFound(ApiResponse<UserDto>.Fail("用户不存在"));
            return Results.Ok(ApiResponse<UserDto>.Ok(user));
        });

        group.MapPost("/change-password", async (
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
    }
}
