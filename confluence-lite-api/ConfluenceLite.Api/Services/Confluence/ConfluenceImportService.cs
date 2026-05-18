using System.Security.Cryptography;
using System.Text.Json;
using System.IO.Compression;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Models.Confluence;

namespace ConfluenceLite.Api.Services.Confluence;

/// <summary>
/// Confluence 导入服务
/// </summary>
public class ConfluenceImportService
{
    private readonly AppDbContext _db;
    private readonly ConfluenceXmlParser _parser;
    private readonly IHostEnvironment _env;
    private readonly ILogger<ConfluenceImportService> _logger;

    public ConfluenceImportService(
        AppDbContext db,
        ConfluenceXmlParser parser,
        IHostEnvironment env,
        ILogger<ConfluenceImportService> logger)
    {
        _db = db;
        _parser = parser;
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// 开始导入任务
    /// </summary>
    public async Task<(ImportTask? Task, string? Error)> StartImportAsync(
        string zipFilePath,
        ImportOptions options,
        long userId)
    {
        // 验证备份文件
        var (isValid, error, version) = await _parser.ValidateBackupAsync(zipFilePath);
        if (!isValid)
        {
            return (null, error);
        }

        _logger.LogInformation("开始导入 Confluence 备份: {File}, Version: {Version}", zipFilePath, version);

        // 创建导入任务
        var task = new ImportTask
        {
            Name = $"Confluence Import ({DateTime.Now:yyyy-MM-dd HH:mm:ss})",
            SourceFile = zipFilePath,
            Status = "pending",
            Options = JsonSerializer.Serialize(options),
            CreatedById = userId,
            CreatedAt = DateTime.Now
        };

        var taskId = await _db.Db.Insertable(task).ExecuteReturnIdentityAsync();
        task.Id = taskId;

        // 在后台执行导入
        _ = Task.Run(() => ExecuteImportAsync(taskId));

        return (task, null);
    }

    /// <summary>
    /// 执行导入（后台任务）
    /// </summary>
    private async Task ExecuteImportAsync(long taskId)
    {
        var task = await _db.Db.Queryable<ImportTask>()
            .Where(t => t.Id == taskId)
            .FirstAsync();

        if (task == null)
        {
            _logger.LogError("导入任务不存在: {TaskId}", taskId);
            return;
        }

        try
        {
            // 更新状态为处理中
            task.Status = "processing";
            await _db.Db.Updateable(task).UpdateColumns(t => t.Status).ExecuteCommandAsync();

            // 解析导入选项
            var options = JsonSerializer.Deserialize<ImportOptions>(task.Options ?? "{}") ?? new ImportOptions();

            // 解析备份文件
            var data = await _parser.ParseBackupAsync(task.SourceFile, progress =>
            {
                UpdateProgressAsync(taskId, progress).Wait();
            });

            // 执行导入
            var result = await ImportDataAsync(data, options, task);

            if (result.IsSuccess)
            {
                task.Status = "completed";
                task.CompletedAt = DateTime.Now;
                _logger.LogInformation("导入完成: {TaskId}, 成功: {Success}, 失败: {Failed}",
                    taskId, result.SuccessCount, result.FailedCount);
            }
            else
            {
                task.Status = "failed";
                task.ErrorMessage = result.ErrorMessage;
                task.CompletedAt = DateTime.Now;
                _logger.LogError("导入失败: {TaskId}, 错误: {Error}",
                    taskId, result.ErrorMessage);
            }

            await _db.Db.Updateable(task).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入任务执行失败: {TaskId}", taskId);
            task.Status = "failed";
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.Now;
            await _db.Db.Updateable(task).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 按依赖顺序导入各类型数据
    /// </summary>
    private async Task<ImportResult> ImportDataAsync(
        ConfluenceBackupData data,
        ImportOptions options,
        ImportTask task)
    {
        var result = new ImportResult();
        var mapper = new DataMappingService(Microsoft.Extensions.Logging.Abstractions.NullLogger<DataMappingService>.Instance);

        try
        {
            // 构建内容映射
            var bodyContentMap = data.BodyContents
                .Where(bc => bc.PageId.HasValue)
                .ToDictionary(bc => bc.PageId!.Value, bc => bc.Body);

            var spaceDescriptionMap = data.SpaceDescriptions
                .Where(sd => sd.SpaceId.HasValue)
                .ToDictionary(sd => sd.SpaceId!.Value, sd => sd.Body);

            var progress = new ImportProgress
            {
                TotalItems = data.Users.Count + data.Spaces.Count + data.Pages.Count +
                            data.Attachments.Count + data.Comments.Count
            };

            // 附件 URL 映射（在导入附件后填充）
            var attachmentUrlMap = new Dictionary<long, string>();

            // 1. 导入用户（最先导入，因为其他实体依赖用户）
            if (options.ImportUsers && data.Users.Any())
            {
                progress.CurrentStep = "正在导入用户...";
                await UpdateProgressAsync(task.Id, progress);

                var userCount = await ImportUsersAsync(data.Users, mapper);
                result.EntityCounts["Users"] = userCount;
                result.SuccessCount += userCount;
                progress.ProcessedItems += userCount;
                progress.EntityCounts["Users"] = userCount;
                await UpdateProgressAsync(task.Id, progress);
            }

            // 2. 导入空间
            if (options.ImportSpaces && data.Spaces.Any())
            {
                progress.CurrentStep = "正在导入空间...";
                await UpdateProgressAsync(task.Id, progress);

                var spaceCount = await ImportSpacesAsync(data.Spaces, mapper, spaceDescriptionMap);
                result.EntityCounts["Spaces"] = spaceCount;
                result.SuccessCount += spaceCount;
                progress.ProcessedItems += spaceCount;
                progress.EntityCounts["Spaces"] = spaceCount;
                await UpdateProgressAsync(task.Id, progress);
            }

            // 3. 导入附件（需要在页面之前导入，以便构建 URL 映射）
            if (options.ImportAttachments && data.Attachments.Any())
            {
                progress.CurrentStep = "正在导入附件...";
                await UpdateProgressAsync(task.Id, progress);

                var attachmentCount = await ImportAttachmentsAsync(
                    data.Attachments,
                    task.SourceFile,
                    data.AttachmentFiles,
                    mapper,
                    attachmentUrlMap);
                result.EntityCounts["Attachments"] = attachmentCount;
                result.SuccessCount += attachmentCount;
                progress.ProcessedItems += attachmentCount;
                progress.EntityCounts["Attachments"] = attachmentCount;
                await UpdateProgressAsync(task.Id, progress);
            }

            // 4. 导入页面（使用内容转换器）
            if (options.ImportPages && data.Pages.Any())
            {
                progress.CurrentStep = "正在导入页面...";
                await UpdateProgressAsync(task.Id, progress);

                var pageCount = await ImportPagesAsync(
                    data.Pages,
                    mapper,
                    bodyContentMap,
                    attachmentUrlMap);
                result.EntityCounts["Pages"] = pageCount;
                result.SuccessCount += pageCount;
                progress.ProcessedItems += pageCount;
                progress.EntityCounts["Pages"] = pageCount;
                await UpdateProgressAsync(task.Id, progress);
            }

            // 5. 导入评论
            if (options.ImportComments && data.Comments.Any())
            {
                progress.CurrentStep = "正在导入评论...";
                await UpdateProgressAsync(task.Id, progress);

                var commentCount = await ImportCommentsAsync(data.Comments, mapper);
                result.EntityCounts["Comments"] = commentCount;
                result.SuccessCount += commentCount;
                progress.ProcessedItems += commentCount;
                progress.EntityCounts["Comments"] = commentCount;
                await UpdateProgressAsync(task.Id, progress);
            }

            result.IsSuccess = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入数据失败");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// 导入用户
    /// </summary>
    private async Task<int> ImportUsersAsync(List<ConfluenceUser> users, DataMappingService mapper)
    {
        var count = 0;
        foreach (var user in users)
        {
            try
            {
                // 检查用户是否已存在
                var existingUser = await _db.Db.Queryable<User>()
                    .Where(u => u.Username == user.Name)
                    .FirstAsync();

                if (existingUser == null)
                {
                    var newUser = mapper.MapUser(user);
                    var newId = await _db.Db.Insertable(newUser).ExecuteReturnIdentityAsync();
                    mapper.AddUserMapping(user.Key, newId);
                    count++;
                }
                else
                {
                    // 使用现有用户ID
                    mapper.AddUserMapping(user.Key, existingUser.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入用户失败: {Username}", user.Name);
            }
        }

        // 添加默认管理员映射
        mapper.AddUserMapping("admin", 1);

        return count;
    }

    /// <summary>
    /// 导入空间
    /// </summary>
    private async Task<int> ImportSpacesAsync(
        List<ConfluenceSpace> spaces,
        DataMappingService mapper,
        Dictionary<long, string> descriptionMap)
    {
        var count = 0;
        var ownerId = 1; // 默认管理员

        foreach (var space in spaces)
        {
            try
            {
                var description = space.DescriptionId.HasValue &&
                                 descriptionMap.TryGetValue(space.DescriptionId.Value, out var desc)
                    ? desc
                    : null;

                var workspace = mapper.MapSpace(space, ownerId, description);

                // 检查是否覆盖
                var existing = await _db.Db.Queryable<Workspace>()
                    .Where(w => w.Key == space.Key)
                    .FirstAsync();

                if (existing != null)
                {
                    workspace.Id = existing.Id;
                    await _db.Db.Updateable(workspace).ExecuteCommandAsync();
                }
                else
                {
                    await _db.Db.Insertable(workspace).ExecuteCommandAsync();
                }

                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入空间失败: {Key}", space.Key);
            }
        }

        return count;
    }

    /// <summary>
    /// 导入页面
    /// </summary>
    private async Task<int> ImportPagesAsync(
        List<ConfluencePage> pages,
        DataMappingService mapper,
        Dictionary<long, string> contentMap,
        Dictionary<long, string> attachmentUrlMap)
    {
        var processedPages = new HashSet<long>();

        // 首先导入所有没有父页面的页面（根页面）
        var rootPages = pages.Where(p => !p.ParentId.HasValue).ToList();
        return await ImportPagesInOrder(rootPages, pages, mapper, contentMap, attachmentUrlMap, processedPages);
    }

    /// <summary>
    /// 按层级顺序导入页面
    /// </summary>
    private async Task<int> ImportPagesInOrder(
        List<ConfluencePage> currentPageList,
        List<ConfluencePage> allPages,
        DataMappingService mapper,
        Dictionary<long, string> contentMap,
        Dictionary<long, string> attachmentUrlMap,
        HashSet<long> processedPages)
    {
        var count = 0;

        foreach (var page in currentPageList)
        {
            if (processedPages.Contains(page.Id))
                continue;

            try
            {
                string? content = null;
                if (page.BodyContentId.HasValue &&
                    contentMap.TryGetValue(page.BodyContentId.Value, out var bodyContent))
                {
                    // 使用内容转换器转换 Confluence 内容
                    var converter = new ConfluenceContentConverter(
                        attachmentUrlMap,
                        mapper.GetPageIdMap());
                    content = converter.ConvertToHtml(bodyContent, page.Id);
                }

                var workspaceId = page.SpaceId.HasValue &&
                                  mapper.GetMappedSpaceId(page.SpaceId.Value) is long mappedSpaceId
                    ? mappedSpaceId
                    : 1;

                var newPage = mapper.MapPage(page, workspaceId, content);

                var existing = await _db.Db.Queryable<Page>()
                    .Where(p => p.Id == page.Id)
                    .FirstAsync();

                if (existing != null)
                {
                    await _db.Db.Updateable(newPage).ExecuteCommandAsync();
                }
                else
                {
                    await _db.Db.Insertable(newPage).ExecuteCommandAsync();
                }

                processedPages.Add(page.Id);
                count++;

                // 导入子页面
                var childPages = allPages.Where(p => p.ParentId == page.Id).ToList();
                if (childPages.Any())
                {
                    count += await ImportPagesInOrder(childPages, allPages, mapper, contentMap, attachmentUrlMap, processedPages);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入页面失败: {Title}", page.Title);
            }
        }

        return count;
    }

    /// <summary>
    /// 导入附件
    /// </summary>
    private async Task<int> ImportAttachmentsAsync(
        List<ConfluenceAttachment> attachments,
        string zipFilePath,
        Dictionary<long, string> attachmentFiles,
        DataMappingService mapper,
        Dictionary<long, string> attachmentUrlMap)
    {
        var count = 0;
        var uploadsDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "attachments");

        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        using var archive = ZipFile.OpenRead(zipFilePath);

        foreach (var attachment in attachments)
        {
            try
            {
                if (!attachment.PageId.HasValue || !attachmentFiles.ContainsKey(attachment.Id))
                    continue;

                var pageId = mapper.GetMappedPageId(attachment.PageId.Value);
                if (pageId == null)
                    continue;

                var creatorId = mapper.MapUserKey(attachment.CreatorKey);

                // 从ZIP提取文件
                var entryName = attachmentFiles[attachment.Id];
                var entry = archive.GetEntry(entryName);
                if (entry == null)
                    continue;

                // 创建存储路径
                var storagePath = Path.Combine("attachments", pageId.ToString(), attachment.Id.ToString());
                var fullPath = Path.Combine(uploadsDir, pageId.ToString(), attachment.Id.ToString());

                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }

                var filePath = Path.Combine(fullPath, SanitizeFileName(attachment.Title));
                using (var stream = entry.Open())
                using (var fileStream = File.Create(filePath))
                {
                    await stream.CopyToAsync(fileStream);
                }

                // 计算文件哈希
                var fileHash = await ComputeFileHashAsync(filePath);

                var newAttachment = mapper.MapAttachment(
                    attachment,
                    pageId.Value,
                    creatorId,
                    Path.Combine(storagePath, SanitizeFileName(attachment.Title))
                );

                newAttachment.FileHash = fileHash;

                var existing = await _db.Db.Queryable<Attachment>()
                    .Where(a => a.Id == attachment.Id)
                    .FirstAsync();

                if (existing != null)
                {
                    newAttachment.CreatedAt = existing.CreatedAt;
                    await _db.Db.Updateable(newAttachment).ExecuteCommandAsync();
                }
                else
                {
                    await _db.Db.Insertable(newAttachment).ExecuteCommandAsync();
                }

                // 添加到 URL 映射（用于内容转换）
                var accessUrl = $"/{newAttachment.StoragePath}";
                attachmentUrlMap[attachment.Id] = accessUrl;

                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入附件失败: {Title}", attachment.Title);
            }
        }

        return count;
    }

    /// <summary>
    /// 导入评论
    /// </summary>
    private async Task<int> ImportCommentsAsync(List<ConfluenceComment> comments, DataMappingService mapper)
    {
        var count = 0;

        foreach (var comment in comments)
        {
            try
            {
                if (!comment.PageId.HasValue || comment.IsDeleted)
                    continue;

                var pageId = mapper.GetMappedPageId(comment.PageId.Value);
                if (pageId == null)
                    continue;

                var newComment = mapper.MapComment(comment, pageId.Value);

                var existing = await _db.Db.Queryable<PageComment>()
                    .Where(c => c.Id == comment.Id)
                    .FirstAsync();

                if (existing != null)
                {
                    await _db.Db.Updateable(newComment).ExecuteCommandAsync();
                }
                else
                {
                    await _db.Db.Insertable(newComment).ExecuteCommandAsync();
                }

                count++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入评论失败: {Id}", comment.Id);
            }
        }

        return count;
    }

    /// <summary>
    /// 更新导入进度
    /// </summary>
    private async Task UpdateProgressAsync(long taskId, ImportProgress progress)
    {
        try
        {
            var task = await _db.Db.Queryable<ImportTask>()
                .Where(t => t.Id == taskId)
                .FirstAsync();

            if (task != null)
            {
                task.Progress = JsonSerializer.Serialize(progress);
                await _db.Db.Updateable(task).ExecuteCommandAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新导入进度失败: {TaskId}", taskId);
        }
    }

    /// <summary>
    /// 计算文件哈希
    /// </summary>
    private async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// 清理文件名
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars));
    }
}

/// <summary>
/// 导入结果
/// </summary>
public class ImportResult
{
    public bool IsSuccess { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, int> EntityCounts { get; set; } = new();
}
