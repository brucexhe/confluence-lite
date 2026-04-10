using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 评论服务 - Native AOT 兼容
/// </summary>
public class CommentService
{
    private readonly AppDbContext _db;
    private readonly ISqlSugarClient _client;

    public CommentService(AppDbContext db)
    {
        _db = db;
        _client = db.Db;
    }

    /// <summary>
    /// 创建评论
    /// </summary>
    public async Task<(CommentDto? comment, string? error)> CreateCommentAsync(long userId, CreateCommentRequest request)
    {
        // 验证页面是否存在
        var page = await _client.Queryable<Page>().InSingleAsync(request.PageId);
        if (page == null)
        {
            return (null, "页面不存在");
        }

        // 如果指定了父评论，验证是否存在
        if (request.ParentId.HasValue)
        {
            var parent = await _client.Queryable<PageComment>().InSingleAsync(request.ParentId.Value);
            if (parent == null || parent.PageId != request.PageId)
            {
                return (null, "父评论不存在或不属于该页面");
            }
        }

        var comment = new PageComment
        {
            PageId = request.PageId,
            UserId = userId,
            Content = request.Content,
            ParentId = request.ParentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var commentId = await _client.Insertable(comment).ExecuteReturnIdentityAsync();
        comment.Id = commentId;

        return (await MapToDtoAsync(comment), null);
    }

    /// <summary>
    /// 获取评论信息
    /// </summary>
    public async Task<CommentDto?> GetCommentByIdAsync(long id)
    {
        var comment = await _client.Queryable<PageComment>().InSingleAsync(id);
        return comment == null ? null : await MapToDtoAsync(comment);
    }

    /// <summary>
    /// 获取页面的评论列表
    /// </summary>
    public async Task<List<CommentDto>> GetCommentsByPageAsync(long pageId)
    {
        var comments = await _client.Queryable<PageComment>()
            .Where(c => c.PageId == pageId && c.ParentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var dtos = new List<CommentDto>();
        foreach (var comment in comments)
        {
            var dto = await MapToDtoAsync(comment);
            // 加载回复
            var replies = await _client.Queryable<PageComment>()
                .Where(c => c.ParentId == comment.Id)
                .OrderBy(c => c.CreatedAt)
                .ToListAsync();

            dto.Replies = new List<CommentDto>();
            foreach (var reply in replies)
            {
                dto.Replies.Add(await MapToDtoAsync(reply));
            }

            dtos.Add(dto);
        }

        return dtos;
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    public async Task<(CommentDto? comment, string? error)> UpdateCommentAsync(long id, long userId, UpdateCommentRequest request)
    {
        var comment = await _client.Queryable<PageComment>().InSingleAsync(id);
        if (comment == null)
        {
            return (null, "评论不存在");
        }

        // 检查权限
        if (comment.UserId != userId)
        {
            return (null, "无权限编辑此评论");
        }

        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        await _client.Updateable(comment).ExecuteCommandAsync();

        return (await MapToDtoAsync(comment), null);
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    public async Task<(bool success, string? error)> DeleteCommentAsync(long id, long userId)
    {
        var comment = await _client.Queryable<PageComment>().InSingleAsync(id);
        if (comment == null)
        {
            return (false, "评论不存在");
        }

        // 检查权限
        if (comment.UserId != userId)
        {
            return (false, "无权限删除此评论");
        }

        // 删除所有回复
        await _client.Deleteable<PageComment>().Where(c => c.ParentId == id).ExecuteCommandAsync();

        // 删除评论
        await _client.Deleteable<PageComment>().In(id).ExecuteCommandAsync();

        return (true, null);
    }

    private async Task<CommentDto> MapToDtoAsync(PageComment comment)
    {
        var user = await _client.Queryable<User>().InSingleAsync(comment.UserId);

        return new CommentDto
        {
            Id = comment.Id,
            PageId = comment.PageId,
            UserId = comment.UserId,
            Content = comment.Content,
            ParentId = comment.ParentId,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            User = user == null ? null : new UserSummaryDto
            {
                Id = user.Id,
                Username = user.Username,
                DisplayName = user.DisplayName
            }
        };
    }
}
