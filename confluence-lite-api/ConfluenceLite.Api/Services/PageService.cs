using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 页面服务 - Native AOT 兼容
/// </summary>
public class PageService
{
    private readonly AppDbContext _db;
    private readonly ISqlSugarClient _client;

    public PageService(AppDbContext db)
    {
        _db = db;
        _client = db.Db;
    }

    /// <summary>
    /// 创建页面
    /// </summary>
    public async Task<(PageDto? page, string? error)> CreatePageAsync(long creatorId, CreatePageRequest request)
    {
        // 验证工作空间是否存在
        var workspace = await _client.Queryable<Workspace>().InSingleAsync(request.WorkspaceId);
        if (workspace == null)
        {
            return (null, "工作空间不存在");
        }

        // 如果指定了父页面，验证是否存在
        if (request.ParentId.HasValue)
        {
            var parent = await _client.Queryable<Page>().InSingleAsync(request.ParentId.Value);
            if (parent == null || parent.WorkspaceId != request.WorkspaceId)
            {
                return (null, "父页面不存在或不属于该工作空间");
            }
        }

        var page = new Page
        {
            Title = request.Title,
            Content = request.Content,
            Status = request.Status,
            WorkspaceId = request.WorkspaceId,
            CreatorId = creatorId,
            ParentId = request.ParentId,
            SortOrder = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var pageId = await _client.Insertable(page).ExecuteReturnIdentityAsync();
        page.Id = pageId;

        return (await MapToDtoAsync(page), null);
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    public async Task<PageDto?> GetPageByIdAsync(long id)
    {
        var page = await _client.Queryable<Page>().InSingleAsync(id);
        return page == null ? null : await MapToDtoAsync(page);
    }

    /// <summary>
    /// 获取工作空间的页面列表
    /// </summary>
    public async Task<PagedResponse<PageDto>> GetPagesByWorkspaceAsync(long workspaceId, PagedRequest request)
    {
        var total = await _client.Queryable<Page>()
            .Where(p => p.WorkspaceId == workspaceId)
            .CountAsync();

        var pages = await _client.Queryable<Page>()
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderBy(p => p.SortOrder)
            .OrderByDescending(p => p.UpdatedAt)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        var dtos = new List<PageDto>();
        foreach (var page in pages)
        {
            dtos.Add(await MapToDtoAsync(page));
        }

        return new PagedResponse<PageDto>
        {
            Items = dtos,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 获取页面树（包含子页面）
    /// </summary>
    public async Task<List<PageTreeNodeDto>> GetPageTreeAsync(long workspaceId)
    {
        // 获取所有页面
        var pages = await _client.Queryable<Page>()
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        // 构建页面树
        var pageDict = new Dictionary<long, PageTreeNodeDto>();
        var rootPages = new List<PageTreeNodeDto>();

        foreach (var page in pages)
        {
            var node = new PageTreeNodeDto
            {
                Id = page.Id,
                Title = page.Title,
                ParentId = page.ParentId,
                SortOrder = page.SortOrder,
                Status = page.Status,
                Children = new List<PageTreeNodeDto>()
            };

            pageDict[page.Id] = node;

            if (page.ParentId == null)
            {
                rootPages.Add(node);
            }
        }

        // 第二轮处理，构建父子关系
        foreach (var page in pages)
        {
            if (page.ParentId.HasValue && pageDict.TryGetValue(page.ParentId.Value, out var parentNode))
            {
                if (pageDict.TryGetValue(page.Id, out var childNode))
                {
                    if (!parentNode.Children!.Contains(childNode))
                    {
                        parentNode.Children.Add(childNode);
                    }
                }
            }
        }

        return rootPages;
    }

    /// <summary>
    /// 获取子页面列表
    /// </summary>
    public async Task<List<PageDto>> GetChildPagesAsync(long parentId)
    {
        var pages = await _client.Queryable<Page>()
            .Where(p => p.ParentId == parentId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();

        var dtos = new List<PageDto>();
        foreach (var page in pages)
        {
            dtos.Add(await MapToDtoAsync(page));
        }

        return dtos;
    }

    /// <summary>
    /// 更新页面
    /// </summary>
    public async Task<(PageDto? page, string? error)> UpdatePageAsync(long id, long userId, UpdatePageRequest request)
    {
        var page = await _client.Queryable<Page>().InSingleAsync(id);
        if (page == null)
        {
            return (null, "页面不存在");
        }

        // 检查权限（创建者可以编辑）
        if (page.CreatorId != userId)
        {
            return (null, "无权限编辑此页面");
        }

        if (request.Title != null)
        {
            page.Title = request.Title;
        }

        if (request.Content != null)
        {
            page.Content = request.Content;
        }

        if (request.Status.HasValue)
        {
            page.Status = request.Status.Value;
        }

        if (request.ParentId.HasValue)
        {
            // 验证父页面
            if (request.ParentId.Value != id)
            {
                var parent = await _client.Queryable<Page>().InSingleAsync(request.ParentId.Value);
                if (parent == null || parent.WorkspaceId != page.WorkspaceId)
                {
                    return (null, "父页面不存在或不属于同一工作空间");
                }
            }
            page.ParentId = request.ParentId.Value == id ? null : request.ParentId;
        }

        if (request.SortOrder.HasValue)
        {
            page.SortOrder = request.SortOrder.Value;
        }

        page.UpdatedAt = DateTime.UtcNow;

        await _client.Updateable(page).ExecuteCommandAsync();

        return (await MapToDtoAsync(page), null);
    }

    /// <summary>
    /// 删除页面
    /// </summary>
    public async Task<(bool success, string? error)> DeletePageAsync(long id, long userId)
    {
        var page = await _client.Queryable<Page>().InSingleAsync(id);
        if (page == null)
        {
            return (false, "页面不存在");
        }

        // 检查权限
        if (page.CreatorId != userId)
        {
            return (false, "无权限删除此页面");
        }

        // 递归删除所有子页面
        await DeletePageRecursiveAsync(id);

        // 删除页面的所有评论
        await _client.Deleteable<PageComment>().Where(c => c.PageId == id).ExecuteCommandAsync();

        return (true, null);
    }

    /// <summary>
    /// 递归删除页面及其子页面
    /// </summary>
    private async Task DeletePageRecursiveAsync(long pageId)
    {
        // 获取所有子页面
        var children = await _client.Queryable<Page>()
            .Where(p => p.ParentId == pageId)
            .ToListAsync();

        foreach (var child in children)
        {
            // 递归删除子页面
            await DeletePageRecursiveAsync(child.Id);
        }

        // 删除页面
        await _client.Deleteable<Page>().In(pageId).ExecuteCommandAsync();
    }

    private async Task<PageDto> MapToDtoAsync(Page page)
    {
        var workspace = await _client.Queryable<Workspace>().InSingleAsync(page.WorkspaceId);
        var creator = await _client.Queryable<User>().InSingleAsync(page.CreatorId);

        return new PageDto
        {
            Id = page.Id,
            Title = page.Title,
            Content = page.Content,
            Status = page.Status,
            WorkspaceId = page.WorkspaceId,
            CreatorId = page.CreatorId,
            ParentId = page.ParentId,
            SortOrder = page.SortOrder,
            CreatedAt = page.CreatedAt,
            UpdatedAt = page.UpdatedAt,
            Workspace = workspace == null ? null : new WorkspaceSummaryDto
            {
                Id = workspace.Id,
                Name = workspace.Name,
                Key = workspace.Key
            },
            Creator = creator == null ? null : new UserSummaryDto
            {
                Id = creator.Id,
                Username = creator.Username,
                DisplayName = creator.DisplayName
            }
        };
    }
}
