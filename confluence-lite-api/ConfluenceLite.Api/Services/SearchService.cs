using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

public class SearchService
{
    private readonly AppDbContext _db;

    public SearchService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取搜索建议 (用于下拉框)
    /// </summary>
    public async Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<SearchSuggestionDto>();

        // 简单的标题匹配，用于快速响应建议
        var pages = await _db.Db.Queryable<Page, Workspace>((p, w) => p.WorkspaceId == w.Id)
            .Where((p, w) => p.IsDeleted == false && p.Title.Contains(query))
            .Take(5)
            .Select((p, w) => new SearchSuggestionDto
            {
                Id = p.Id,
                Title = p.Title,
                Type = "page",
                SpaceKey = w.Key
            })
            .ToListAsync();

        var attachments = await _db.Db.Queryable<Attachment, Page, Workspace>((a, p, w) => a.PageId == p.Id && p.WorkspaceId == w.Id)
            .Where((a, p, w) => a.IsDeleted == false && a.FileName.Contains(query))
            .Take(5)
            .Select((a, p, w) => new SearchSuggestionDto
            {
                Id = a.Id,
                Title = a.FileName,
                Type = "attachment",
                ContentType = a.ContentType,
                SpaceKey = w.Key
            })
            .ToListAsync();

        return pages.Concat(attachments).ToList();
    }

    /// <summary>
    /// 全局搜索 (用于搜索结果页)
    /// </summary>
    public async Task<List<SearchResultDto>> SearchAllAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<SearchResultDto>();

        // PostgreSQL FTS 搜索
        // 搜索页面 (标题加权 A, 内容加权 B)
        var sqlPages = @"
            SELECT 
                p.id as Id, 
                ts_headline('simple', p.title, plainto_tsquery('simple', @query), 'StartSel=<mark>, StopSel=</mark>, MaxWords=35, MinWords=15') as Title,
                ts_headline('simple', p.content, plainto_tsquery('simple', @query), 'StartSel=<mark>, StopSel=</mark>, MaxWords=35, MinWords=15') as Content,
                'page' as Type,
                w.name as SpaceName,
                w.key as SpaceKey,
                p.updatedat as UpdatedAt,
                u.displayname as CreatorName
            FROM pages p
            JOIN workspaces w ON p.workspaceid = w.id
            JOIN users u ON p.creatorid = u.id
            WHERE p.isdeleted = false 
            AND (
                to_tsvector('simple', p.title || ' ' || COALESCE(p.content, '')) @@ plainto_tsquery('simple', @query)
            )
            ORDER BY ts_rank(to_tsvector('simple', p.title || ' ' || COALESCE(p.content, '')), plainto_tsquery('simple', @query)) DESC
            LIMIT 50";

        var pageResults = await _db.Db.Ado.SqlQueryAsync<SearchResultDto>(sqlPages, new { query });

        // 搜索附件
        var sqlAttachments = @"
            SELECT 
                a.id as Id, 
                ts_headline('simple', a.filename, plainto_tsquery('simple', @query), 'StartSel=<mark>, StopSel=</mark>, MaxWords=35, MinWords=15') as Title,
                ts_headline('simple', a.comment, plainto_tsquery('simple', @query), 'StartSel=<mark>, StopSel=</mark>, MaxWords=35, MinWords=15') as Content,
                'attachment' as Type,
                a.contenttype as ContentType,
                w.name as SpaceName,
                w.key as SpaceKey,
                a.updatedat as UpdatedAt,
                u.displayname as CreatorName
            FROM attachments a
            JOIN pages p ON a.pageid = p.id
            JOIN workspaces w ON p.workspaceid = w.id
            JOIN users u ON a.creatorid = u.id
            WHERE a.isdeleted = false 
            AND (
                to_tsvector('simple', a.filename || ' ' || COALESCE(a.comment, '')) @@ plainto_tsquery('simple', @query)
            )
            LIMIT 20";

        var attachmentResults = await _db.Db.Ado.SqlQueryAsync<SearchResultDto>(sqlAttachments, new { query });

        var allResults = pageResults.Concat(attachmentResults)
            .OrderByDescending(r => r.UpdatedAt)
            .ToList();

        return allResults;
    }
    
    /// <summary>
    /// 初始化搜索索引 (由开发者或系统管理调用)
    /// </summary>
    public async Task EnsureIndexesAsync()
    {
        // 创建 GIN 索引以加速全文检索
        await _db.Db.Ado.ExecuteCommandAsync(@"
            CREATE INDEX IF NOT EXISTS idx_pages_fts ON pages USING GIN (to_tsvector('simple', title || ' ' || COALESCE(content, '')));
            CREATE INDEX IF NOT EXISTS idx_attachments_fts ON attachments USING GIN (to_tsvector('simple', filename || ' ' || COALESCE(comment, '')));
        ");
    }
}
