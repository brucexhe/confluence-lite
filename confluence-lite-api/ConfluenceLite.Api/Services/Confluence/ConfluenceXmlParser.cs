using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.IO.Compression;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Models.Confluence;

namespace ConfluenceLite.Api.Services.Confluence;

/// <summary>
/// Confluence XML 解析服务
/// </summary>
public class ConfluenceXmlParser
{
    private readonly ILogger<ConfluenceXmlParser> _logger;

    public ConfluenceXmlParser(ILogger<ConfluenceXmlParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证备份文件有效性
    /// </summary>
    public async Task<(bool IsValid, string? Error, string? Version)> ValidateBackupAsync(string zipPath)
    {
        try
        {
            if (!File.Exists(zipPath))
                return (false, "文件不存在", null);

            using var archive = ZipFile.OpenRead(zipPath);

            // 检查必需文件
            var entitiesEntry = archive.GetEntry("entities.xml");
            if (entitiesEntry == null)
                return (false, "无效的 Confluence 备份文件：缺少 entities.xml", null);

            // 检查版本信息
            var descriptorEntry = archive.GetEntry("exportDescriptor.xml");
            if (descriptorEntry != null)
            {
                using var descriptorStream = descriptorEntry.Open();
                using var reader = new StreamReader(descriptorStream);
                var content = await reader.ReadToEndAsync();
                var descriptor = XDocument.Parse(content);
                var buildNumberElement = descriptor.Descendants("buildNumber").FirstOrDefault();
                if (buildNumberElement != null)
                {
                    var buildNumber = int.Parse(buildNumberElement.Value);
                    var version = GetConfluenceVersion(buildNumber);
                    if (!version.StartsWith("7."))
                    {
                        return (false, $"不支持的 Confluence 版本：{version}。仅支持 Confluence 7.x 版本。", version);
                    }
                    return (true, null, version);
                }
            }

            return (true, null, "Unknown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证备份文件失败: {Message}", ex.Message);
            return (false, $"备份文件验证失败: {ex.Message}", null);
        }
    }

    /// <summary>
    /// 从 ZIP 中提取并解析所有数据（Confluence 7.x）
    /// </summary>
    public async Task<ConfluenceBackupData> ParseBackupAsync(string zipPath, Action<ImportProgress>? progressCallback = null)
    {
        var result = new ConfluenceBackupData();
        var progress = new ImportProgress { CurrentStep = "正在打开备份文件..." };
        progressCallback?.Invoke(progress);

        using var archive = ZipFile.OpenRead(zipPath);

        // 解析 entities.xml
        var entitiesEntry = archive.GetEntry("entities.xml");
        if (entitiesEntry == null)
            throw new InvalidOperationException("备份文件中缺少 entities.xml");

        progress.CurrentStep = "正在解析 entities.xml...";
        progressCallback?.Invoke(progress);

        using (var entitiesStream = entitiesEntry.Open())
        {
            await ParseEntitiesAsync(entitiesStream, result, progressCallback);
        }

        // 解析附件路径
        progress.CurrentStep = "正在扫描附件文件...";
        progressCallback?.Invoke(progress);

        ParseAttachmentFiles(archive, result);

        _logger.LogInformation("解析完成: {SpaceCount} 个空间, {PageCount} 个页面, {UserCount} 个用户, {AttachmentCount} 个附件, {CommentCount} 个评论",
            result.Spaces.Count, result.Pages.Count, result.Users.Count,
            result.Attachments.Count, result.Comments.Count);

        return result;
    }

    /// <summary>
    /// 解析 entities.xml 文件
    /// </summary>
    private async Task ParseEntitiesAsync(Stream xmlStream, ConfluenceBackupData result, Action<ImportProgress>? progressCallback)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        using var reader = XmlReader.Create(xmlStream, settings);
        var currentElement = new StringBuilder();
        var classAttribute = string.Empty;
        var idAttribute = 0L;
        var totalCount = 0;

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "object")
            {
                classAttribute = reader.GetAttribute("class") ?? string.Empty;
                var idStr = reader.GetAttribute("id");
                long.TryParse(idStr, out idAttribute);

                currentElement.Clear();
            }
            else if (reader.NodeType == XmlNodeType.Text)
            {
                currentElement.Append(await reader.GetValueAsync());
            }
            else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "object")
            {
                var entity = CreateEntity(classAttribute, idAttribute, currentElement.ToString());
                if (entity != null)
                {
                    AddEntityToResult(result, entity);
                    totalCount++;
                }
            }
        }

        var progress = new ImportProgress
        {
            TotalItems = totalCount,
            ProcessedItems = totalCount,
            CurrentStep = "解析完成"
        };
        progressCallback?.Invoke(progress);
    }

    /// <summary>
    /// 创建实体对象
    /// </summary>
    private ConfluenceEntity? CreateEntity(string className, long id, string xmlContent)
    {
        if (string.IsNullOrEmpty(className) || id <= 0)
            return null;

        var properties = ParseProperties(xmlContent);

        ConfluenceEntity entity;
        switch (className)
        {
            case "Space":
                entity = new ConfluenceSpace { Id = id, Class = className };
                break;
            case "Page":
                entity = new ConfluencePage { Id = id, Class = className };
                break;
            case "User":
                entity = new ConfluenceUser { Id = id, Class = className };
                break;
            case "Attachment":
                entity = new ConfluenceAttachment { Id = id, Class = className };
                break;
            case "Comment":
                entity = new ConfluenceComment { Id = id, Class = className };
                break;
            case "BodyContent":
                entity = new ConfluenceBodyContent { Id = id, Class = className };
                break;
            case "SpaceDescription":
                entity = new ConfluenceSpaceDescription { Id = id, Class = className };
                break;
            default:
                return null;
        }

        entity.Properties = properties;
        return entity;
    }

    /// <summary>
    /// 解析属性和集合
    /// </summary>
    private Dictionary<string, object?> ParseProperties(string xmlContent)
    {
        var properties = new Dictionary<string, object?>();

        if (string.IsNullOrWhiteSpace(xmlContent))
            return properties;

        try
        {
            var doc = XDocument.Parse($"<root>{xmlContent}</root>");
            var root = doc.Root;

            if (root == null)
                return properties;

            // 解析 property 元素
            foreach (var prop in root.Elements("property"))
            {
                var name = prop.Attribute("name")?.Value;
                var value = prop.Attribute("value")?.Value;

                if (!string.IsNullOrEmpty(name))
                {
                    properties[name] = value;
                }
            }

            // 解析 collection 元素
            foreach (var coll in root.Elements("collection"))
            {
                var name = coll.Attribute("name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    properties[$"{name}.id"] = name; // 标记为集合引用
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析属性失败: {Message}", ex.Message);
        }

        return properties;
    }

    /// <summary>
    /// 将实体添加到结果中
    /// </summary>
    private void AddEntityToResult(ConfluenceBackupData result, ConfluenceEntity entity)
    {
        switch (entity)
        {
            case ConfluenceSpace space:
                result.Spaces.Add(space);
                break;
            case ConfluencePage page:
                result.Pages.Add(page);
                break;
            case ConfluenceUser user:
                result.Users.Add(user);
                break;
            case ConfluenceAttachment attachment:
                result.Attachments.Add(attachment);
                break;
            case ConfluenceComment comment:
                result.Comments.Add(comment);
                break;
            case ConfluenceBodyContent bodyContent:
                result.BodyContents.Add(bodyContent);
                break;
            case ConfluenceSpaceDescription spaceDescription:
                result.SpaceDescriptions.Add(spaceDescription);
                break;
        }
    }

    /// <summary>
    /// 解析附件文件路径
    /// </summary>
    private void ParseAttachmentFiles(ZipArchive archive, ConfluenceBackupData result)
    {
        var attachmentEntries = archive.Entries
            .Where(e => e.FullName.StartsWith("attachments/", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var entry in attachmentEntries)
        {
            // Confluence 7.x 格式: attachments/{attId}/{pageId}/{version}
            var parts = entry.FullName.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3 && long.TryParse(parts[1], out var attachmentId))
            {
                result.AttachmentFiles[attachmentId] = entry.FullName;
            }
        }
    }

    /// <summary>
    /// 根据构建号获取 Confluence 版本
    /// </summary>
    private string GetConfluenceVersion(int buildNumber)
    {
        // Confluence 版本映射
        return buildNumber switch
        {
            >= 8800 => "8.8+",
            >= 8700 => "8.7",
            >= 8600 => "8.6",
            >= 8500 => "8.5",
            >= 8400 => "8.4",
            >= 8300 => "8.3",
            >= 8200 => "8.2",
            >= 8100 => "8.1",
            >= 8000 => "8.0",
            >= 7800 => "7.19+",
            >= 7700 => "7.18",
            >= 7600 => "7.17",
            >= 7500 => "7.16",
            >= 7400 => "7.15",
            >= 7300 => "7.14",
            >= 7200 => "7.13",
            >= 7100 => "7.12",
            >= 7000 => "7.11",
            >= 6900 => "7.10",
            >= 6800 => "7.9",
            >= 6700 => "7.8",
            >= 6600 => "7.7",
            >= 6500 => "7.6",
            >= 6400 => "7.5",
            >= 6300 => "7.4",
            >= 6200 => "7.3",
            >= 6100 => "7.2",
            >= 6000 => "7.1",
            >= 5900 => "7.0",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Confluence 备份数据容器
/// </summary>
public class ConfluenceBackupData
{
    public List<ConfluenceSpace> Spaces { get; set; } = new();
    public List<ConfluencePage> Pages { get; set; } = new();
    public List<ConfluenceUser> Users { get; set; } = new();
    public List<ConfluenceAttachment> Attachments { get; set; } = new();
    public List<ConfluenceComment> Comments { get; set; } = new();
    public List<ConfluenceBodyContent> BodyContents { get; set; } = new();
    public List<ConfluenceSpaceDescription> SpaceDescriptions { get; set; } = new();

    /// <summary>
    /// 附件ID到ZIP路径的映射
    /// </summary>
    public Dictionary<long, string> AttachmentFiles { get; set; } = new();
}
