using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Models.Confluence;

namespace ConfluenceLite.Api.Services.Confluence;

/// <summary>
/// Confluence 数据映射服务
/// </summary>
public class DataMappingService
{
    private readonly ILogger<DataMappingService> _logger;

    /// <summary>
    /// 用户ID映射：Confluence User Key -> System User ID
    /// </summary>
    private readonly Dictionary<string, long> _userKeyMap = new();

    /// <summary>
    /// 空间ID映射：Confluence Space ID -> System Workspace ID
    /// </summary>
    private readonly Dictionary<long, long> _spaceIdMap = new();

    /// <summary>
    /// 页面ID映射：Confluence Page ID -> System Page ID
    /// </summary>
    private readonly Dictionary<long, long> _pageIdMap = new();

    /// <summary>
    /// 附件ID映射：Confluence Attachment ID -> System Attachment ID
    /// </summary>
    private readonly Dictionary<long, long> _attachmentIdMap = new();

    /// <summary>
    /// 评论ID映射：Confluence Comment ID -> System Comment ID
    /// </summary>
    private readonly Dictionary<long, long> _commentIdMap = new();

    public DataMappingService(ILogger<DataMappingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 映射空间
    /// </summary>
    public Workspace MapSpace(ConfluenceSpace source, long ownerId, string? description = null)
    {
        var workspace = new Workspace
        {
            Id = source.Id,
            Name = source.Name,
            Key = source.Key,
            Description = description,
            OwnerId = ownerId,
            Status = 1,
            IsPersonal = false,
            IsDefault = false,
            IsDeleted = false,
            HomePageId = source.HomePageId,
            CreatedAt = source.CreationDate ?? DateTime.Now,
            UpdatedAt = source.LastModificationDate ?? DateTime.Now
        };

        return workspace;
    }

    /// <summary>
    /// 映射页面
    /// </summary>
    public Page MapPage(ConfluencePage source, long workspaceId, string? content = null)
    {
        // 映射用户ID
        var creatorId = MapUserKey(source.CreatorKey);
        var lastModifierId = MapUserKey(source.LastModifierKey);

        // 映射父页面ID
        var parentId = source.ParentId.HasValue ? GetMappedPageId(source.ParentId.Value) : null;

        var page = new Page
        {
            Id = source.Id,
            Title = source.Title,
            Content = content,
            Version = source.Version,
            WorkspaceId = workspaceId,
            CreatorId = creatorId,
            LastModifierId = lastModifierId,
            ParentId = parentId,
            Status = source.ContentStatus == "current" ? 1 : 0,
            IsDeleted = false,
            CreatedAt = source.CreationDate ?? DateTime.Now,
            UpdatedAt = source.LastModificationDate ?? DateTime.Now
        };

        return page;
    }

    /// <summary>
    /// 映射用户
    /// </summary>
    public User MapUser(ConfluenceUser source)
    {
        var user = new User
        {
            Username = source.Name,
            Email = source.EmailAddress,
            DisplayName = source.FullName,
            Status = source.Active ? 1 : 0,
            IsAdmin = false,
            IsDeleted = false,
            PasswordHash = "", // 导入用户需要重置密码
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        return user;
    }

    /// <summary>
    /// 映射附件
    /// </summary>
    public Attachment MapAttachment(ConfluenceAttachment source, long pageId, long creatorId, string storagePath)
    {
        var attachment = new Attachment
        {
            Id = source.Id,
            FileName = source.Title,
            FileSize = source.PageSize,
            ContentType = source.ContentType ?? "application/octet-stream",
            StoragePath = storagePath,
            PageId = pageId,
            CreatorId = creatorId,
            Version = source.Version,
            IsDeleted = false,
            CreatedAt = source.CreationDate ?? DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        return attachment;
    }

    /// <summary>
    /// 映射评论
    /// </summary>
    public PageComment MapComment(ConfluenceComment source, long pageId)
    {
        var userId = MapUserKey(source.CreatorKey);

        var comment = new PageComment
        {
            Id = source.Id,
            Content = source.Content,
            PageId = pageId,
            UserId = userId,
            CreatedAt = source.CreationDate ?? DateTime.Now,
            UpdatedAt = source.LastModificationDate ?? DateTime.Now
        };

        return comment;
    }

    /// <summary>
    /// 添加用户映射
    /// </summary>
    public void AddUserMapping(string confluenceKey, long systemUserId)
    {
        _userKeyMap[confluenceKey] = systemUserId;
    }

    /// <summary>
    /// 添加空间映射
    /// </summary>
    public void AddSpaceMapping(long confluenceId, long systemId)
    {
        _spaceIdMap[confluenceId] = systemId;
    }

    /// <summary>
    /// 添加页面映射
    /// </summary>
    public void AddPageMapping(long confluenceId, long systemId)
    {
        _pageIdMap[confluenceId] = systemId;
    }

    /// <summary>
    /// 添加附件映射
    /// </summary>
    public void AddAttachmentMapping(long confluenceId, long systemId)
    {
        _attachmentIdMap[confluenceId] = systemId;
    }

    /// <summary>
    /// 添加评论映射
    /// </summary>
    public void AddCommentMapping(long confluenceId, long systemId)
    {
        _commentIdMap[confluenceId] = systemId;
    }

    /// <summary>
    /// 映射用户键到系统用户ID
    /// </summary>
    public long MapUserKey(string? confluenceKey)
    {
        if (string.IsNullOrEmpty(confluenceKey))
            return 1; // 默认用户

        if (_userKeyMap.TryGetValue(confluenceKey, out var userId))
            return userId;

        _logger.LogWarning("未找到用户映射: {Key}", confluenceKey);
        return 1; // 默认用户
    }

    /// <summary>
    /// 获取映射后的空间ID
    /// </summary>
    public long? GetMappedSpaceId(long confluenceSpaceId)
    {
        if (_spaceIdMap.TryGetValue(confluenceSpaceId, out var workspaceId))
            return workspaceId;

        _logger.LogWarning("未找到空间映射: {SpaceId}", confluenceSpaceId);
        return null;
    }

    /// <summary>
    /// 获取映射后的页面ID
    /// </summary>
    public long? GetMappedPageId(long confluencePageId)
    {
        if (_pageIdMap.TryGetValue(confluencePageId, out var pageId))
            return pageId;

        _logger.LogWarning("未找到页面映射: {PageId}", confluencePageId);
        return null;
    }

    /// <summary>
    /// 获取映射后的附件ID
    /// </summary>
    public long? GetMappedAttachmentId(long confluenceAttachmentId)
    {
        if (_attachmentIdMap.TryGetValue(confluenceAttachmentId, out var attachmentId))
            return attachmentId;

        return null;
    }

    /// <summary>
    /// 获取映射后的评论ID
    /// </summary>
    public long? GetMappedCommentId(long confluenceCommentId)
    {
        if (_commentIdMap.TryGetValue(confluenceCommentId, out var commentId))
            return commentId;

        return null;
    }

    /// <summary>
    /// 获取所有映射统计
    /// </summary>
    public Dictionary<string, int> GetMappingStatistics()
    {
        return new Dictionary<string, int>
        {
            ["Users"] = _userKeyMap.Count,
            ["Spaces"] = _spaceIdMap.Count,
            ["Pages"] = _pageIdMap.Count,
            ["Attachments"] = _attachmentIdMap.Count,
            ["Comments"] = _commentIdMap.Count
        };
    }

    /// <summary>
    /// 获取页面 ID 映射副本
    /// </summary>
    public Dictionary<long, long> GetPageIdMap()
    {
        return new Dictionary<long, long>(_pageIdMap);
    }
}
