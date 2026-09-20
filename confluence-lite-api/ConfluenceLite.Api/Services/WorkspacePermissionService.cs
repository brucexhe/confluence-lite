using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;
using Npgsql;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 空间有效权限位（Owner / 系统管理员与权限表记录合并后的结果）
/// </summary>
public class SpacePermissions
{
    public bool IsOwner { get; set; }
    public bool IsSiteAdmin { get; set; }
    public bool ViewSpace { get; set; }
    public bool CreatePage { get; set; }
    public bool DeletePage { get; set; }
    public bool DeleteOwnPage { get; set; }
    public bool EditPage { get; set; }
    public bool ExportPage { get; set; }
    public bool AddComment { get; set; }
    public bool DeleteComment { get; set; }
    public bool AdminSpace { get; set; }
    public bool SetPermissions { get; set; }

    /// <summary>
    /// 是否空间管理员（Owner / 系统管理员 / AdminSpace / SetPermissions）
    /// </summary>
    public bool IsSpaceAdmin => IsOwner || IsSiteAdmin || AdminSpace || SetPermissions;
}

/// <summary>
/// 工作空间成员与权限服务 - Native AOT 兼容
/// 权限记录存储于 workspace_permissions 表（TargetType=2 用户粒度）
/// </summary>
public class WorkspacePermissionService
{
    private readonly AppDbContext _db;

    public WorkspacePermissionService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取用户在空间的有效权限（Owner / 系统管理员 → 全部权限；否则读权限表记录）
    /// </summary>
    public async Task<SpacePermissions> GetEffectivePermissionsAsync(long userId, bool isSiteAdmin, long workspaceId)
    {
        var result = new SpacePermissions { IsSiteAdmin = isSiteAdmin };

        var workspace = await _db.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null || workspace.IsDeleted)
        {
            return result;
        }

        // Owner / 系统管理员拥有全部权限
        if (workspace.OwnerId == userId || isSiteAdmin)
        {
            result.IsOwner = workspace.OwnerId == userId;
            result.ViewSpace = true;
            result.CreatePage = true;
            result.DeletePage = true;
            result.DeleteOwnPage = true;
            result.EditPage = true;
            result.ExportPage = true;
            result.AddComment = true;
            result.DeleteComment = true;
            result.AdminSpace = true;
            result.SetPermissions = true;
            return result;
        }

        var perm = await GetMemberPermissionAsync(workspaceId, userId);
        if (perm == null)
        {
            return result;
        }

