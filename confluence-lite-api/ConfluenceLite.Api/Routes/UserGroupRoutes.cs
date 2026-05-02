using Microsoft.AspNetCore.Mvc;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Mappers;

namespace ConfluenceLite.Api.Routes;

/// <summary>
/// 用户组路由
/// </summary>
public static class UserGroupRoutes
{
    public static void MapUserGroupRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/user-groups")
            .WithTags("User Groups");

        // 获取用户组列表
        group.MapGet("/", async (
            UserGroupService service,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null) =>
        {
            var request = new PagedRequest { Page = page, PageSize = pageSize };
            var (data, error) = await service.GetUserGroupsAsync(request, search);

            if (error != null)
                return Results.BadRequest(ApiResponse<PagedResponse<UserGroupDto>>.Fail(error));

            return Results.Ok(ApiResponse<PagedResponse<UserGroupDto>>.Ok(data!));
        });

        // 创建用户组
        group.MapPost("/", async (
            CreateUserGroupRequest request,
            UserGroupService service) =>
        {
            var (data, error) = await service.CreateUserGroupAsync(request);

            if (error != null)
                return Results.BadRequest(ApiResponse<UserGroupDto>.Fail(error));

            return Results.Ok(ApiResponse<UserGroupDto>.Ok(data!, "用户组创建成功"));
        });

        // 获取用户组详情
        group.MapGet("/{id}", async (
            long id,
            UserGroupService service) =>
        {
            var (data, error) = await service.GetUserGroupByIdAsync(id);

            if (error != null)
                return Results.NotFound(ApiResponse<UserGroupDto>.Fail(error));

            return Results.Ok(ApiResponse<UserGroupDto>.Ok(data!));
        });

        // 更新用户组
        group.MapPut("/{id}", async (
            long id,
            UpdateUserGroupRequest request,
            UserGroupService service) =>
        {
            var (data, error) = await service.UpdateUserGroupAsync(id, request);

            if (error != null)
                return Results.BadRequest(ApiResponse<UserGroupDto>.Fail(error));

            return Results.Ok(ApiResponse<UserGroupDto>.Ok(data!, "用户组更新成功"));
        });

        // 删除用户组
        group.MapDelete("/{id}", async (
            long id,
            UserGroupService service) =>
        {
            var (success, error) = await service.DeleteUserGroupAsync(id);

            if (error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error));

            return Results.Ok(ApiResponse<bool>.Ok(true, "用户组删除成功"));
        });

        // 获取用户组成员列表
        group.MapGet("/{id}/members", async (
            long id,
            UserGroupService service) =>
        {
            var (data, error) = await service.GetGroupMembersAsync(id);

            if (error != null)
                return Results.NotFound(ApiResponse<UserGroupMembersResponse>.Fail(error));

            return Results.Ok(ApiResponse<UserGroupMembersResponse>.Ok(data!));
        });

        // 添加用户组成员
        group.MapPost("/{id}/members", async (
            long id,
            AddGroupMembersRequest request,
            UserGroupService service) =>
        {
            var (success, error) = await service.AddGroupMembersAsync(id, request);

            if (error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error));

            return Results.Ok(ApiResponse<bool>.Ok(true, "成员添加成功"));
        });

        // 移除用户组成员
        group.MapDelete("/{id}/members/{userId}", async (
            long id,
            long userId,
            UserGroupService service) =>
        {
            var (success, error) = await service.RemoveGroupMemberAsync(id, userId);

            if (error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error));

            return Results.Ok(ApiResponse<bool>.Ok(true, "成员移除成功"));
        });
    }
}
