using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;
using Npgsql;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 工作空间服务 - Native AOT 兼容
/// </summary>
public class WorkspaceService
{
    private readonly AppDbContext _db;

    public WorkspaceService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 创建工作空间
    /// </summary>
    public async Task<(WorkspaceDto? workspace, string? error)> CreateWorkspaceAsync(long ownerId, CreateWorkspaceRequest request)
    {
        // 将 Key 统一转换为大写
        var upperKey = request.Key.ToUpper();

        // 检查Key是否已存在（只检查未删除的工作空间，允许重用已删除空间的 Key）
        var exists = await _db.Db.Queryable<Workspace>()
            .Where(w => w.Key == upperKey && !w.IsDeleted)
            .AnyAsync();

        if (exists)
        {
            return (null, "工作空间标识已存在");
        }

        var workspace = new Workspace
        {
            Name = request.Name,
            Description = request.Description,
            Key = upperKey, // 存储为大写
            OwnerId = ownerId,
            Status = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        int workspaceId;
        try
        {
            workspaceId = await _db.Db.Insertable(workspace).ExecuteReturnIdentityAsync();
        }
        catch (NpgsqlException ex) when (ex.SqlState == "23505")
        {
            return (null, "工作空间标识已存在");
        }
        workspace.Id = workspaceId;

        return (await MapToDtoAsync(workspace), null);
    }

    /// <summary>
    /// 获取工作空间信息
    /// </summary>
    public async Task<WorkspaceDto?> GetWorkspaceByIdAsync(long id)
    {
        var workspace = await _db.Db.Queryable<Workspace>()
            .Where(w => w.Id == id && !w.IsDeleted)
            .FirstAsync();
        return workspace == null ? null : await MapToDtoAsync(workspace);
    }

    /// <summary>
    /// 根据Key获取工作空间
    /// </summary>
    public async Task<WorkspaceDto?> GetWorkspaceByKeyAsync(string key)
    {
        // 将输入的 key 转为大写进行查询
        var upperKey = key.ToUpper();
        var workspace = await _db.Db.Queryable<Workspace>()
            .Where(w => w.Key == upperKey && !w.IsDeleted)
            .FirstAsync();

        return workspace == null ? null : await MapToDtoAsync(workspace);
    }

    /// <summary>
    /// 获取工作空间列表
    /// </summary>
    public async Task<PagedResponse<WorkspaceDto>> GetWorkspaceListAsync(PagedRequest request)
    {
        var total = await _db.Db.Queryable<Workspace>().Where(w => !w.IsDeleted).CountAsync();

        var workspaces = await _db.Db.Queryable<Workspace>()
            .Where(w => !w.IsDeleted)
            .OrderBy(w => w.Id)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = new List<WorkspaceDto>();
        foreach (var workspace in workspaces)
        {
            dtos.Add(await MapToDtoAsync(workspace));
        }

        return new PagedResponse<WorkspaceDto>
        {
            Items = dtos,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 获取用户的工作空间列表
    /// </summary>
    public async Task<List<WorkspaceDto>> GetUserWorkspacesAsync(long userId)
    {
        var workspaces = await _db.Db.Queryable<Workspace>()
            .Where(w => w.OwnerId == userId && !w.IsDeleted)
            .OrderByDescending(w => w.UpdatedAt)
            .ToListAsync();

        var dtos = new List<WorkspaceDto>();
        foreach (var workspace in workspaces)
        {
            dtos.Add(await MapToDtoAsync(workspace));
        }

        return dtos;
    }

    /// <summary>
    /// 更新工作空间
    /// </summary>
    public async Task<(WorkspaceDto? workspace, string? error)> UpdateWorkspaceAsync(long id, long userId, UpdateWorkspaceRequest request)
    {
        var workspace = await _db.Db.Queryable<Workspace>()
            .Where(w => w.Id == id && !w.IsDeleted)
            .FirstAsync();
        if (workspace == null)
        {
            return (null, "工作空间不存在");
        }

        // 检查权限
        if (workspace.OwnerId != userId)
        {
            return (null, "无权限修改此工作空间");
        }

        if (request.Name != null)
        {
            workspace.Name = request.Name;
        }

        if (request.Description != null)
        {
            workspace.Description = request.Description;
        }

        if (request.Status.HasValue)
        {
            workspace.Status = request.Status.Value;
        }

        workspace.UpdatedAt = DateTime.Now;

        await _db.Workspaces.UpdateAsync(workspace);

        return (await MapToDtoAsync(workspace), null);
    }

    /// <summary>
    /// 删除工作空间（逻辑删除）
    /// </summary>
    public async Task<(bool success, string? error)> DeleteWorkspaceAsync(long id, long userId)
    {
        var workspace = await _db.Db.Queryable<Workspace>()
            .Where(w => w.Id == id && !w.IsDeleted)
            .FirstAsync();
        if (workspace == null)
        {
            return (false, "工作空间不存在");
        }

        // 检查权限
        if (workspace.OwnerId != userId)
        {
            return (false, "无权限删除此工作空间");
        }

        // 逻辑删除：标记为已删除
        workspace.IsDeleted = true;
        workspace.DeletedAt = DateTime.Now;
        workspace.UpdatedAt = DateTime.Now;

        await _db.Workspaces.UpdateAsync(workspace);

        return (true, null);
    }

    private async Task<WorkspaceDto> MapToDtoAsync(Workspace workspace)
    {
        var owner = await _db.Users.GetByIdAsync(workspace.OwnerId);
        var pageCount = await _db.Db.Queryable<Page>().Where(p => p.WorkspaceId == workspace.Id && !p.IsDeleted).CountAsync();

        return new WorkspaceDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            Key = workspace.Key,
            Icon = workspace.Icon,
            OwnerId = workspace.OwnerId,
            Status = workspace.Status,
            CreatedAt = workspace.CreatedAt,
            UpdatedAt = workspace.UpdatedAt,
            Owner = owner == null ? null : new UserSummaryDto
            {
                Id = owner.Id,
                Username = owner.Username,
                DisplayName = owner.DisplayName
            },
            PageCount = pageCount
        };
    }
}
