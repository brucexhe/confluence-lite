using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 用户组服务
/// </summary>
public class UserGroupService
{
    private readonly AppDbContext _db;

    public UserGroupService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取用户组列表（分页、搜索）
    /// </summary>
    public async Task<(PagedResponse<UserGroupDto>?, string?)> GetUserGroupsAsync(PagedRequest request, string? search)
    {
        try
        {
            var query = _db.Db.Queryable<UserGroup>();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(g => g.Name.Contains(search) || (g.Description != null && g.Description.Contains(search)));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(g => g.Id)
                .ToPageListAsync(request.Page, request.PageSize);

            var dtos = new List<UserGroupDto>();
            foreach (var item in items)
            {
                var memberCount = await _db.Db.Queryable<UserGroupMember>().Where(m => m.GroupId == item.Id).CountAsync();
                dtos.Add(new UserGroupDto
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description ?? string.Empty,
                    MemberCount = memberCount,
                    CreatedAt = item.CreatedAt
                });
            }

            var response = new PagedResponse<UserGroupDto>
            {
                Items = dtos,
                Total = total,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return (response, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 获取用户组详情
    /// </summary>
    public async Task<(UserGroupDto?, string?)> GetUserGroupByIdAsync(long id)
    {
        try
        {
            var group = await _db.Db.Queryable<UserGroup>().FirstAsync(g => g.Id == id);
            if (group == null)
                return (null, "用户组不存在");

            var memberCount = await _db.Db.Queryable<UserGroupMember>().Where(m => m.GroupId == id).CountAsync();

            var dto = new UserGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description ?? string.Empty,
                MemberCount = memberCount,
                CreatedAt = group.CreatedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 创建用户组
    /// </summary>
    public async Task<(UserGroupDto?, string?)> CreateUserGroupAsync(CreateUserGroupRequest request)
    {
        try
        {
            var existing = await _db.Db.Queryable<UserGroup>().Where(g => g.Name == request.Name).FirstAsync();
            if (existing != null)
                return (null, "用户组名称已存在");

            var group = new UserGroup
            {
                Name = request.Name,
                Description = request.Description,
                IsDefault = false,
                IsSystem = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var id = await _db.UserGroups.InsertReturnIdentityAsync(group);
            group.Id = id;

            var dto = new UserGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description ?? string.Empty,
                MemberCount = 0,
                CreatedAt = group.CreatedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 更新用户组
    /// </summary>
    public async Task<(UserGroupDto?, string?)> UpdateUserGroupAsync(long id, UpdateUserGroupRequest request)
    {
        try
        {
            var group = await _db.Db.Queryable<UserGroup>().FirstAsync(g => g.Id == id);
            if (group == null)
                return (null, "用户组不存在");

            if (group.IsSystem)
                return (null, "系统组不能修改");

            var existing = await _db.Db.Queryable<UserGroup>().Where(g => g.Name == request.Name && g.Id != id).FirstAsync();
            if (existing != null)
                return (null, "用户组名称已存在");

            group.Name = request.Name;
            group.Description = request.Description;
            group.UpdatedAt = DateTime.Now;

            await _db.UserGroups.UpdateAsync(group);

            var memberCount = await _db.Db.Queryable<UserGroupMember>().Where(m => m.GroupId == id).CountAsync();

            var dto = new UserGroupDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description ?? string.Empty,
                MemberCount = memberCount,
                CreatedAt = group.CreatedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 删除用户组
    /// </summary>
    public async Task<(bool, string?)> DeleteUserGroupAsync(long id)
    {
        try
        {
            var group = await _db.Db.Queryable<UserGroup>().FirstAsync(g => g.Id == id);
            if (group == null)
                return (false, "用户组不存在");

            if (group.IsSystem)
                return (false, "系统组不能删除");

            await _db.UserGroupMembers.DeleteAsync(m => m.GroupId == id);
            await _db.UserGroups.DeleteAsync(g => g.Id == id);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 获取用户组成员列表
    /// </summary>
    public async Task<(UserGroupMembersResponse?, string?)> GetGroupMembersAsync(long id)
    {
        try
        {
            var group = await _db.Db.Queryable<UserGroup>().FirstAsync(g => g.Id == id);
            if (group == null)
                return (null, "用户组不存在");

            var members = await _db.Db.Queryable<UserGroupMember>()
                .LeftJoin<User>((m, u) => m.UserId == u.Id)
                .Where(m => m.GroupId == id)
                .Select((m, u) => new UserGroupMemberDto
                {
                    UserId = m.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    DisplayName = u.DisplayName,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();

            var response = new UserGroupMembersResponse
            {
                GroupId = group.Id,
                GroupName = group.Name,
                Members = members,
                Total = members.Count
            };

            return (response, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 添加用户组成员
    /// </summary>
    public async Task<(bool, string?)> AddGroupMembersAsync(long id, AddGroupMembersRequest request)
    {
        try
        {
            var group = await _db.Db.Queryable<UserGroup>().FirstAsync(g => g.Id == id);
            if (group == null)
                return (false, "用户组不存在");

            var existingMemberIds = await _db.Db.Queryable<UserGroupMember>()
                .Where(m => m.GroupId == id && request.UserIds.Contains(m.UserId))
                .Select(m => m.UserId)
                .ToListAsync();

            var newUserIds = request.UserIds.Where(uid => !existingMemberIds.Contains(uid)).ToList();

            if (newUserIds.Count == 0)
                return (true, null);

            var members = newUserIds.Select(userId => new UserGroupMember
            {
                GroupId = id,
                UserId = userId,
                Role = 0,
                JoinedAt = DateTime.Now
            }).ToList();

            await _db.UserGroupMembers.InsertRangeAsync(members);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 移除用户组成员
    /// </summary>
    public async Task<(bool, string?)> RemoveGroupMemberAsync(long id, long userId)
    {
        try
        {
            var member = await _db.Db.Queryable<UserGroupMember>()
                .Where(m => m.GroupId == id && m.UserId == userId)
                .FirstAsync();

            if (member == null)
                return (false, "成员不存在");

            await _db.UserGroupMembers.DeleteAsync(m => m.GroupId == id && m.UserId == userId);

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