        result.ViewSpace = perm.ViewSpace;
        result.CreatePage = perm.CreatePage;
        result.DeletePage = perm.DeletePage;
        result.DeleteOwnPage = perm.DeleteOwnPage;
        result.EditPage = perm.EditPage;
        result.ExportPage = perm.ExportPage;
        result.AddComment = perm.AddComment;
        result.DeleteComment = perm.DeleteComment;
        result.AdminSpace = perm.AdminSpace;
        result.SetPermissions = perm.SetPermissions;
        return result;
    }

    /// <summary>
    /// 是否空间管理员（Owner / 系统管理员 / AdminSpace / SetPermissions）
    /// </summary>
    public async Task<bool> IsSpaceAdminAsync(long userId, bool isSiteAdmin, long workspaceId)
    {
        var p = await GetEffectivePermissionsAsync(userId, isSiteAdmin, workspaceId);
        return p.IsSpaceAdmin;
    }

    /// <summary>
    /// 加入公开空间（默认仅 ViewSpace，幂等）
    /// </summary>
    public async Task<(bool success, string? error)> JoinAsync(long userId, long workspaceId)
    {
        var workspace = await _db.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null || workspace.IsDeleted)
        {
            return (false, "工作空间不存在");
        }
        if (!workspace.IsPublic)
        {
            return (false, "该空间为非公开空间，无法自助加入");
        }
        if (workspace.OwnerId == userId)
        {
            return (true, null); // Owner 无需加入
        }

        if (await GetMemberPermissionAsync(workspaceId, userId) != null)
        {
            return (true, null); // 已加入，幂等
        }

        var perm = new WorkspacePermission
        {
            WorkspaceId = workspaceId,
            TargetType = 2,
            TargetId = userId,
            ViewSpace = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        try
        {
            await _db.Db.Insertable(perm).ExecuteReturnIdentityAsync();
        }
        catch (NpgsqlException ex) when (ex.SqlState == "23505")
        {
            // 并发重复加入，唯一约束兜底，视为已加入
        }

        return (true, null);
    }

    /// <summary>
    /// 退出空间（Owner 不可退出自己的空间）
    /// </summary>
    public async Task<(bool success, string? error)> LeaveAsync(long userId, long workspaceId)
    {
        var workspace = await _db.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null || workspace.IsDeleted)
        {
            return (false, "工作空间不存在");
        }
        if (workspace.OwnerId == userId)
        {
            return (false, "空间所有者不能退出自己的空间");
        }

        await _db.Db.Deleteable<WorkspacePermission>()
            .Where(p => p.WorkspaceId == workspaceId && p.TargetType == 2 && p.TargetId == userId)
            .ExecuteCommandAsync();

        return (true, null);
    }

    /// <summary>
    /// 添加或更新空间成员权限（幂等；不可操作 Owner）
    /// </summary>
    public async Task<(bool success, string? error)> SetMemberAsync(long workspaceId, UpdateMemberPermissionsRequest request)
    {
        var workspace = await _db.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null || workspace.IsDeleted)
        {
            return (false, "工作空间不存在");
        }
        if (request.UserId == workspace.OwnerId)
        {
            return (false, "不能修改空间所有者的权限");
        }

        var targetUser = await _db.Users.GetByIdAsync(request.UserId);
        if (targetUser == null || targetUser.IsDeleted || targetUser.Status == 0)
        {
            return (false, "目标用户不存在或已被禁用");
        }

        var perm = await GetMemberPermissionAsync(workspaceId, request.UserId);
        var isNew = perm == null;
        perm ??= new WorkspacePermission
        {
            WorkspaceId = workspaceId,
            TargetType = 2,
            TargetId = request.UserId,
            CreatedAt = DateTime.Now
        };

        perm.ViewSpace = request.ViewSpace;
        perm.CreatePage = request.CreatePage;
        perm.DeletePage = request.DeletePage;
        perm.DeleteOwnPage = request.DeleteOwnPage;
        perm.EditPage = request.EditPage;
        perm.ExportPage = request.ExportPage;
        perm.AddComment = request.AddComment;
        perm.DeleteComment = request.DeleteComment;
        perm.AdminSpace = request.AdminSpace;
        perm.SetPermissions = request.SetPermissions;
        perm.UpdatedAt = DateTime.Now;

        if (isNew)
        {
            try
            {
                await _db.Db.Insertable(perm).ExecuteReturnIdentityAsync();
            }
            catch (NpgsqlException ex) when (ex.SqlState == "23505")
            {
                return (false, "该用户已是空间成员");
            }
        }
        else
        {
            await _db.Db.Updateable(perm).ExecuteCommandAsync();
        }

        return (true, null);
    }

    /// <summary>
    /// 移除空间成员（不可移除 Owner）
    /// </summary>
    public async Task<(bool success, string? error)> RemoveMemberAsync(long workspaceId, long targetUserId)
    {
        var workspace = await _db.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null || workspace.IsDeleted)
        {
            return (false, "工作空间不存在");
        }
        if (targetUserId == workspace.OwnerId)
        {
            return (false, "不能移除空间所有者");
        }

        await _db.Db.Deleteable<WorkspacePermission>()
            .Where(p => p.WorkspaceId == workspaceId && p.TargetType == 2 && p.TargetId == targetUserId)
            .ExecuteCommandAsync();

        return (true, null);
    }

    /// <summary>
    /// 获取空间成员列表（Owner + 权限表用户）
    /// </summary>
    public async Task<PagedResponse<WorkspaceMemberDto>> ListMembersAsync(long workspaceId, PagedRequest request, string? q = null)
    {
        var result = new PagedResponse<WorkspaceMemberDto>
        {
            Items = new List<WorkspaceMemberDto>(),
            Page = request.Page,
            PageSize = request.PageSize
        };

        var workspace = await _db.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null || workspace.IsDeleted)
        {
            return result;
        }

        var perms = await _db.Db.Queryable<WorkspacePermission>()
            .Where(p => p.WorkspaceId == workspaceId && p.TargetType == 2)
            .ToListAsync();
        var permDict = perms
            .Where(p => p.TargetId.HasValue)
            .GroupBy(p => p.TargetId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var memberIds = perms.Where(p => p.TargetId.HasValue).Select(p => p.TargetId!.Value).ToList();
        if (!memberIds.Contains(workspace.OwnerId))
        {
            memberIds.Add(workspace.OwnerId);
        }

        var users = await _db.Db.Queryable<User>()
            .Where(u => memberIds.Contains(u.Id) && !u.IsDeleted)
            .ToListAsync();

        IEnumerable<User> userQuery = users.OrderBy(u => u.Id);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            userQuery = userQuery.Where(u =>
                (u.Username != null && u.Username.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (u.DisplayName != null && u.DisplayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                (u.Email != null && u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
        }

        var filtered = userQuery.ToList();
        result.Total = filtered.Count;

        foreach (var user in filtered.Skip(request.Skip).Take(request.PageSize))
        {
            permDict.TryGetValue(user.Id, out var perm);
            var isOwner = user.Id == workspace.OwnerId;

            result.Items.Add(new WorkspaceMemberDto
            {
                User = new UserSummaryDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl
                },
                IsOwner = isOwner,
                ViewSpace = isOwner || (perm?.ViewSpace ?? false),
                CreatePage = isOwner || (perm?.CreatePage ?? false),
                DeletePage = isOwner || (perm?.DeletePage ?? false),
                DeleteOwnPage = isOwner || (perm?.DeleteOwnPage ?? false),
                EditPage = isOwner || (perm?.EditPage ?? false),
                ExportPage = isOwner || (perm?.ExportPage ?? false),
                AddComment = isOwner || (perm?.AddComment ?? false),
                DeleteComment = isOwner || (perm?.DeleteComment ?? false),
                AdminSpace = isOwner || (perm?.AdminSpace ?? false),
                SetPermissions = isOwner || (perm?.SetPermissions ?? false),
                JoinedAt = perm?.CreatedAt ?? workspace.CreatedAt
            });
        }

        return result;
    }

    /// <summary>
    /// 搜索可邀请的用户（排除已是空间成员的用户，限 10 条）
    /// </summary>
    public async Task<List<UserSummaryDto>> SearchUsersAsync(long workspaceId, string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return new List<UserSummaryDto>();
        }
        var keyword = q.Trim();

        var workspace = await _db.Workspaces.GetByIdAsync(workspaceId);
        if (workspace == null || workspace.IsDeleted)
        {
            return new List<UserSummaryDto>();
        }

        var perms = await _db.Db.Queryable<WorkspacePermission>()
            .Where(p => p.WorkspaceId == workspaceId && p.TargetType == 2)
            .ToListAsync();
        var memberIds = perms.Where(p => p.TargetId.HasValue).Select(p => p.TargetId!.Value).ToList();
        if (!memberIds.Contains(workspace.OwnerId))
        {
            memberIds.Add(workspace.OwnerId);
        }

        var users = await _db.Db.Queryable<User>()
            .Where(u => !u.IsDeleted && u.Status == 1 && !memberIds.Contains(u.Id))
            .Where(u => u.Username.Contains(keyword) || (u.DisplayName != null && u.DisplayName.Contains(keyword)) || (u.Email != null && u.Email.Contains(keyword)))
            .Take(10)
            .ToListAsync();

        return users.Select(u => new UserSummaryDto
        {
            Id = u.Id,
            Username = u.Username,
            DisplayName = u.DisplayName,
            AvatarUrl = u.AvatarUrl
        }).ToList();
    }

    /// <summary>
    /// 获取公开空间目录（含当前用户加入状态与成员数）
    /// </summary>
    public async Task<PagedResponse<DiscoverWorkspaceDto>> GetDiscoverAsync(long userId, PagedRequest request, string? q = null)
    {
        var result = new PagedResponse<DiscoverWorkspaceDto>
        {
            Items = new List<DiscoverWorkspaceDto>(),
            Page = request.Page,
            PageSize = request.PageSize
        };

        var query = _db.Db.Queryable<Workspace>()
            .Where(w => w.IsPublic && !w.IsDeleted && w.Status == 1);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(w => w.Name.Contains(keyword) || (w.Description != null && w.Description.Contains(keyword)));
        }

        result.Total = await query.CountAsync();
        var workspaces = await query
            .OrderBy(w => w.Id)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();
        if (workspaces.Count == 0)
        {
            return result;
        }

        var wsIds = workspaces.Select(w => w.Id).ToList();

        // 当前用户已加入（ViewSpace）的空间
        var joinedIds = (await _db.Db.Queryable<WorkspacePermission>()
                .Where(p => p.TargetType == 2 && p.TargetId == userId && p.ViewSpace && wsIds.Contains(p.WorkspaceId))
                .Select(p => p.WorkspaceId)
                .ToListAsync())
            .ToHashSet();

        // 各空间成员数（用户粒度权限记录数）
        var memberCounts = (await _db.Db.Queryable<WorkspacePermission>()
                .Where(p => p.TargetType == 2 && wsIds.Contains(p.WorkspaceId))
                .GroupBy(p => p.WorkspaceId)
                .Select(p => new
                {
                    WorkspaceId = p.WorkspaceId,
                    Count = SqlFunc.AggregateCount(p.Id)
                })
                .ToListAsync())
            .ToDictionary(x => x.WorkspaceId, x => x.Count);

        foreach (var workspace in workspaces)
        {
            var owner = await _db.Users.GetByIdAsync(workspace.OwnerId);
            var pageCount = await _db.Db.Queryable<Page>()
                .Where(p => p.WorkspaceId == workspace.Id && !p.IsDeleted)
                .CountAsync();

            result.Items.Add(new DiscoverWorkspaceDto
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Description = workspace.Description,
                Key = workspace.Key,
                Icon = workspace.Icon,
                Owner = owner == null ? null : new UserSummaryDto
                {
                    Id = owner.Id,
                    Username = owner.Username,
                    DisplayName = owner.DisplayName,
                    AvatarUrl = owner.AvatarUrl
                },
                MemberCount = memberCounts.TryGetValue(workspace.Id, out var count)
                    ? count + (owner != null ? 1 : 0)
                    : 1,
                PageCount = pageCount,
                IsJoined = joinedIds.Contains(workspace.Id) || workspace.OwnerId == userId,
                IsOwner = workspace.OwnerId == userId
            });
        }

        return result;
    }

    /// <summary>
    /// 获取用户在指定空间的成员权限记录（未加入返回 null）
    /// </summary>
    private async Task<WorkspacePermission?> GetMemberPermissionAsync(long workspaceId, long userId)
    {
        return await _db.Db.Queryable<WorkspacePermission>()
            .Where(p => p.WorkspaceId == workspaceId && p.TargetType == 2 && p.TargetId == userId)
            .FirstAsync();
    }
}
