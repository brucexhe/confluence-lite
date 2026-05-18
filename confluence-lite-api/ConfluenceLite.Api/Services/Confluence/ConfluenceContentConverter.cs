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
            // 首先尝试解析为 XML
            if (confluenceContent.TrimStart().StartsWith("<"))
            {
                return ConvertXmlContent(confluenceContent, pageId);
            }

            // 如果不是 XML 格式，直接返回（可能已经是纯文本或 HTML）
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
            var doc = XDocument.Parse($"<root>{content}</root>");
            var root = doc.Root;

            if (root == null)
                return content;

            // 转换各种元素
            ConvertImageReferences(root);
            ConvertMacros(root);
            ConvertInternalLinks(root);
            ConvertStructuredMacroTasks(root);

            // 移除命名空间并返回 HTML
            return RemoveNamespaces(root.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "XML 解析失败，使用简化转换");
            return FallbackConvert(content);
        }
    }

    /// <summary>
    /// 转换图片引用
    /// </summary>
    private void ConvertImageReferences(XElement root)
    {
        // 查找所有 ac:image 元素
        var images = root.Descendants(Ac + "image").ToList();

        foreach (var image in images)
        {
            try
            {
                var attachment = image.Descendants(Ri + "attachment").FirstOrDefault();
                var pageRef = image.Descendants(Ri + "page").FirstOrDefault();

                if (attachment == null)
                    continue;

                var filename = attachment.Attribute(Ri + "filename")?.Value;

                // 尝试获取附件 ID
                long? attachmentId = null;
                if (pageRef != null)
                {
                    var contentId = pageRef.Attribute(Ri + "content-id")?.Value;
                    if (long.TryParse(contentId, out var id))
                    {
                        attachmentId = id;
                    }
                }

                // 构建 img 标签
                var imgTag = BuildImageTag(filename, attachmentId);

                if (imgTag != null)
                {
                    image.ReplaceWith(imgTag);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "转换图片引用失败");
            }
        }
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
            new XAttribute("src", src),
            new XAttribute("alt", filename ?? ""),
            new XAttribute("loading", "lazy")
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
        xml = Regex.Replace(xml, @"<root>|</root>", "", RegexOptions.IgnoreCase);

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

        // 简单的图片引用转换
        content = Regex.Replace(content,
            @"<ri:attachment ri:filename=""([^""]+)""[^>]*/>",
            m => {
                var filename = m.Groups[1].Value;
                var match = _attachmentUrlMap.FirstOrDefault(x =>
                    x.Value.EndsWith(filename, StringComparison.OrdinalIgnoreCase));

                if (match.Key != 0)
                {
                    return $@"<img src=""{match.Value}"" alt=""{filename}"" loading=""lazy"" />";
                }

                return $@"<span class=""broken-image"">[图片: {filename}]</span>";
            },
            RegexOptions.IgnoreCase);

        // 移除其他命名空间标签
        content = Regex.Replace(content, @"<ac:[^>]+>", "", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"</ac:[^>]+>", "", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"<ri:[^>]+/>", "", RegexOptions.IgnoreCase);

        return content;
    }
}
