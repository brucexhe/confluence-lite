using System.Security.Cryptography;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 分享服务
/// </summary>
public class ShareService
{
    private readonly AppDbContext _db;

    public ShareService(AppDbContext db)
    {
        _db = db;
    }

    private static string GenerateCode(int length = 16)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = RandomNumberGenerator.GetBytes(length);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }

    private async Task<Share?> GetShareByCodeAsync(string code)
    {
        return await _db.Db.Queryable<Share>().FirstAsync(s => s.Code == code);
    }

    /// <summary>
    /// 创建分享
    /// </summary>
    public async Task<(ShareDto? share, string? error)> CreateShareAsync(long userId, CreateShareRequest request)
    {
        var page = await _db.Pages.GetByIdAsync(request.PageId);
        if (page == null || page.IsDeleted)
            return (null, "页面不存在");

        var share = new Share
        {
            Code = GenerateCode(),
            PageId = request.PageId,
            SharedById = userId,
            SharedWithId = request.SharedWithId,
            VisitPassword = !string.IsNullOrEmpty(request.VisitPassword)
                ? PasswordService.HashPassword(request.VisitPassword)
                : null,
            AllowEdit = request.AllowEdit,
            ExpireAt = request.ExpireAt,
            IsRead = false,
            CreatedAt = DateTime.Now
        };

        var id = await _db.Db.Insertable(share).ExecuteReturnIdentityAsync();
        share.Id = id;

        return (await MapToDtoAsync(share), null);
    }

    /// <summary>
    /// 获取公开分享信息
    /// </summary>
    public async Task<PublicShareInfoDto?> GetPublicShareInfoAsync(string code)
    {
        var share = await GetShareByCodeAsync(code);
        if (share == null) return null;

        var page = await _db.Pages.GetByIdAsync(share.PageId);
        var sharedBy = await _db.Users.GetByIdAsync(share.SharedById);

        return new PublicShareInfoDto
        {
            HasPassword = !string.IsNullOrEmpty(share.VisitPassword),
            AllowEdit = share.AllowEdit,
            IsExpired = share.ExpireAt.HasValue && share.ExpireAt.Value < DateTime.Now,
            IsUserSpecific = share.SharedWithId.HasValue,
            SharedByDisplayName = sharedBy?.DisplayName ?? sharedBy?.Username,
            PageTitle = page?.Title
        };
    }

    /// <summary>
    /// 通过分享获取页面内容
    /// </summary>
    public async Task<(PageDto? page, string? error)> GetSharePageContentAsync(
        string code, string? password, CurrentUser? currentUser)
    {
        var share = await GetShareByCodeAsync(code);
        if (share == null)
            return (null, "分享不存在");

        if (share.ExpireAt.HasValue && share.ExpireAt.Value < DateTime.Now)
            return (null, "分享已过期");

        if (share.SharedWithId.HasValue)
        {
            if (currentUser == null || !currentUser.IsAuthenticated)
                return (null, "没有访问权限");

            if (currentUser.UserId != share.SharedWithId.Value && currentUser.UserId != share.SharedById)
                return (null, "没有访问权限");
        }

        if (!string.IsNullOrEmpty(share.VisitPassword))
        {
            if (string.IsNullOrEmpty(password))
                return (null, "需要密码验证");

            if (!PasswordService.VerifyPassword(password, share.VisitPassword))
                return (null, "密码错误");
        }

        var page = await _db.Pages.GetByIdAsync(share.PageId);
        if (page == null || page.IsDeleted)
            return (null, "页面不存在");

        return (await MapPageToDtoAsync(page), null);
    }

    /// <summary>
    /// 通过分享更新页面内容
    /// </summary>
    public async Task<(PageDto? page, string? error)> UpdatePageViaShareAsync(
        string code, string? password, CurrentUser? currentUser, UpdateSharePageRequest request)
    {
        var share = await GetShareByCodeAsync(code);
        if (share == null)
            return (null, "分享不存在");

        if (!share.AllowEdit)
            return (null, "此分享不允许编辑");

        if (share.ExpireAt.HasValue && share.ExpireAt.Value < DateTime.Now)
            return (null, "分享已过期");

        if (share.SharedWithId.HasValue)
        {
            if (currentUser == null || !currentUser.IsAuthenticated)
                return (null, "没有访问权限");

            if (currentUser.UserId != share.SharedWithId.Value && currentUser.UserId != share.SharedById)
                return (null, "没有访问权限");
        }

        if (!string.IsNullOrEmpty(share.VisitPassword))
        {
            if (string.IsNullOrEmpty(password))
                return (null, "需要密码验证");

            if (!PasswordService.VerifyPassword(password, share.VisitPassword))
                return (null, "密码错误");
        }

        var page = await _db.Pages.GetByIdAsync(share.PageId);
        if (page == null || page.IsDeleted)
            return (null, "页面不存在");

        page.Title = request.Title;
        page.Content = request.Content;
        page.UpdatedAt = DateTime.Now;

        await _db.Db.Updateable(page).ExecuteCommandAsync();

        return (await MapPageToDtoAsync(page), null);
    }

    /// <summary>
    /// 获取页面的分享列表
    /// </summary>
    public async Task<List<ShareDto>> GetSharesByPageAsync(long pageId, long userId)
    {
        var page = await _db.Pages.GetByIdAsync(pageId);
        if (page == null || page.CreatorId != userId)
            return new List<ShareDto>();

        var shares = await _db.Db.Queryable<Share>()
            .Where(s => s.PageId == pageId)
            .OrderBy(s => s.CreatedAt, SqlSugar.OrderByType.Desc)
            .ToListAsync();

        var result = new List<ShareDto>();
        foreach (var share in shares)
        {
            result.Add(await MapToDtoAsync(share));
        }
        return result;
    }

    /// <summary>
    /// 获取我创建的所有分享
    /// </summary>
    public async Task<List<ShareDto>> GetMySharesAsync(long userId)
    {
        var shares = await _db.Db.Queryable<Share>()
            .Where(s => s.SharedById == userId)
            .OrderBy(s => s.CreatedAt, SqlSugar.OrderByType.Desc)
            .ToListAsync();

        var result = new List<ShareDto>();
        foreach (var share in shares)
        {
            result.Add(await MapToDtoAsync(share));
        }
        return result;
    }

    /// <summary>
    /// 更新分享设置
    /// </summary>
    public async Task<(ShareDto? share, string? error)> UpdateShareAsync(long id, long userId, UpdateShareRequest request)
    {
        var share = await _db.Shares.GetByIdAsync(id);
        if (share == null)
            return (null, "分享不存在");

        if (share.SharedById != userId)
        {
            var page = await _db.Pages.GetByIdAsync(share.PageId);
            if (page == null || page.CreatorId != userId)
                return (null, "没有权限修改此分享");
        }

        if (request.ExpireAt.HasValue)
            share.ExpireAt = request.ExpireAt;
        if (request.AllowEdit.HasValue)
            share.AllowEdit = request.AllowEdit.Value;
        if (request.VisitPassword == "")
            share.VisitPassword = null;
        else if (request.VisitPassword != null)
            share.VisitPassword = PasswordService.HashPassword(request.VisitPassword);

        await _db.Db.Updateable(share)
            .UpdateColumns(s => new { s.ExpireAt, s.AllowEdit, s.VisitPassword })
            .ExecuteCommandAsync();

        return (await MapToDtoAsync(share), null);
    }

    /// <summary>
    /// 删除分享
    /// </summary>
    public async Task<(bool success, string? error)> DeleteShareAsync(long id, long userId)
    {
        var share = await _db.Shares.GetByIdAsync(id);
        if (share == null)
            return (false, "分享不存在");

        if (share.SharedById != userId)
        {
            var page = await _db.Pages.GetByIdAsync(share.PageId);
            if (page == null || page.CreatorId != userId)
                return (false, "没有权限删除此分享");
        }

        await _db.Db.Deleteable(share).ExecuteCommandAsync();
        return (true, null);
    }

    private async Task<ShareDto> MapToDtoAsync(Share share)
    {
        var dto = new ShareDto
        {
            Id = share.Id,
            Code = share.Code,
            PageId = share.PageId,
            SharedById = share.SharedById,
            SharedWithId = share.SharedWithId,
            HasPassword = !string.IsNullOrEmpty(share.VisitPassword),
            AllowEdit = share.AllowEdit,
            IsExpired = share.ExpireAt.HasValue && share.ExpireAt.Value < DateTime.Now,
            CreatedAt = share.CreatedAt,
            ExpireAt = share.ExpireAt
        };

        var sharedBy = await _db.Users.GetByIdAsync(share.SharedById);
        dto.SharedBy = sharedBy != null ? new UserSummaryDto
        {
            Id = sharedBy.Id,
            Username = sharedBy.Username,
            DisplayName = sharedBy.DisplayName
        } : null;

        if (share.SharedWithId.HasValue)
        {
            var sharedWith = await _db.Users.GetByIdAsync(share.SharedWithId.Value);
            dto.SharedWith = sharedWith != null ? new UserSummaryDto
            {
                Id = sharedWith.Id,
                Username = sharedWith.Username,
                DisplayName = sharedWith.DisplayName
            } : null;
        }

        var page = await _db.Pages.GetByIdAsync(share.PageId);
        dto.Page = page != null ? new SharePageInfoDto
        {
            Id = page.Id,
            Title = page.Title
        } : null;

        return dto;
    }

    private async Task<PageDto> MapPageToDtoAsync(Page page)
    {
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
            Creator = creator != null ? new UserSummaryDto
            {
                Id = creator.Id,
                Username = creator.Username,
                DisplayName = creator.DisplayName
            } : null
        };
    }
}
