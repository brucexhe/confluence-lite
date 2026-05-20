using System.Security.Cryptography;
using System.Text.Json;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Models.Confluence;
using ConfluenceLite.Api.Mappers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    private readonly IServiceScopeFactory _scopeFactory;

    public ConfluenceImportService(
        AppDbContext db,
        ConfluenceXmlParser parser,
        IHostEnvironment env,
        ILogger<ConfluenceImportService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _db = db;
        _parser = parser;
        _env = env;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// 开始导入任务
    /// </summary>
    public async Task<(ImportTask? Task, string? Error)> StartImportAsync(
        string zipFilePath,
        ImportOptions options,
        long userId)
    {
        try
        {
            _logger.LogInformation("StartImportAsync 开始: {File}", zipFilePath);

            // 快速验证：只检查文件是否存在和扩展名
            if (!File.Exists(zipFilePath))
            {
                return (null, "文件不存在");
            }

            if (!zipFilePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return (null, "仅支持 .zip 格式的备份文件");
            }

            _logger.LogInformation("文件验证通过，创建导入任务");

            // 创建导入任务（先不验证内容，让后台任务去做）
            var optionsJson = JsonSerializer.Serialize(options, AppJsonContext.Default.ImportOptions);
            var taskName = $"Confluence Import ({DateTime.Now:yyyy-MM-dd HH:mm:ss})";

            _logger.LogInformation("正在保存导入任务到数据库...");

            // 使用 SQL 插入以正确处理 PostgreSQL 的 jsonb 类型
            var taskIdList = await _db.Db.Ado.SqlQueryAsync<long>(
                "INSERT INTO \"import_tasks\" (\"name\", \"sourcefile\", \"status\", \"options\", \"createdat\", \"createdbyid\") " +
                "VALUES (@name, @sourcefile, @status, @options::jsonb, @createdat, @createdbyid) " +
                "RETURNING \"id\"",
                new
                {
                    name = taskName,
                    sourcefile = zipFilePath,
                    status = "pending",
                    options = optionsJson,
                    createdat = DateTime.Now,
                    createdbyid = userId
                });

            var taskId = taskIdList.FirstOrDefault();
            _logger.LogInformation("导入任务已保存，ID: {TaskId}", taskId);

            var task = new ImportTask
            {
                Id = taskId,
                Name = taskName,
                SourceFile = zipFilePath,
                Status = "pending",
                Options = optionsJson,
                CreatedById = userId,
                CreatedAt = DateTime.Now
            };

            // 在后台执行导入（包含验证和解析）
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                try
                {
                    await ExecuteImportAsync(scope, taskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "后台导入任务执行失败: {TaskId}", taskId);
                }
            });

            _logger.LogInformation("StartImportAsync 完成，返回任务: {TaskId}", taskId);
            return (task, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StartImportAsync 发生异常");
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 执行导入（后台任务）
    /// </summary>
    private async Task ExecuteImportAsync(IServiceScope scope, long taskId)
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var parser = scope.ServiceProvider.GetRequiredService<ConfluenceXmlParser>();

        var task = await db.Db.Queryable<ImportTask>()
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
            await db.Db.Updateable(task).UpdateColumns(t => t.Status).ExecuteCommandAsync();

            // 先验证备份文件（在后台线程中，不会阻塞请求）
            _logger.LogInformation("任务 {TaskId} 开始验证备份文件: {File}", taskId, task.SourceFile);
            var (isValid, error, version) = await parser.ValidateBackupAsync(task.SourceFile);
            if (!isValid)
            {
                _logger.LogError("任务 {TaskId} 备份文件验证失败: {Error}", taskId, error);
                task.Status = "failed";
                task.ErrorMessage = error;
                task.CompletedAt = DateTime.Now;
                await db.Db.Updateable(task).ExecuteCommandAsync();
                return;
            }

            _logger.LogInformation("任务 {TaskId} 备份文件验证成功，版本: {Version}", taskId, version);

            // 解析导入选项
            var options = string.IsNullOrEmpty(task.Options)
                ? new ImportOptions()
                : JsonSerializer.Deserialize(task.Options, AppJsonContext.Default.ImportOptions) ?? new ImportOptions();

            // 解析备份文件
            var data = await parser.ParseBackupAsync(task.SourceFile, async progress =>
            {
                await UpdateProgressAsync(db, task, progress);
            });

            // 执行导入
            var result = await ImportDataAsync(db, _env, _logger, data, options, task);

            if (result.IsSuccess)
            {
                task.Status = "completed";
                task.CompletedAt = DateTime.Now;

                // 更新最终进度
                var finalProgress = new ImportProgress
                {
                    TotalItems = data.Users.Count + data.Spaces.Count + data.Pages.Count +
                                data.Attachments.Count + data.Comments.Count,
                    ProcessedItems = result.SuccessCount,
                    FailedItems = result.FailedCount,
                    CurrentStep = "导入完成",
                    EntityCounts = result.EntityCounts
                };
                task.Progress = JsonSerializer.Serialize(finalProgress, AppJsonContext.Default.ImportProgress);

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

            await db.Db.Updateable(task).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入任务执行失败: {TaskId}", taskId);
            task.Status = "failed";
            task.ErrorMessage = ex.Message;
            task.CompletedAt = DateTime.Now;
            await db.Db.Updateable(task).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 按依赖顺序导入各类型数据
    /// </summary>
    private async Task<ImportResult> ImportDataAsync(
        AppDbContext db,
        IHostEnvironment env,
        ILogger<ConfluenceImportService> logger,
        ConfluenceBackupData data,
        ImportOptions options,
        ImportTask task)
    {
        var result = new ImportResult();
        var mapper = new DataMappingService(Microsoft.Extensions.Logging.Abstractions.NullLogger<DataMappingService>.Instance);

        try
        {
            var zipPath = Path.Combine(env.ContentRootPath, task.SourceFile.TrimStart('/'));

            // 构建内容映射 (使用主体自身的 Id 作为 Key，以对应 Page.BodyContentId 和 Space.DescriptionId)
            var bodyContentMap = data.BodyContents
                .ToDictionary(bc => bc.Id, bc => bc.Body);

            var spaceDescriptionMap = data.SpaceDescriptions
                .ToDictionary(sd => sd.Id, sd => sd.Body);

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
                await UpdateProgressAsync(db, task, progress);

                var userCount = await ImportUsersAsync(db, logger, data.Users, mapper);
                result.EntityCounts["Users"] = userCount;
                result.SuccessCount += userCount;
                progress.ProcessedItems += userCount;
                progress.EntityCounts["Users"] = userCount;
                await UpdateProgressAsync(db, task, progress);
            }

            // 2. 导入空间
            if (options.ImportSpaces && data.Spaces.Any())
            {
                progress.CurrentStep = "正在导入空间...";
                await UpdateProgressAsync(db, task, progress);

                var spaceCount = await ImportSpacesAsync(db, logger, data.Spaces, mapper, spaceDescriptionMap, task.CreatedById, options);
                result.EntityCounts["Spaces"] = spaceCount;
                result.SuccessCount += spaceCount;
                progress.ProcessedItems += spaceCount;
                progress.EntityCounts["Spaces"] = spaceCount;
                await UpdateProgressAsync(db, task, progress);
            }

            // 2. 预处理页面（过滤草稿并预注册 ID 映射，以便附件先导入）
            var publishedPages = data.Pages.Where(p => p.ContentStatus == "current").ToList();
            var draftCount = data.Pages.Count - publishedPages.Count;
            foreach (var p in publishedPages)
            {
                mapper.AddPageMapping(p.Id, p.Id);
            }

            // 3. 导入附件（需要在页面之前导入，以便构建 URL 映射）
            if (options.ImportAttachments && data.Attachments.Any())
            {
                progress.CurrentStep = "正在导入附件...";
                await UpdateProgressAsync(db, task, progress);

                var attachmentCount = await ImportAttachmentsAsync(db, env, logger,
                    data.Attachments,
                    zipPath, // 使用任务 ZIP 路径
                    data.AttachmentFiles,
                    mapper,
                    attachmentUrlMap,
                    options);
                result.EntityCounts["Attachments"] = attachmentCount;
                result.SuccessCount += attachmentCount;
                progress.ProcessedItems += attachmentCount;
                progress.EntityCounts["Attachments"] = attachmentCount;
                await UpdateProgressAsync(db, task, progress);
            }

            // 4. 导入页面（使用内容转换器）
            if (options.ImportPages && publishedPages.Any())
            {
                progress.CurrentStep = "正在导入页面...";
                await UpdateProgressAsync(db, task, progress);

                var pageCount = await ImportPagesAsync(db, logger,
                    publishedPages, // 使用过滤后的页面列表
                    mapper,
                    bodyContentMap,
                    attachmentUrlMap,
                    options);
                result.EntityCounts["Pages"] = pageCount;
                result.SuccessCount += pageCount;
                progress.ProcessedItems += pageCount + draftCount; // 包含跳过的草稿
                progress.EntityCounts["Pages"] = pageCount + draftCount; // 包含草稿数量，便于前端统计
                await UpdateProgressAsync(db, task, progress);
            }
            else if (options.ImportPages)
            {
                // 即使没有发布页面，也要统计草稿数量
                progress.ProcessedItems += draftCount;
                progress.EntityCounts["Pages"] = draftCount;
            }

            // 记录草稿数量（如果有）
            if (draftCount > 0)
            {
                result.EntityCounts["DraftPages"] = draftCount;
                logger.LogInformation("跳过 {DraftCount} 个草稿页面", draftCount);
            }

            // 5. 导入评论
            if (options.ImportComments && data.Comments.Any())
            {
                progress.CurrentStep = "正在导入评论...";
                await UpdateProgressAsync(db, task, progress);

                var commentCount = await ImportCommentsAsync(db, logger, data.Comments, mapper, options);
                result.EntityCounts["Comments"] = commentCount;
                result.SuccessCount += commentCount;
                progress.ProcessedItems += commentCount;
                progress.EntityCounts["Comments"] = commentCount;
                await UpdateProgressAsync(db, task, progress);
            }

            result.IsSuccess = true;
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "导入数据失败");
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// 导入用户
    /// </summary>
    private async Task<int> ImportUsersAsync(AppDbContext db, ILogger<ConfluenceImportService> logger, List<ConfluenceUser> users, DataMappingService mapper)
    {
        var count = 0;
        foreach (var user in users)
        {
            // 检查用户是否已存在
            var existingUser = await db.Db.Queryable<User>()
                .Where(u => u.Username == user.Name)
                .FirstAsync();

            if (existingUser == null)
            {
                var newUser = mapper.MapUser(user);
                var newId = await db.Db.Insertable(newUser).ExecuteReturnIdentityAsync();
                mapper.AddUserMapping(user.Key, newId);
                count++;
            }
            else
            {
                // 使用现有用户ID
                mapper.AddUserMapping(user.Key, existingUser.Id);
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
        AppDbContext db,
        ILogger<ConfluenceImportService> logger,
        List<ConfluenceSpace> spaces,
        DataMappingService mapper,
        Dictionary<long, string> descriptionMap,
        long ownerId,
        ImportOptions options)
    {
        var count = 0;

        foreach (var space in spaces)
        {
            var description = space.DescriptionId.HasValue &&
                             descriptionMap.TryGetValue(space.DescriptionId.Value, out var desc)
                ? desc
                : null;

            var workspace = mapper.MapSpace(space, ownerId, description);

            // 检查是否覆盖
            var existing = await db.Db.Queryable<Workspace>()
                .Where(w => w.Key == space.Key)
                .FirstAsync();

            long newId;
            if (existing != null)
            {
                if (options.OverwriteExisting)
                {
                    workspace.Id = existing.Id;
                    await db.Db.Updateable(workspace).ExecuteCommandAsync();
                    newId = existing.Id;
                    logger.LogInformation("覆盖工作空间: {Key}", space.Key);
                }
                else
                {
                    newId = existing.Id;
                    logger.LogInformation("跳过已存在的工作空间: {Key}", space.Key);
                }
            }
            else
            {
                // 强制插入原始 ID，使用原始 SQL 以绕过 SqlSugar 的自增列限制
                await db.Db.Ado.ExecuteCommandAsync(
                    "INSERT INTO \"workspaces\" (\"id\", \"name\", \"key\", \"description\", \"ownerid\", \"status\", \"icon\", \"color\", \"homepageid\", \"ispersonal\", \"isdefault\", \"isdeleted\", \"createdat\", \"updatedat\") " +
                    "VALUES (@Id::bigint, @Name, @Key, @Description, @OwnerId::bigint, @Status::integer, @Icon, @Color, @HomePageId::bigint, @IsPersonal::boolean, @IsDefault::boolean, @IsDeleted::boolean, @CreatedAt::timestamp, @UpdatedAt::timestamp)",
                    new
                    {
                        workspace.Id,
                        workspace.Name,
                        workspace.Key,
                        workspace.Description,
                        workspace.OwnerId,
                        workspace.Status,
                        workspace.Icon,
                        workspace.Color,
                        workspace.HomePageId,
                        workspace.IsPersonal,
                        workspace.IsDefault,
                        workspace.IsDeleted,
                        workspace.CreatedAt,
                        workspace.UpdatedAt
                    });
                newId = space.Id;
            }

            if (newId > 0)
            {
                mapper.AddSpaceMapping(space.Id, newId);
                count++;
            }
        }

        // 导入完成后尝试更新序列，防止后续自增冲突（仅对 PostgreSQL 尝试）
        try { await db.Db.Ado.ExecuteCommandAsync("SELECT setval('\"workspaces_id_seq\"', (SELECT MAX(id) FROM \"workspaces\"))"); } catch { }

        return count;
    }

    /// <summary>
    /// 导入页面
    /// </summary>
    private async Task<int> ImportPagesAsync(
        AppDbContext db,
        ILogger<ConfluenceImportService> logger,
        List<ConfluencePage> pages,
        DataMappingService mapper,
        Dictionary<long, string> contentMap,
        Dictionary<long, string> attachmentUrlMap,
        ImportOptions options)
    {
        var processedPages = new HashSet<long>();

        // 首先导入所有没有父页面的页面（根页面，ParentId 为 null 或 0）
        var rootPages = pages.Where(p => !p.ParentId.HasValue || p.ParentId == 0).ToList();
        return await ImportPagesInOrder(db, logger, rootPages, pages, mapper, contentMap, attachmentUrlMap, processedPages, options);
    }

    /// <summary>
    /// 按层级顺序导入页面
    /// </summary>
    private async Task<int> ImportPagesInOrder(
        AppDbContext db,
        ILogger<ConfluenceImportService> logger,
        List<ConfluencePage> rootPages,
        List<ConfluencePage> allPages,
        DataMappingService mapper,
        Dictionary<long, string> contentMap,
        Dictionary<long, string> attachmentUrlMap,
        HashSet<long> processedPages,
        ImportOptions options)
    {
        var count = 0;

        foreach (var page in rootPages)
        {
            if (processedPages.Contains(page.Id))
                continue;

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

            // 检查是否覆盖
            var existing = await db.Db.Queryable<Page>()
                .Where(p => p.Id == page.Id)
                .FirstAsync();

            long newPageId;
            if (existing != null)
            {
                if (options.OverwriteExisting)
                {
                    newPage.Id = existing.Id;
                    await db.Db.Updateable(newPage).ExecuteCommandAsync();
                    newPageId = existing.Id;
                }
                else
                {
                    newPageId = existing.Id;
                }
            }
            else
            {
                // 强制插入原始 ID，使用原始 SQL 插入
                await db.Db.Ado.ExecuteCommandAsync(
                    "INSERT INTO \"pages\" (\"id\", \"title\", \"content\", \"status\", \"workspaceid\", \"creatorid\", \"parentid\", \"sortorder\", \"version\", \"lastmodifierid\", \"isdeleted\", \"createdat\", \"updatedat\") " +
                    "VALUES (@Id::bigint, @Title, @Content, @Status::integer, @WorkspaceId::bigint, @CreatorId::bigint, @ParentId::bigint, @SortOrder::integer, @Version::integer, @LastModifierId::bigint, @IsDeleted::boolean, @CreatedAt::timestamp, @UpdatedAt::timestamp)",
                    new
                    {
                        newPage.Id,
                        newPage.Title,
                        newPage.Content,
                        newPage.Status,
                        newPage.WorkspaceId,
                        newPage.CreatorId,
                        newPage.ParentId,
                        newPage.SortOrder,
                        newPage.Version,
                        newPage.LastModifierId,
                        newPage.IsDeleted,
                        newPage.CreatedAt,
                        newPage.UpdatedAt
                    });
                newPageId = page.Id;
            }

            mapper.AddPageMapping(page.Id, newPageId);

            processedPages.Add(page.Id);
            count++;

            // 递归导入子页面
            var childPages = allPages.Where(p => p.ParentId == page.Id).ToList();
            if (childPages.Any())
            {
                count += await ImportPagesInOrder(db, logger, childPages, allPages, mapper, contentMap, attachmentUrlMap, processedPages, options);
            }
        }

        // 尝试更新序列
        if (rootPages.Any(p => !p.ParentId.HasValue || p.ParentId == 0))
        {
            try { await db.Db.Ado.ExecuteCommandAsync("SELECT setval('\"pages_id_seq\"', (SELECT MAX(id) FROM \"pages\"))"); } catch { }
        }

        return count;
    }

    /// <summary>
    /// 导入附件
    /// </summary>
    private async Task<int> ImportAttachmentsAsync(
        AppDbContext db,
        IHostEnvironment env,
        ILogger<ConfluenceImportService> logger,
        List<ConfluenceAttachment> attachments,
        string zipFilePath,
        Dictionary<long, string> attachmentFiles,
        DataMappingService mapper,
        Dictionary<long, string> attachmentUrlMap,
        ImportOptions options)
    {
        var count = 0;
        var skippedNoPageId = 0;
        var skippedNoFile = 0;
        var skippedNoPageMapping = 0;
        var skippedNoEntry = 0;
        var uploadsDir = Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "attachments");

        logger.LogInformation("开始导入附件，总数: {AttachmentCount}", attachments.Count);
        logger.LogInformation("ZIP 中解析到的附件文件数: {FileCount}", attachmentFiles.Count);

        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        using var archive = ZipFile.OpenRead(zipFilePath);

        foreach (var attachment in attachments)
        {
            if (!attachment.PageId.HasValue)
            {
                skippedNoPageId++;
                logger.LogWarning("附件 {AttachmentId} ({Title}) 被跳过: PageId 为空", attachment.Id, attachment.Title);
                continue;
            }

            if (!attachmentFiles.ContainsKey(attachment.Id))
            {
                skippedNoFile++;
                logger.LogWarning("附件 {AttachmentId} ({Title}) 被跳过: ZIP 中未找到对应文件", attachment.Id, attachment.Title);
                continue;
            }

            var pageId = mapper.GetMappedPageId(attachment.PageId.Value);
            if (pageId == null)
            {
                skippedNoPageMapping++;
                logger.LogWarning("附件 {AttachmentId} ({Title}) 被跳过: 所属页面 {PageId} 未被映射", attachment.Id, attachment.Title, attachment.PageId.Value);
                continue;
            }

            var creatorId = mapper.MapUserKey(attachment.CreatorKey);

            // 从ZIP提取文件
            var entryName = attachmentFiles[attachment.Id];
            var entry = archive.GetEntry(entryName);
            if (entry == null)
            {
                skippedNoEntry++;
                logger.LogWarning("附件 {AttachmentId} ({Title}) 被跳过: ZIP 条目 {EntryName} 不存在", attachment.Id, attachment.Title, entryName);
                continue;
            }

            // 创建存储路径
            var storagePath = Path.Combine("attachments", pageId.Value.ToString(), attachment.Id.ToString());
            var fullPath = Path.Combine(uploadsDir, pageId.Value.ToString(), attachment.Id.ToString());

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

            // 获取实际文件大小
            var actualFileSize = new FileInfo(filePath).Length;

            // 根据文件扩展名推断 ContentType
            var contentType = GetContentTypeFromFile(attachment.Title);

            var newAttachment = mapper.MapAttachment(
                attachment,
                pageId.Value,
                creatorId,
                Path.Combine(storagePath, SanitizeFileName(attachment.Title))
            );

            // 覆盖从 XML 获取的值，使用实际的文件信息
            newAttachment.FileHash = fileHash;
            newAttachment.FileSize = actualFileSize;
            newAttachment.ContentType = contentType;

            var existing = await db.Db.Queryable<Attachment>()
                .Where(a => a.FileName == attachment.Title && a.PageId == pageId.Value)
                .FirstAsync();

            long newId;
            if (existing != null)
            {
                if (options.OverwriteExisting)
                {
                    newAttachment.Id = existing.Id;
                    newAttachment.CreatedAt = existing.CreatedAt;
                    await db.Db.Updateable(newAttachment).ExecuteCommandAsync();
                }
                newId = existing.Id;
            }
            else
            {
                // 强制插入原始 ID
                await db.Db.Ado.ExecuteCommandAsync(
                    "INSERT INTO \"attachments\" (\"id\", \"filename\", \"filesize\", \"contenttype\", \"storagepath\", \"pageid\", \"creatorid\", \"version\", \"isdeleted\", \"createdat\", \"updatedat\", \"filehash\") " +
                    "VALUES (@Id::bigint, @FileName, @FileSize::bigint, @ContentType, @StoragePath, @PageId::bigint, @CreatorId::bigint, @Version::integer, @IsDeleted::boolean, @CreatedAt::timestamp, @UpdatedAt::timestamp, @FileHash)",
                    new
                    {
                        newAttachment.Id,
                        newAttachment.FileName,
                        newAttachment.FileSize,
                        newAttachment.ContentType,
                        newAttachment.StoragePath,
                        newAttachment.PageId,
                        newAttachment.CreatorId,
                        newAttachment.Version,
                        newAttachment.IsDeleted,
                        newAttachment.CreatedAt,
                        newAttachment.UpdatedAt,
                        newAttachment.FileHash
                    });
                newId = attachment.Id;
            }

            mapper.AddAttachmentMapping(attachment.Id, newId);

            // 添加到 URL 映射（用于内容转换）
            // 静态文件通过 /uploads 路径访问，所以需要添加此前缀
            var accessUrl = $"/uploads/{newAttachment.StoragePath.Replace("\\", "/")}";
            attachmentUrlMap[attachment.Id] = accessUrl;

            count++;
        }

        logger.LogInformation("附件导入完成: 成功 {SuccessCount}, 跳过 - 无PageId: {NoPageId}, 无文件: {NoFile}, 无页面映射: {NoMapping}, 无ZIP条目: {NoEntry}",
            count, skippedNoPageId, skippedNoFile, skippedNoPageMapping, skippedNoEntry);
        logger.LogInformation("附件 URL 映射构建完成，共 {Count} 条", attachmentUrlMap.Count);

        // 尝试更新序列
        try { await db.Db.Ado.ExecuteCommandAsync("SELECT setval('\"attachments_id_seq\"', (SELECT MAX(id) FROM \"attachments\"))"); } catch { }

        return count;
    }

    /// <summary>
    /// 导入评论
    /// </summary>
    private async Task<int> ImportCommentsAsync(
        AppDbContext db,
        ILogger<ConfluenceImportService> logger,
        List<ConfluenceComment> comments,
        DataMappingService mapper,
        ImportOptions options)
    {
        var count = 0;

        foreach (var comment in comments)
        {
            if (!comment.PageId.HasValue || comment.IsDeleted)
                continue;

            var pageId = mapper.GetMappedPageId(comment.PageId.Value);
            if (pageId == null)
                continue;

            var newComment = mapper.MapComment(comment, pageId.Value);

            // 检查重复
            var existing = await db.Db.Queryable<PageComment>()
                .Where(c => c.PageId == pageId.Value && c.Content == comment.Content)
                .FirstAsync();

            long newId;
            if (existing != null)
            {
                if (options.OverwriteExisting)
                {
                    newComment.Id = existing.Id;
                    await db.Db.Updateable(newComment).ExecuteCommandAsync();
                }
                newId = existing.Id;
            }
            else
            {
                // 强制插入原始 ID
                await db.Db.Ado.ExecuteCommandAsync(
                    "INSERT INTO \"page_comments\" (\"id\", \"content\", \"pageid\", \"userid\", \"createdat\", \"updatedat\") " +
                    "VALUES (@Id::bigint, @Content, @PageId::bigint, @UserId::bigint, @CreatedAt::timestamp, @UpdatedAt::timestamp)",
                    new
                    {
                        newComment.Id,
                        newComment.Content,
                        newComment.PageId,
                        newComment.UserId,
                        newComment.CreatedAt,
                        newComment.UpdatedAt
                    });
                newId = comment.Id;
            }

            mapper.AddCommentMapping(comment.Id, newId);
            count++;
        }

        // 尝试更新序列
        try { await db.Db.Ado.ExecuteCommandAsync("SELECT setval('\"page_comments_id_seq\"', (SELECT MAX(id) FROM \"page_comments\"))"); } catch { }

        return count;
    }


    /// <summary>
    /// 更新导入进度
    /// </summary>
    private async Task UpdateProgressAsync(AppDbContext db, ImportTask task, ImportProgress progress)
    {
        try
        {
            var progressJson = JsonSerializer.Serialize(progress, AppJsonContext.Default.ImportProgress);
            task.Progress = progressJson;

            await db.Db.Ado.ExecuteCommandAsync(
                "UPDATE \"import_tasks\" SET \"progress\" = @progress::jsonb WHERE \"id\" = @id",
                new { progress = progressJson, id = task.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新导入进度失败: {TaskId}", task.Id);
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

    /// <summary>
    /// 根据文件扩展名获取 MIME 类型
    /// </summary>
    private static string GetContentTypeFromFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            // 图片
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".webp" => "image/webp",
            ".ico" => "image/x-icon",

            // 文档
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",

            // 文本
            ".txt" => "text/plain",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".md" => "text/markdown",

            // 压缩
            ".zip" => "application/zip",
            ".rar" => "application/vnd.rar",
            ".7z" => "application/x-7z-compressed",
            ".tar" => "application/x-tar",
            ".gz" => "application/gzip",

            // 视频
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".webm" => "video/webm",

            // 音频
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",

            // 默认
            _ => "application/octet-stream"
        };
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
