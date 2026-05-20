using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace ConfluenceLite.Api.Services.Confluence;

/// <summary>
/// Confluence Storage Format 内容转换服务
/// </summary>
public class ConfluenceContentConverter
{
    private readonly Dictionary<long, string> _attachmentUrlMap;
    private readonly Dictionary<long, long> _pageIdMap;
    private readonly ILogger<ConfluenceContentConverter> _logger;

    // XML 命名空间
    private static readonly XNamespace Ac = "http://atlassian.com/content";
    private static readonly XNamespace Ri = "http://atlassian.com/resource/identifier";

    public ConfluenceContentConverter(
        Dictionary<long, string> attachmentUrlMap,
        Dictionary<long, long>? pageIdMap = null,
        ILogger<ConfluenceContentConverter>? logger = null)
    {
        _attachmentUrlMap = attachmentUrlMap;
        _pageIdMap = pageIdMap ?? new Dictionary<long, long>();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ConfluenceContentConverter>.Instance;
    }

    /// <summary>
    /// 将 Confluence Storage Format 转换为 HTML
    /// </summary>
    public string ConvertToHtml(string confluenceContent, long pageId)
    {
        if (string.IsNullOrWhiteSpace(confluenceContent))
            return string.Empty;

        try
        {
            if (confluenceContent.Contains("ac:image")
            || confluenceContent.Contains("ri:attachment"))
            {
                     // 调试：输出内容的前 200 个字符
            _logger.LogDebug("Page {PageId} 内容预览: {ContentPreview}...", pageId,
                confluenceContent.Length > 200 ? confluenceContent.Substring(0, 200) : confluenceContent);

            }
       
            // 首先尝试解析为 XML
            if (confluenceContent.TrimStart().StartsWith("<"))
            {
                _logger.LogDebug("内容以 < 开头，尝试解析为 XML");
                return ConvertXmlContent(confluenceContent, pageId);
            }

            // 如果不是 XML 格式，直接返回（可能已经是纯文本或 HTML）
            _logger.LogDebug("内容不以 < 开头，直接返回");
            return confluenceContent;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "转换 Confluence 内容失败，使用简化转换: {Message}", ex.Message);
            return FallbackConvert(confluenceContent);
        }
    }

    /// <summary>
    /// 转换 XML 格式的 Confluence 内容
    /// </summary>
    private string ConvertXmlContent(string content, long pageId)
    {
        try
        {
            var doc = XDocument.Parse($"<root xmlns:ac=\"http://atlassian.com/content\" xmlns:ri=\"http://atlassian.com/resource/identifier\">{content}</root>");
            var root = doc.Root;

            if (root == null)
                return content;

            // 调试：输出 XML 中的一些元素信息
            var allElements = root.Descendants().ToList();

            // 使用 LocalName 查找，不依赖命名空间
            var imageElements = root.Descendants().Where(e => e.Name.LocalName == "image").ToList();
            var linkElements = root.Descendants().Where(e => e.Name.LocalName == "link").ToList();
            var attachmentElements = root.Descendants().Where(e => e.Name.LocalName == "attachment").ToList();

            _logger.LogDebug("Page {PageId} XML 解析结果: 总元素数={TotalCount}, image={ImageCount}, link={LinkCount}, attachment={AttachmentCount}",
                pageId, allElements.Count, imageElements.Count, linkElements.Count, attachmentElements.Count);

            // 如果没有找到 image 元素，输出一些元素的 LocalName 来调试
            if (imageElements.Count == 0 && allElements.Count > 0)
            {
                var sampleElements = allElements.Take(10).Select(e => e.Name.LocalName).ToList();
                _logger.LogDebug("前 10 个元素的 LocalName: {Elements}", string.Join(", ", sampleElements));
            }

            // 转换各种元素
            ConvertImageReferences(root);
            ConvertFileLinks(root);
            ConvertMacros(root);
            ConvertInternalLinks(root);
            ConvertStructuredMacroTasks(root);

            // 移除命名空间并返回 HTML
            return RemoveNamespaces(root.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Page {PageId} XML 解析失败，使用简化转换: {Message}", pageId, ex.Message);
            return FallbackConvert(content);
        }
    }

    /// <summary>
    /// 转换图片引用（Confluence 的 ac:image 元素可能包含图片或文件）
    /// </summary>
    private void ConvertImageReferences(XElement root)
    {
        // 使用 LocalName 查找，不依赖命名空间匹配
        var images = root.Descendants().Where(e => e.Name.LocalName == "image").ToList();

        _logger.LogDebug("找到 {Count} 个 image 元素", images.Count);

        foreach (var image in images)
        {
            try
            {
                // 使用 LocalName 查找 attachment 和 page 元素
                var attachment = image.Descendants().FirstOrDefault(e => e.Name.LocalName == "attachment");
                var pageRef = image.Descendants().FirstOrDefault(e => e.Name.LocalName == "page");

                if (attachment == null)
                    continue;

                // 获取 filename 属性（可能带命名空间前缀）
                var filename = attachment.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "filename")?.Value;

                // 尝试获取附件 ID
                long? attachmentId = null;

                // 优先从 <ri:attachment> 获取 attachment-id
                var attachmentIdAttr = attachment.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "attachment-id");
                if (attachmentIdAttr != null && long.TryParse(attachmentIdAttr.Value, out var attId))
                {
                    attachmentId = attId;
                }
                // 备用：从 <ri:page> 获取 content-id
                else if (pageRef != null)
                {
                    var contentId = pageRef.Attributes()
                        .FirstOrDefault(a => a.Name.LocalName == "content-id")?.Value;
                    if (long.TryParse(contentId, out var id))
                    {
                        attachmentId = id;
                    }
                }

                // 根据文件扩展名判断是图片还是文件
                XElement? replacementTag = null;

                if (IsImageFile(filename))
                {
                    // 是图片文件，生成 img 标签
                    _logger.LogDebug("文件 {Filename} 是图片，生成 img 标签", filename);
                    replacementTag = BuildImageTag(filename, attachmentId);
                }
                else
                {
                    // 是其他文件，生成 a 标签
                    _logger.LogDebug("文件 {Filename} 不是图片，生成 file 链接标签", filename);
                    replacementTag = BuildFileLinkTag(filename, attachmentId);
                }

                if (replacementTag != null)
                {
                    image.ReplaceWith(replacementTag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "转换图片引用失败");
            }
        }
    }

    /// <summary>
    /// 判断文件是否为图片
    /// </summary>
    private static bool IsImageFile(string? filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" or ".webp" or ".ico";
    }

    /// <summary>
    /// 构建 img 标签
    /// </summary>
    private XElement? BuildImageTag(string? filename, long? attachmentId)
    {
        string? src = null;

        if (attachmentId.HasValue && _attachmentUrlMap.TryGetValue(attachmentId.Value, out var mappedUrl))
        {
            src = mappedUrl;
        }
        else if (!string.IsNullOrEmpty(filename))
        {
            // 尝试通过文件名查找
            var match = _attachmentUrlMap.FirstOrDefault(x =>
                x.Value.EndsWith(filename, StringComparison.OrdinalIgnoreCase));

            if (match.Key != 0)
            {
                src = match.Value;
            }
        }

        if (string.IsNullOrEmpty(src))
        {
            _logger.LogWarning("无法找到附件 URL: {Filename}, {AttachmentId}", filename, attachmentId);
            return null;
        }

        return new XElement("img",
            new XAttribute("class", "image"),
            new XAttribute("src", src),
            new XAttribute("alt", filename ?? ""),
            new XAttribute("loading", "lazy")
        );
    }

    /// <summary>
    /// 转换文件链接
    /// </summary>
    private void ConvertFileLinks(XElement root)
    {
        // 使用 LocalName 查找，不依赖命名空间匹配
        var allLinks = root.Descendants().Where(e => e.Name.LocalName == "link").ToList();
        _logger.LogDebug("找到 {Count} 个 link 元素", allLinks.Count);

        var links = allLinks
            .Where(l => l.Descendants().Any(d => d.Name.LocalName == "attachment"))
            .ToList();

        _logger.LogDebug("其中 {Count} 个 link 元素包含附件引用", links.Count);

        foreach (var link in links)
        {
            try
            {
                // 使用 LocalName 查找 attachment 元素
                var attachment = link.Descendants().FirstOrDefault(e => e.Name.LocalName == "attachment");
                if (attachment == null)
                    continue;

                // 获取 filename 属性（可能带命名空间前缀）
                var filename = attachment.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "filename")?.Value;

                // 尝试获取附件 ID
                long? attachmentId = null;

                // 优先从 <ri:attachment> 获取 attachment-id
                var attachmentIdAttr = attachment.Attributes()
                    .FirstOrDefault(a => a.Name.LocalName == "attachment-id");
                if (attachmentIdAttr != null && long.TryParse(attachmentIdAttr.Value, out var attId))
                {
                    attachmentId = attId;
                }

                // 构建 a 标签
                var linkTag = BuildFileLinkTag(filename, attachmentId);

                if (linkTag != null)
                {
                    link.ReplaceWith(linkTag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "转换文件链接失败");
            }
        }
    }

    /// <summary>
    /// 构建文件链接 a 标签
    /// </summary>
    private XElement? BuildFileLinkTag(string? filename, long? attachmentId)
    {
        string? href = null;

        if (attachmentId.HasValue && _attachmentUrlMap.TryGetValue(attachmentId.Value, out var mappedUrl))
        {
            href = mappedUrl;
        }
        else if (!string.IsNullOrEmpty(filename))
        {
            // 尝试通过文件名查找
            var match = _attachmentUrlMap.FirstOrDefault(x =>
                x.Value.EndsWith(filename, StringComparison.OrdinalIgnoreCase));

            if (match.Key != 0)
            {
                href = match.Value;
            }
        }

        if (string.IsNullOrEmpty(href))
        {
            _logger.LogWarning("无法找到文件链接 URL: {Filename}, {AttachmentId}", filename, attachmentId);
            // 如果找不到链接，至少显示文件名
            if (!string.IsNullOrEmpty(filename))
            {
                return new XElement("a",
                    new XAttribute("class", "file"),
                    new XAttribute("href", "#"),
                    filename
                );
            }
            return null;
        }

        return new XElement("a",
            new XAttribute("class", "file"),
            new XAttribute("href", href),
            new XAttribute("src", href),
            new XAttribute("target", "_blank"),
            new XAttribute("rel", "noopener"),
            filename ?? "文件"
        );
    }

    /// <summary>
    /// 转换宏 (macros)
    /// </summary>
    private void ConvertMacros(XElement root)
    {
        // Info 宏
        ConvertInfoMacro(root);

        // Note 宏
        ConvertNoteMacro(root);

        // Warning 宏
        ConvertWarningMacro(root);

        // Tip 宏
        ConvertTipMacro(root);

        // Code 宏
        ConvertCodeMacro(root);

        // Panel 宏
        ConvertPanelMacro(root);

        // Expand 宏
        ConvertExpandMacro(root);

        // Children 宏 - 转换为占位符
        ConvertChildrenMacro(root);

        // Table 宏
        ConvertTableMacros(root);

        // View File 宏
        ConvertViewFileMacro(root);
    }

    /// <summary>
    /// 转换 Info 宏
    /// </summary>
    private void ConvertInfoMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "info")
            .ToList();

        foreach (var macro in macros)
        {
            var body = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();
            var content = body?.ToString() ?? "";

            var replacement = new XElement("div",
                new XAttribute("class", "confluence-macro info-macro"),
                new XElement("div",
                    new XAttribute("class", "macro-header"),
                    new XElement("span", new XText("ℹ️ Info"))
                ),
                new XElement("div",
                    new XAttribute("class", "macro-body"),
                    new XElement("div", new XAttribute("class", "macro-content"), XElement.Parse("<div>" + content + "</div>").Nodes())
                )
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换 Note 宏
    /// </summary>
    private void ConvertNoteMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "note")
            .ToList();

        foreach (var macro in macros)
        {
            var body = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();
            var content = body?.ToString() ?? "";

            var replacement = new XElement("div",
                new XAttribute("class", "confluence-macro note-macro"),
                new XElement("div",
                    new XAttribute("class", "macro-header"),
                    new XElement("span", new XText("📝 Note"))
                ),
                new XElement("div",
                    new XAttribute("class", "macro-body"),
                    new XElement("div", new XAttribute("class", "macro-content"), XElement.Parse("<div>" + content + "</div>").Nodes())
                )
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换 Warning 宏
    /// </summary>
    private void ConvertWarningMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "warning")
            .ToList();

        foreach (var macro in macros)
        {
            var body = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();
            var content = body?.ToString() ?? "";

            var replacement = new XElement("div",
                new XAttribute("class", "confluence-macro warning-macro"),
                new XElement("div",
                    new XAttribute("class", "macro-header"),
                    new XElement("span", new XText("⚠️ Warning"))
                ),
                new XElement("div",
                    new XAttribute("class", "macro-body"),
                    new XElement("div", new XAttribute("class", "macro-content"), XElement.Parse("<div>" + content + "</div>").Nodes())
                )
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换 Tip 宏
    /// </summary>
    private void ConvertTipMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "tip")
            .ToList();

        foreach (var macro in macros)
        {
            var body = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();
            var content = body?.ToString() ?? "";

            var replacement = new XElement("div",
                new XAttribute("class", "confluence-macro tip-macro"),
                new XElement("div",
                    new XAttribute("class", "macro-header"),
                    new XElement("span", new XText("💡 Tip"))
                ),
                new XElement("div",
                    new XAttribute("class", "macro-body"),
                    new XElement("div", new XAttribute("class", "macro-content"), XElement.Parse("<div>" + content + "</div>").Nodes())
                )
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换 Code 宏
    /// </summary>
    private void ConvertCodeMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "code")
            .ToList();

        foreach (var macro in macros)
        {
            var languageParam = macro.Descendants(Ac + "parameter")
                .FirstOrDefault(p => p.Attribute(Ac + "name")?.Value == "language");
            var language = languageParam?.Value ?? "text";

            var plainBody = macro.Descendants(Ac + "plain-text-body").FirstOrDefault();
            var richBody = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();

            string code;
            if (plainBody != null)
            {
                code = plainBody.Value;
            }
            else if (richBody != null)
            {
                code = richBody.ToString(SaveOptions.DisableFormatting);
            }
            else
            {
                code = "";
            }

            var replacement = new XElement("pre",
                new XAttribute("class", $"language-{language}"),
                new XElement("code", new XAttribute("class", $"language-{language}"), new XText(code))
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换 Panel 宏
    /// </summary>
    private void ConvertPanelMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "panel")
            .ToList();

        foreach (var macro in macros)
        {
            var body = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();
            var content = body?.ToString() ?? "";

            var titleParam = macro.Descendants(Ac + "parameter")
                .FirstOrDefault(p => p.Attribute(Ac + "name")?.Value == "title");
            var title = titleParam?.Value;

            var replacement = new XElement("div",
                new XAttribute("class", "confluence-macro panel-macro")
            );

            if (!string.IsNullOrEmpty(title))
            {
                replacement.Add(new XElement("div",
                    new XAttribute("class", "panel-title"),
                    new XText(title)
                ));
            }

            replacement.Add(new XElement("div",
                new XAttribute("class", "panel-content"),
                XElement.Parse("<div>" + content + "</div>").Nodes()
            ));

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换 Expand 宏
    /// </summary>
    private void ConvertExpandMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "expand")
            .ToList();

        foreach (var macro in macros)
        {
            var body = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();
            var content = body?.ToString() ?? "";

            var titleParam = macro.Descendants(Ac + "parameter")
                .FirstOrDefault(p => p.Attribute(Ac + "name")?.Value == "title");
            var title = titleParam?.Value ?? "展开/收起";

            var replacement = new XElement("details",
                new XAttribute("class", "confluence-macro expand-macro"),
                new XElement("summary",
                    new XAttribute("class", "expand-title"),
                    new XText(title)
                ),
                new XElement("div",
                    new XAttribute("class", "expand-content"),
                    XElement.Parse("<div>" + content + "</div>").Nodes()
                )
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换 Children 宏（页面列表）
    /// </summary>
    private void ConvertChildrenMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "children")
            .ToList();

        foreach (var macro in macros)
        {
            // Children 宏在导入时无法动态生成，使用占位符
            var replacement = new XElement("div",
                new XAttribute("class", "confluence-macro children-macro-placeholder"),
                new XElement("p",
                    new XAttribute("class", "text-muted"),
                    new XText("📄 子页面列表（需要在导入后手动更新）")
                )
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 转换表格宏
    /// </summary>
    private void ConvertTableMacros(XElement root)
    {
        // TODO: 实现表格宏的转换
        // 目前表格通常是直接存储的 HTML，不需要特殊处理
    }

    /// <summary>
    /// 转换 View File 宏
    /// </summary>
    private void ConvertViewFileMacro(XElement root)
    {
        var macros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "view-file")
            .ToList();

        foreach (var macro in macros)
        {
            try
            {
                var nameParam = macro.Descendants(Ac + "parameter")
                    .FirstOrDefault(p => p.Attribute(Ac + "name")?.Value == "name");

                var attachment = nameParam?.Descendants(Ri + "attachment").FirstOrDefault();
                if (attachment == null) continue;

                var filename = attachment.Attribute(Ri + "filename")?.Value;
                var attachmentIdAttr = attachment.Attribute(Ri + "attachment-id")?.Value;
                long? attachmentId = null;
                if (!string.IsNullOrEmpty(attachmentIdAttr) && long.TryParse(attachmentIdAttr, out var id))
                {
                    attachmentId = id;
                }

                string? url = null;
                if (attachmentId.HasValue && _attachmentUrlMap.TryGetValue(attachmentId.Value, out var mappedUrl))
                {
                    url = mappedUrl;
                }
                else if (!string.IsNullOrEmpty(filename))
                {
                    var match = _attachmentUrlMap.FirstOrDefault(x =>
                        x.Value.EndsWith(filename, StringComparison.OrdinalIgnoreCase));
                    if (match.Key != 0)
                    {
                        url = match.Value;
                    }
                }

                if (!string.IsNullOrEmpty(url))
                {
                    var fileType="";
                    if(filename.EndsWith(".mp4") || filename.EndsWith(".avi") || filename.EndsWith(".mov"))
                    {
                        fileType="video";
                    }
                    else if(filename.EndsWith(".mp3") || filename.EndsWith(".wav") || filename.EndsWith(".ogg"))
                    {
                        fileType="audio";
                    }
                    else if(filename.EndsWith(".doc") || filename.EndsWith(".docx") || filename.EndsWith(".pdf"))
                    {
                        fileType="file";
                    }
                    else if(filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".gif"))
                    {
                        fileType="image";
                    }

                    // 使用用户要求的 src 属性，同时保留标准的 href
                    var linkTag = new XElement("a",
                        new XAttribute("class", fileType),
                        new XAttribute("href", url), 
                        new XAttribute("target", "_blank"),
                        new XAttribute("rel", "noopener"),
                        filename ?? "文件"
                    );
                    macro.ReplaceWith(linkTag);
                }
                else if (!string.IsNullOrEmpty(filename))
                {
                    // 找不到 URL 时，至少保留文件名
                    macro.ReplaceWith(new XElement("a",
                        new XAttribute("class", "file"),
                        new XAttribute("href", "#"),
                        new XAttribute("src", "#"),
                        filename
                    ));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "转换 view-file 宏失败");
            }
        }
    }

    /// <summary>
    /// 转换内部链接
    /// </summary>
    private void ConvertInternalLinks(XElement root)
    {
        var links = root.Descendants(Ac + "link").ToList();

        foreach (var link in links)
        {
            try
            {
                var pageRef = link.Descendants(Ri + "page").FirstOrDefault();
                if (pageRef == null)
                    continue;

                var contentId = pageRef.Attribute(Ri + "content-id")?.Value;
                if (string.IsNullOrEmpty(contentId) || !long.TryParse(contentId, out var confluencePageId))
                    continue;

                // 映射到新的页面 ID
                var targetPageId = _pageIdMap.GetValueOrDefault(confluencePageId, confluencePageId);

                // 获取链接文本
                var linkText = link.Descendants(Ac + "plain-text-link-body").FirstOrDefault()?.Value
                    ?? link.Value
                    ?? $"页面 {targetPageId}";

                var replacement = new XElement("a",
                    new XAttribute("href", $"/pages/{targetPageId}"),
                    new XAttribute("class", "internal-link"),
                    new XText(linkText)
                );

                link.ReplaceWith(replacement);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "转换内部链接失败");
            }
        }
    }

    /// <summary>
    /// 转换结构化宏任务
    /// </summary>
    private void ConvertStructuredMacroTasks(XElement root)
    {
        // 任务列表宏
        var taskMacros = root.Descendants(Ac + "structured-macro")
            .Where(m => m.Attribute(Ac + "name")?.Value == "tasklist")
            .ToList();

        foreach (var macro in taskMacros)
        {
            var body = macro.Descendants(Ac + "rich-text-body").FirstOrDefault();
            var content = body?.ToString() ?? "";

            var replacement = new XElement("div",
                new XAttribute("class", "confluence-macro tasklist-macro"),
                XElement.Parse("<div>" + content + "</div>").Nodes()
            );

            macro.ReplaceWith(replacement);
        }
    }

    /// <summary>
    /// 移除 XML 命名空间，使输出更接近标准 HTML
    /// </summary>
    private string RemoveNamespaces(string xml)
    {
        // 移除 ac: 和 ri: 命名空间声明
        xml = Regex.Replace(xml, @"xmlns:ac=""[^""]*""", "");
        xml = Regex.Replace(xml, @"xmlns:ri=""[^""]*""", "");
        xml = Regex.Replace(xml, @"<root[^>]*>|</root>", "", RegexOptions.IgnoreCase);

        // 移除自闭合标签中的空格
        xml = Regex.Replace(xml, @"<(\w+)([^>]*)/>\s*", "<$1$2 />");

        return xml;
    }

    /// <summary>
    /// 简化的降级转换（当 XML 解析失败时使用）
    /// </summary>
    private string FallbackConvert(string content)
    {
        // 移除宏包装，保留内容
        content = Regex.Replace(content,
            @"<ac:structured-macro[^>]*ac:name=""info""[^>]*>.*?<ac:rich-text-body>(.*?)</ac:rich-text-body>.*?</ac:structured-macro>",
            @"<div class=""confluence-macro info-macro""><div class=""macro-header"">ℹ️ Info</div><div class=""macro-body"">$1</div></div>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        content = Regex.Replace(content,
            @"<ac:structured-macro[^>]*ac:name=""warning""[^>]*>.*?<ac:rich-text-body>(.*?)</ac:rich-text-body>.*?</ac:structured-macro>",
            @"<div class=""confluence-macro warning-macro""><div class=""macro-header"">⚠️ Warning</div><div class=""macro-body"">$1</div></div>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // 简单的附件引用转换（区分图片和文件）
        content = Regex.Replace(content,
            @"<ri:attachment ri:filename=""([^""]+)""[^>]*/>",
            m => {
                var filename = m.Groups[1].Value;
                var match = _attachmentUrlMap.FirstOrDefault(x =>
                    x.Value.EndsWith(filename, StringComparison.OrdinalIgnoreCase));

                if (match.Key != 0)
                {
                    // 根据文件扩展名判断是图片还是文件
                    if (IsImageFile(filename))
                    {
                        return $@"<img class=""image"" src=""{match.Value}"" alt=""{filename}"" loading=""lazy"" />";
                    }
                    else
                    {
                        return $@"<a class=""file"" href=""{match.Value}"" src=""{match.Value}"" target=""_blank"" rel=""noopener"">{filename}</a>";
                    }
                }

                return $@"<span class=""broken-attachment"">[文件: {filename}]</span>";
            },
            RegexOptions.IgnoreCase);

        // 移除其他命名空间标签
        content = Regex.Replace(content, @"<ac:[^>]+>", "", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"</ac:[^>]+>", "", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"<ri:[^>]+/>", "", RegexOptions.IgnoreCase);

        return content;
    }
}
