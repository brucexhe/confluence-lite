using System.Net;
using System.Text.RegularExpressions;
using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 页面任务服务 - 从页面内容提取任务并保持与 page_tasks 表同步
/// 内容 HTML 是唯一事实来源(SoT)，page_tasks 是保存时自动解析的投影索引 - Native AOT 兼容
/// </summary>
public class PageTaskService
{
    private readonly AppDbContext _db;
    private readonly WorkspacePermissionService _permissions;

    /// <summary>
    /// 匹配任务项开标签 + 内容（到第一个闭合 li，嵌套场景文本可能截断但状态同步不受影响）
    /// </summary>
    private static readonly Regex TaskItemRegex = new(
        @"<li\b[^>]*data-task-id=""(?<uid>[^""]+)""(?<attrs>[^>]*)>(?<body>.*?)</li>",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 匹配开标签中的 class 属性值
    /// </summary>
    private static readonly Regex ClassAttrRegex = new(@"class=""(?<value>[^""]*)""", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// 去除 HTML 标签（提取任务纯文本用）
    /// </summary>
    private static readonly Regex StripTagsRegex = new(@"<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);

    public PageTaskService(AppDbContext db, WorkspacePermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    /// <summary>
    /// 校验用户对空间的指定权限位，不满足返回错误信息
    /// </summary>
    private async Task<string?> CheckPermissionAsync(long workspaceId, long userId, bool isSiteAdmin, Func<SpacePermissions, bool> requirement, string actionName)
    {
        var permissions = await _permissions.GetEffectivePermissionsAsync(userId, isSiteAdmin, workspaceId);
        return requirement(permissions) ? null : $"无权限{actionName}";
    }

    /// <summary>
    /// 从页面内容中解析任务列表（uid、内容、完成状态、顺序）
    /// </summary>
    private static List<(string Uid, string Content, bool IsCompleted, int Position)> ParseTasks(string? content)
    {
        var result = new List<(string, string, bool, int)>();
        if (string.IsNullOrEmpty(content))
        {
            return result;
        }

        var position = 0;
        foreach (Match m in TaskItemRegex.Matches(content))
        {
            var uid = m.Groups["uid"].Value;
            if (string.IsNullOrWhiteSpace(uid))
            {
                continue;
            }

            var isCompleted = false;
            var classMatch = ClassAttrRegex.Match(m.Groups["attrs"].Value);
            if (classMatch.Success)
            {
                isCompleted = classMatch.Groups["value"].Value
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Contains("task-done", StringComparer.OrdinalIgnoreCase);
            }

            var text = StripTagsRegex.Replace(m.Groups["body"].Value, " ");
            text = WebUtility.HtmlDecode(text).Trim();
            if (text.Length > 2000)
            {
                text = text[..2000];
            }

            result.Add((uid, text, isCompleted, position));
            position++;
        }

        return result;
    }

    /// <summary>
    /// 将内容中的任务同步到 page_tasks 表（新增/更新/软删除）
    /// </summary>
    public async Task SyncFromContentAsync(long pageId, long workspaceId, long creatorId, string? content)
    {
        // 快速预检：内容中无任务标记时，将该页任务全部软删后返回
        var hasTaskMarker = content != null
            && (content.Contains("data-task-id", StringComparison.OrdinalIgnoreCase)
                || content.Contains("task-list", StringComparison.OrdinalIgnoreCase));

        var existing = await _db.Db.Queryable<PageTask>()
            .Where(t => t.PageId == pageId && !t.IsDeleted)
            .ToListAsync();

        if (!hasTaskMarker)
        {
            await SoftDeleteAllAsync(existing);
            return;
        }

        var parsed = ParseTasks(content);
        // 同一 uid 多次出现（回车延续时编辑器克隆 li 属性、复制粘贴任务项）仅保留首个，避免违反唯一约束
        var seenUids = new HashSet<string>(StringComparer.Ordinal);
        parsed = parsed.Where(p => seenUids.Add(p.Uid)).ToList();
        var parsedUids = seenUids;
        var now = DateTime.Now;

        // 内容中消失的任务 -> 软删除
        var removed = existing.Where(t => !parsedUids.Contains(t.TaskUid)).ToList();
        await SoftDeleteAllAsync(removed);

        // 历史软删记录（任务可能被移除后又撤销/粘贴回来，复活而非新插入）
        var deletedHistory = await _db.Db.Queryable<PageTask>()
            .Where(t => t.PageId == pageId && t.IsDeleted)
            .Select(t => new { t.TaskUid, t.Id })
            .ToListAsync();
        var deletedUidIds = deletedHistory.ToDictionary(d => d.TaskUid, d => d.Id);

        var toInsert = new List<PageTask>();
        foreach (var (uid, text, isCompleted, position) in parsed)
        {
            var existingTask = existing.FirstOrDefault(t => t.TaskUid == uid);
            if (existingTask != null)
            {
                var changed = existingTask.Content != text
                    || existingTask.Position != position
                    || existingTask.IsCompleted != isCompleted;
                if (!changed)
                {
                    continue;
                }

                existingTask.Content = text;
                existingTask.Position = position;
                // 状态未变化时不覆盖完成时间/完成人
                if (existingTask.IsCompleted != isCompleted)
                {
                    existingTask.IsCompleted = isCompleted;
                    existingTask.CompletedAt = isCompleted ? now : null;
                    existingTask.CompletedById = isCompleted ? creatorId : null;
                }
                existingTask.IsDeleted = false;
                existingTask.UpdatedAt = now;
                await _db.PageTasks.UpdateAsync(existingTask);
                continue;
            }

            var wasDeleted = deletedUidIds.TryGetValue(uid, out var resurrectId);
            if (wasDeleted && await _db.PageTasks.GetByIdAsync(resurrectId) is { } resurrected)
            {
                resurrected.Content = text;
                resurrected.Position = position;
                resurrected.IsCompleted = isCompleted;
                resurrected.CompletedAt = isCompleted ? now : null;
                resurrected.CompletedById = isCompleted ? creatorId : null;
                resurrected.IsDeleted = false;
                resurrected.UpdatedAt = now;
                await _db.PageTasks.UpdateAsync(resurrected);
                continue;
            }

            toInsert.Add(new PageTask
            {
                PageId = pageId,
                WorkspaceId = workspaceId,
                TaskUid = uid,
                Content = text,
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? now : null,
                CompletedById = isCompleted ? creatorId : null,
                Position = position,
                CreatorId = creatorId,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        if (toInsert.Count > 0)
        {
            await _db.Db.Insertable(toInsert).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 批量软删除任务
    /// </summary>
    private async Task SoftDeleteAllAsync(List<PageTask> tasks)
    {
        if (tasks.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        foreach (var task in tasks)
        {
            task.IsDeleted = true;
            task.UpdatedAt = now;
        }
        await _db.Db.Updateable(tasks).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除页面对应的所有任务（页面删除时调用）
    /// </summary>
    public async Task DeleteByPageAsync(long pageId)
    {
        var tasks = await _db.Db.Queryable<PageTask>()
            .Where(t => t.PageId == pageId && !t.IsDeleted)
            .ToListAsync();
        await SoftDeleteAllAsync(tasks);
    }

    /// <summary>
    /// 勾选/取消任务（即时保存，不产生版本快照）：
    /// 更新 page_tasks 行并同步 pages.content 中对应 li 的 task-done class
    /// </summary>
    public async Task<(bool success, string? error)> ToggleTaskAsync(long pageId, string taskUid, long userId, bool isSiteAdmin, bool isCompleted)
    {
        var page = await _db.Pages.GetByIdAsync(pageId);
        if (page == null)
        {
            return (false, "页面不存在");
        }

        // 需要编辑页面权限（与 Confluence 行为一致）
        var permissionError = await CheckPermissionAsync(page.WorkspaceId, userId, isSiteAdmin, p => p.EditPage, "编辑此页面任务");
        if (permissionError != null)
        {
            return (false, permissionError);
        }

        var task = await _db.Db.Queryable<PageTask>()
            .Where(t => t.PageId == pageId && t.TaskUid == taskUid && !t.IsDeleted)
            .FirstAsync();
        if (task == null)
        {
            return (false, "任务不存在");
        }

        var now = DateTime.Now;

        // 更新任务表
        task.IsCompleted = isCompleted;
        task.CompletedAt = isCompleted ? now : null;
        task.CompletedById = isCompleted ? userId : null;
        task.UpdatedAt = now;
        await _db.PageTasks.UpdateAsync(task);

        // 同步内容中该任务 li 的 task-done class（仅属性级替换，不受嵌套影响）
        if (page.Content != null)
        {
            page.Content = UpdateTaskClassInContent(page.Content, taskUid, isCompleted);
            page.UpdatedAt = now;
            await _db.Pages.UpdateAsync(page);
        }

        return (true, null);
    }

    /// <summary>
    /// 在内容 HTML 中增删指定任务 li 的 task-done class
    /// </summary>
    private static string UpdateTaskClassInContent(string content, string taskUid, bool isCompleted)
    {
        var escapedUid = Regex.Escape(taskUid);
        var openTagRegex = new Regex(
            @"(?<prefix><li\b[^>]*data-task-id=""" + escapedUid + @"""[^>]*>)",
            RegexOptions.IgnoreCase);

        return openTagRegex.Replace(content, m =>
        {
            var tag = m.Groups["prefix"].Value;
            var classMatch = ClassAttrRegex.Match(tag);
            if (classMatch.Success)
            {
                var classes = classMatch.Groups["value"].Value
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                var hasDone = classes.Contains("task-done", StringComparer.OrdinalIgnoreCase);
                if (isCompleted && !hasDone)
                {
                    classes.Add("task-done");
                }
                else if (!isCompleted && hasDone)
                {
                    classes.RemoveAll(c => string.Equals(c, "task-done", StringComparison.OrdinalIgnoreCase));
                }
                var newTag = tag.Replace(classMatch.Value, $"class=\"{string.Join(' ', classes)}\"");
                return newTag;
            }

            if (isCompleted)
            {
                // 无 class 属性则插入 class="task-done"（插在 > 前）
                return tag[..^1] + " class=\"task-done\">";
            }

            return tag;
        });
    }

    /// <summary>
    /// 获取页面任务列表
    /// </summary>
    public async Task<(List<PageTaskDto>? tasks, string? error)> GetPageTasksAsync(long pageId, long userId, bool isSiteAdmin)
    {
        var page = await _db.Pages.GetByIdAsync(pageId);
        if (page == null)
        {
            return (null, "页面不存在");
        }

        var permissionError = await CheckPermissionAsync(page.WorkspaceId, userId, isSiteAdmin, p => p.ViewSpace, "查看该空间页面任务");
        if (permissionError != null)
        {
            return (null, permissionError);
        }

        var tasks = await _db.Db.Queryable<PageTask>()
            .Where(t => t.PageId == pageId && !t.IsDeleted)
            .OrderBy(t => t.Position)
            .ToListAsync();

        var dtos = tasks.Select(t => new PageTaskDto
        {
            Id = t.Id,
            PageId = t.PageId,
            TaskUid = t.TaskUid,
            Content = t.Content,
            IsCompleted = t.IsCompleted,
            CompletedAt = t.CompletedAt,
            Position = t.Position,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        return (dtos, null);
    }
}
