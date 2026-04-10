using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 工作空间服务 - Native AOT 兼容
/// </summary>
public class WorkspaceService
{
    private readonly AppDbContext _db;
    private readonly ISqlSugarClient _client;

    public WorkspaceService(AppDbContext db)
    {
        _db = db;
        _client = db.Db;
    }

    /// <summary>
    /// 创建工作空间
    /// </summary>
    public async Task<(WorkspaceDto? workspace, string? error)> CreateWorkspaceAsync(long ownerId, CreateWorkspaceRequest request)
    {
        // 检查Key是否已存在
        var exists = await _client.Queryable<Workspace>()
            .Where(w => w.Key == request.Key)
            .AnyAsync();

        if (exists)
        {
            return (null, "工作空间标识已存在");
        }

        var workspace = new Workspace
        {
            Name = request.Name,
            Description = request.Description,
            Key = request.Key,
            OwnerId = ownerId,
            Status = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var workspaceId = await _client.Insertable(workspace).ExecuteReturnIdentityAsync();
        workspace.Id = workspaceId;

        return (await MapToDtoAsync(workspace), null);
    }

    /// <summary>
    /// 获取工作空间信息
    /// </summary>
    public async Task<WorkspaceDto?> GetWorkspaceByIdAsync(long id)
    {
        var workspace = await _client.Queryable<Workspace>().InSingleAsync(id);
        return workspace == null ? null : await MapToDtoAsync(workspace);
    }

    /// <summary>
    /// 根据Key获取工作空间
    /// </summary>
    public async Task<WorkspaceDto?> GetWorkspaceByKeyAsync(string key)
    {
        var workspace = await _client.Queryable<Workspace>()
            .Where(w => w.Key == key)
            .FirstAsync();

        return workspace == null ? null : await MapToDtoAsync(workspace);
    }

    /// <summary>
    /// 获取工作空间列表
    /// </summary>
    public async Task<PagedResponse<WorkspaceDto>> GetWorkspaceListAsync(PagedRequest request)
    {
        var total = await _client.Queryable<Workspace>().CountAsync();

        var workspaces = await _client.Queryable<Workspace>()
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
        var workspaces = await _client.Queryable<Workspace>()
            .Where(w => w.OwnerId == userId)
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
        var workspace = await _client.Queryable<Workspace>().InSingleAsync(id);
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

        workspace.UpdatedAt = DateTime.UtcNow;

        await _client.Updateable(workspace).ExecuteCommandAsync();

        return (await MapToDtoAsync(workspace), null);
    }

    /// <summary>
    /// 删除工作空间
    /// </summary>
    public async Task<(bool success, string? error)> DeleteWorkspaceAsync(long id, long userId)
    {
        var workspace = await _client.Queryable<Workspace>().InSingleAsync(id);
        if (workspace == null)
        {
            return (false, "工作空间不存在");
        }

        // 检查权限
        if (workspace.OwnerId != userId)
        {
            return (false, "无权限删除此工作空间");
        }

        // 删除工作空间的所有页面
        await _client.Deleteable<Page>().Where(p => p.WorkspaceId == id).ExecuteCommandAsync();

        // 删除工作空间
        await _client.Deleteable<Workspace>().In(id).ExecuteCommandAsync();

        return (true, null);
    }

    private async Task<WorkspaceDto> MapToDtoAsync(Workspace workspace)
    {
        var owner = await _client.Queryable<User>().InSingleAsync(workspace.OwnerId);
        var pageCount = await _client.Queryable<Page>().Where(p => p.WorkspaceId == workspace.Id).CountAsync();

        return new WorkspaceDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            Key = workspace.Key,
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
