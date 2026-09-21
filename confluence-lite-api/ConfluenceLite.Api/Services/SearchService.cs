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
    /// 空间可见性过滤：仅搜索自己拥有或加入（有 ViewSpace 权限）的空间；系统管理员不受限
    /// </summary>
    private string GetSpaceVisibilityFilter(bool isSiteAdmin)
    {
        if (isSiteAdmin) return string.Empty;
        return """
            AND (
                w.ownerid = @userId
                OR w.id IN (
                    SELECT workspaceid FROM workspace_permissions
                    WHERE targettype = 2 AND targetid = @userId AND viewspace = true
                )
            )
            """;
    }

    /// <summary>
    /// 获取搜索建议 (用于下拉框)
    /// </summary>
    public async Task<List<SearchSuggestionDto>> GetSuggestionsAsync(string query, long userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<SearchSuggestionDto>();

        var permFilter = GetSpaceVisibilityFilter(isSiteAdmin);

        // 简单的标题匹配，用于快速响应建议
        var pages = await _db.Db.Ado.SqlQueryAsync<SearchSuggestionDto>($"""
            SELECT
                p.id as Id,
                p.title as Title,
                'page' as Type,
                w.key as SpaceKey
            FROM pages p
            JOIN workspaces w ON p.workspaceid = w.id
            WHERE p.isdeleted = false
            AND w.isdeleted = false
            AND p.title ILIKE '%' || @query || '%'
            {permFilter}
            LIMIT 5
            """, new { query, userId });

        var attachments = await _db.Db.Ado.SqlQueryAsync<SearchSuggestionDto>($"""
            SELECT
                a.id as Id,
                a.filename as Title,
                'attachment' as Type,
                a.contenttype as ContentType,
                w.key as SpaceKey
            FROM attachments a
            JOIN pages p ON a.pageid = p.id
            JOIN workspaces w ON p.workspaceid = w.id
            WHERE a.isdeleted = false
            AND p.isdeleted = false
            AND w.isdeleted = false
            AND a.filename ILIKE '%' || @query || '%'
            {permFilter}
            LIMIT 5
            """, new { query, userId });

        return pages.Concat(attachments).ToList();
    }

    /// <summary>
    /// 全局搜索 (用于搜索结果页)
    /// </summary>
    public async Task<List<SearchResultDto>> SearchAllAsync(string query, long userId, bool isSiteAdmin)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<SearchResultDto>();

        var permFilter = GetSpaceVisibilityFilter(isSiteAdmin);

        // PostgreSQL FTS 搜索
        // 搜索页面 (标题加权 A, 内容加权 B)
        var sqlPages = $"""
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
            AND w.isdeleted = false
            AND (
                to_tsvector('simple', p.title || ' ' || COALESCE(p.content, '')) @@ plainto_tsquery('simple', @query)
            )
            {permFilter}
            ORDER BY ts_rank(to_tsvector('simple', p.title || ' ' || COALESCE(p.content, '')), plainto_tsquery('simple', @query)) DESC
            LIMIT 50
            """;

        var pageResults = await _db.Db.Ado.SqlQueryAsync<SearchResultDto>(sqlPages, new { query, userId });

        // 搜索附件
        var sqlAttachments = $"""
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
            AND p.isdeleted = false
            AND w.isdeleted = false
            AND (
                to_tsvector('simple', a.filename || ' ' || COALESCE(a.comment, '')) @@ plainto_tsquery('simple', @query)
            )
            {permFilter}
            LIMIT 20
            """;

        var attachmentResults = await _db.Db.Ado.SqlQueryAsync<SearchResultDto>(sqlAttachments, new { query, userId });

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
