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
    public async Task<ConfluenceBackupData> ParseBackupAsync(string zipPath, Func<ImportProgress, Task>? progressCallback = null)
    {
        var result = new ConfluenceBackupData();
        var progress = new ImportProgress { CurrentStep = "正在打开备份文件..." };
        if (progressCallback != null)
            await progressCallback(progress);

        using var archive = ZipFile.OpenRead(zipPath);

        // 解析 entities.xml
        var entitiesEntry = archive.GetEntry("entities.xml");
        if (entitiesEntry == null)
            throw new InvalidOperationException("备份文件中缺少 entities.xml");

        progress.CurrentStep = "正在解析 entities.xml...";
        if (progressCallback != null)
            await progressCallback(progress);

        using (var entitiesStream = entitiesEntry.Open())
        {
            await ParseEntitiesAsync(entitiesStream, result, progressCallback);
        }

        // 解析附件路径
        progress.CurrentStep = "正在扫描附件文件...";
        if (progressCallback != null)
            await progressCallback(progress);

        ParseAttachmentFiles(archive, result);

        _logger.LogInformation("解析完成: {SpaceCount} 个空间, {PageCount} 个页面, {UserCount} 个用户, {AttachmentCount} 个附件, {CommentCount} 个评论, {BodyCount} 个正文, {DescCount} 个描述",
            result.Spaces.Count, result.Pages.Count, result.Users.Count,
            result.Attachments.Count, result.Comments.Count, result.BodyContents.Count, result.SpaceDescriptions.Count);

        return result;
    }

    /// <summary>
    /// 解析 entities.xml 文件
    /// </summary>
    private async Task ParseEntitiesAsync(Stream xmlStream, ConfluenceBackupData result, Func<ImportProgress, Task>? progressCallback)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            DtdProcessing = DtdProcessing.Ignore
        };

        using var reader = XmlReader.Create(xmlStream, settings);
        var totalCount = 0;

        while (await reader.ReadAsync())
        {
            // Confluence 7.x 实体通常是 <object class="...">
            if (reader.NodeType == XmlNodeType.Element && reader.LocalName == "object")
            {
                var classAttr = reader.GetAttribute("class");

                if (!string.IsNullOrEmpty(classAttr))
                {
                    try
                    {
                        // 使用 ReadSubtree 并使用 XElement 加载，这是最稳健的解析方式
                        using var subReader = reader.ReadSubtree();
                        var element = await XElement.LoadAsync(subReader, LoadOptions.None, default);

                        // Confluence 7.19 ID 格式为子元素 <id name="id">52854786</id>
                        // 兼容旧版本的属性格式 id="..."
                        var idVal = element.Attribute("id")?.Value ?? element.Element("id")?.Value;

                        if (!string.IsNullOrEmpty(idVal))
                        {
                            // 尝试转换为 long，如果是非数字 ID（如某些 User Key），转换失败则为 0，
                            // 但 CreateEntityFromElement 内部会从属性中获取真正的 Key
                            long.TryParse(idVal, out var idAttribute);

                            var entity = CreateEntityFromElement(classAttr, idAttribute, element);
                            if (entity != null)
                            {
                                AddEntityToResult(result, entity);
                                totalCount++;

                                if (totalCount % 500 == 0 && progressCallback != null)
                                {
                                    await progressCallback(new ImportProgress
                                    {
                                        TotalItems = totalCount,
                                        ProcessedItems = totalCount,
                                        CurrentStep = $"解析 entities.xml 中 (已处理 {totalCount} 个实体)..."
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析 XML 元素失败，Class: {Class}", classAttr);
                    }
                }
            }
        }

        var progress = new ImportProgress
        {
            TotalItems = totalCount,
            ProcessedItems = totalCount,
            CurrentStep = "解析完成"
        };
        if (progressCallback != null)
            await progressCallback(progress);
    }

    /// <summary>
    /// 从 XElement 创建实体对象
    /// </summary>
    private ConfluenceEntity? CreateEntityFromElement(string className, long id, XElement element)
    {
        if (string.IsNullOrEmpty(className) || id <= 0)
            return null;

        var shortClassName = className.Contains('.') ? className.Split('.').Last() : className;
        var properties = ParsePropertiesFromElement(element);

        ConfluenceEntity entity;
        switch (shortClassName)
        {
            case "Space":
                entity = new ConfluenceSpace { Id = id, Class = className };
                break;
            case "Page":
            case "BlogPost":
                entity = new ConfluencePage { Id = id, Class = className };
                break;
            case "User":
            case "ConfluenceUser":
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
    /// 从 XElement 解析属性
    /// </summary>
    private Dictionary<string, object?> ParsePropertiesFromElement(XElement element)
    {
        var properties = new Dictionary<string, object?>();

        // 1. 解析 property 元素
        foreach (var prop in element.Elements("property"))
        {
            var name = prop.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;

            // Confluence 7.19 引用格式: <property name="space"><id name="id">123</id></property>
            var idChild = prop.Element("id");
            if (idChild != null)
            {
                var idName = idChild.Attribute("name")?.Value ?? "id";
                var idVal = idChild.Value;
                properties[$"{name}.{idName}"] = idVal;
                properties[name] = idVal; // 同时也直接存储
            }
            else
            {
                // 兼容逻辑：检查子 element (旧版本引用类型)
                var eleRef = prop.Element("element");
                if (eleRef != null)
                {
                    var refIdStr = eleRef.Attribute("id")?.Value;
                    if (long.TryParse(refIdStr, out var refId))
                    {
                        properties[$"{name}.id"] = refId;
                        properties[name] = refId; 
                    }
                }
                else
                {
                    // 尝试从 value 属性读取，否则读取节点文本（自动包含 CDATA）
                    var valueAttr = prop.Attribute("value");
                    properties[name] = valueAttr != null ? valueAttr.Value : prop.Value;
                }
            }
        }

        // 2. 解析 collection 元素
        foreach (var coll in element.Elements("collection"))
        {
            var name = coll.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name)) continue;

            // Confluence 7.19 集合元素格式: <collection><element><id name="id">123</id></element></collection>
            var firstElement = coll.Element("element");
            if (firstElement != null)
            {
                var idChild = firstElement.Element("id");
                if (idChild != null)
                {
                    var idName = idChild.Attribute("name")?.Value ?? "id";
                    properties[$"{name}.{idName}"] = idChild.Value;
                }
                else
                {
                    // 兼容旧版本：<element id="123" />
                    var refIdStr = firstElement.Attribute("id")?.Value;
                    if (long.TryParse(refIdStr, out var refId))
                    {
                        properties[$"{name}.id"] = refId;
                    }
                }
            }
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
            // 规范化路径分隔符
            var normalizedPath = entry.FullName.Replace('\\', '/');
            // Confluence 7.x 格式: attachments/{attId}/{pageId}/{version}
            var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
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
