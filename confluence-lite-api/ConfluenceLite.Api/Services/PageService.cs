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

    public PageService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 创建页面
    /// </summary>
    public async Task<(PageDto? page, string? error)> CreatePageAsync(long creatorId, CreatePageRequest request)
    {
        // 验证工作空间是否存在
        var workspace = await _db.Workspaces.GetByIdAsync(request.WorkspaceId);
        if (workspace == null)
        {
            return (null, "工作空间不存在");
        }

        // 如果指定了父页面，验证是否存在
        if (request.ParentId.HasValue)
        {
            var parent = await _db.Pages.GetByIdAsync(request.ParentId.Value);
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
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var pageId = await _db.Db.Insertable(page).ExecuteReturnIdentityAsync();
        page.Id = pageId;

        // 保存初始版本
        await SaveVersionAsync(pageId, page, creatorId);

        return (await MapToDtoAsync(page), null);
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    public async Task<PageDto?> GetPageByIdAsync(long id)
    {
        var page = await _db.Pages.GetByIdAsync(id);
        return page == null ? null : await MapToDtoAsync(page);
    }

    /// <summary>
    /// 获取工作空间的页面列表
    /// </summary>
    public async Task<PagedResponse<PageDto>> GetPagesByWorkspaceAsync(long workspaceId, PagedRequest request)
    {
        var total = await _db.Db.Queryable<Page>()
            .Where(p => p.WorkspaceId == workspaceId)
            .CountAsync();

        var pages = await _db.Db.Queryable<Page>()
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
        var pages = await _db.Db.Queryable<Page>()
            .Where(p => p.WorkspaceId == workspaceId)
            .OrderBy(p => p.SortOrder)
            .OrderBy(p => p.Title)
            .ToListAsync();

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
        var pages = await _db.Db.Queryable<Page>()
            .Where(p => p.ParentId == parentId)
            .OrderBy(p => p.SortOrder)
            .OrderBy(p => p.Id)
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
        var page = await _db.Pages.GetByIdAsync(id);
        if (page == null)
        {
            return (null, "页面不存在");
        }

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
            if (request.ParentId.Value != id)
            {
                var parent = await _db.Pages.GetByIdAsync(request.ParentId.Value);
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

        page.UpdatedAt = DateTime.Now;

        // 保存更新前版本
        await SaveVersionAsync(id, page, userId);

        await _db.Pages.UpdateAsync(page);

        return (await MapToDtoAsync(page), null);
    }

    /// <summary>
    /// 删除页面
    /// </summary>
    public async Task<(bool success, string? error)> DeletePageAsync(long id, long userId)
    {
        var page = await _db.Pages.GetByIdAsync(id);
        if (page == null)
        {
            return (false, "页面不存在");
        }

        if (page.CreatorId != userId)
        {
            return (false, "无权限删除此页面");
        }

        await DeletePageRecursiveAsync(id);

        // 删除页面的所有评论
        await _db.Db.Deleteable<PageComment>().Where(c => c.PageId == id).ExecuteCommandAsync();

        return (true, null);
    }

    /// <summary>
    /// 递归删除页面及其子页面
    /// </summary>
    private async Task DeletePageRecursiveAsync(long pageId)
    {
        var children = await _db.Db.Queryable<Page>()
            .Where(p => p.ParentId == pageId)
            .OrderBy(p => p.Id)
            .ToListAsync();

        foreach (var child in children)
        {
            await DeletePageRecursiveAsync(child.Id);
        }

        await _db.Db.Deleteable<Page>().In(pageId).ExecuteCommandAsync();
        // 删除页面的所有版本
        await _db.Db.Deleteable<PageVersion>().Where(v => v.PageId == pageId).ExecuteCommandAsync();
    }

    /// <summary>
    /// 保存页面版本快照
    /// </summary>
    private async Task SaveVersionAsync(long pageId, Page page, long editorId)
    {
        // 获取当前最大版本号
        var maxVersion = await _db.Db.Queryable<PageVersion>()
            .Where(v => v.PageId == pageId)
            .MaxAsync<int?>(v => v.VersionNumber);

        var version = new PageVersion
        {
            PageId = pageId,
            VersionNumber = (maxVersion ?? 0) + 1,
            Title = page.Title,
            Content = page.Content,
            EditorId = editorId,
            CreatedAt = DateTime.Now
        };

        await _db.Db.Insertable(version).ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取页面版本列表
    /// </summary>
    public async Task<List<PageVersionListDto>> GetPageVersionsAsync(long pageId)
    {
        var versions = await _db.Db.Queryable<PageVersion>()
            .Where(v => v.PageId == pageId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();

        var dtos = new List<PageVersionListDto>();
        foreach (var v in versions)
        {
            var editor = await _db.Users.GetByIdAsync(v.EditorId);
            dtos.Add(new PageVersionListDto
            {
                Id = v.Id,
                PageId = v.PageId,
                VersionNumber = v.VersionNumber,
                Title = v.Title,
                ChangeComment = v.ChangeComment,
                EditorId = v.EditorId,
                CreatedAt = v.CreatedAt,
                Editor = editor == null ? null : new UserSummaryDto
                {
                    Id = editor.Id,
                    Username = editor.Username,
                    DisplayName = editor.DisplayName
                }
            });
        }
        return dtos;
    }

    /// <summary>
    /// 获取单个版本详情
    /// </summary>
    public async Task<PageVersionDto?> GetPageVersionAsync(long versionId)
    {
        var v = await _db.PageVersions.GetByIdAsync(versionId);
        if (v == null) return null;

        var editor = await _db.Users.GetByIdAsync(v.EditorId);
        return new PageVersionDto
        {
            Id = v.Id,
            PageId = v.PageId,
            VersionNumber = v.VersionNumber,
            Title = v.Title,
            Content = v.Content,
            ChangeComment = v.ChangeComment,
            EditorId = v.EditorId,
            CreatedAt = v.CreatedAt,
            Editor = editor == null ? null : new UserSummaryDto
            {
                Id = editor.Id,
                Username = editor.Username,
                DisplayName = editor.DisplayName
            }
        };
    }

    /// <summary>
    /// 删除页面版本
    /// </summary>
    public async Task<(bool success, string? error)> DeletePageVersionAsync(long versionId)
    {
        var version = await _db.PageVersions.GetByIdAsync(versionId);
        if (version == null)
        {
            return (false, "版本不存在");
        }

        await _db.PageVersions.DeleteAsync(version);
        return (true, null);
    }

    private async Task<PageDto> MapToDtoAsync(Page page)
    {
        var workspace = await _db.Workspaces.GetByIdAsync(page.WorkspaceId);
        var creator = await _db.Users.GetByIdAsync(page.CreatorId);

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
                Key = workspace.Key,
                Icon = workspace.Icon
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
