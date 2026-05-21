using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

public class RecentService
{
    private readonly AppDbContext _db;

    public RecentService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 添加或更新最近访问记录
    /// </summary>
    public async Task AddRecentAsync(long userId, long pageId)
    {
        var existing = await _db.Db.Queryable<Recent>()
            .Where(r => r.UserId == userId && r.PageId == pageId)
            .FirstAsync();

        if (existing != null)
        {
            existing.VisitedAt = DateTime.Now;
            await _db.Db.Updateable(existing).ExecuteCommandAsync();
        }
        else
        {
            var recent = new Recent
            {
                UserId = userId,
                PageId = pageId,
                VisitedAt = DateTime.Now
            };
            await _db.Db.Insertable(recent).ExecuteCommandAsync();
        }

        // 可选：限制每个用户的最近记录数量，例如 50 条
        await CleanupOldRecentsAsync(userId, 50);
    }

    /// <summary>
    /// 获取用户的最近访问列表
    /// </summary>
    public async Task<List<RecentDto>> GetRecentPagesAsync(long userId)
    {
        // 连表查询页面和空间信息
        return await _db.Db.Queryable<Recent>()
            .LeftJoin<Page>((r, p) => r.PageId == p.Id)
            .LeftJoin<Workspace>((r, p, w) => p.WorkspaceId == w.Id)
            .LeftJoin<User>((r, p, w, u) => p.CreatorId == u.Id)
            .Where((r, p, w, u) => r.UserId == userId && p.IsDeleted == false)
            .OrderBy((r, p, w, u) => r.VisitedAt, OrderByType.Desc)
            .Take(20)
            .Select((r, p, w, u) => new RecentDto
            {
                PageId = r.PageId,
                Title = p.Title,
                SpaceKey = w.Key,
                SpaceName = w.Name,
                CreatorName = u.DisplayName ?? u.Username,
                VisitedAt = r.VisitedAt
            })
            .ToListAsync();
    }

    private async Task CleanupOldRecentsAsync(long userId, int limit)
    {
        var count = await _db.Db.Queryable<Recent>().Where(r => r.UserId == userId).CountAsync();
        if (count > limit)
        {
            var oldestId = await _db.Db.Queryable<Recent>()
                .Where(r => r.UserId == userId)
                .OrderBy(r => r.VisitedAt, OrderByType.Desc)
                .Skip(limit - 1)
                .Select(r => r.Id)
                .FirstAsync();

            if (oldestId > 0)
            {
                await _db.Db.Deleteable<Recent>()
                    .Where(r => r.UserId == userId && r.VisitedAt < _db.Db.Queryable<Recent>().Where(x => x.Id == oldestId).Select(x => x.VisitedAt).First())
                    .ExecuteCommandAsync();
            }
        }
    }
}
