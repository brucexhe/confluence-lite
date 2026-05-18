using System.Text.Json.Serialization;

namespace ConfluenceLite.Api.Models.Confluence;

/// <summary>
/// Confluence 对象基类
/// </summary>
public class ConfluenceEntity
{
    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, object?> Properties { get; set; } = new();

    /// <summary>
    /// 获取属性值
    /// </summary>
    protected T? GetProperty<T>(string key, T? defaultValue = default)
    {
        if (Properties.TryGetValue(key, out var value))
        {
            if (value == null) return defaultValue;
            if (value is T tValue) return tValue;

            // 尝试将字符串转换为目标类型（如 long, int, bool）
            try
            {
                var strValue = value.ToString();
                if (string.IsNullOrEmpty(strValue)) return defaultValue;

                if (typeof(T) == typeof(long) && long.TryParse(strValue, out var l)) return (T)(object)l;
                if (typeof(T) == typeof(int) && int.TryParse(strValue, out var i)) return (T)(object)i;
                if (typeof(T) == typeof(bool))
                {
                    if (bool.TryParse(strValue, out var b)) return (T)(object)b;
                    if (strValue == "1" || strValue.Equals("true", StringComparison.OrdinalIgnoreCase)) return (T)(object)true;
                    if (strValue == "0" || strValue.Equals("false", StringComparison.OrdinalIgnoreCase)) return (T)(object)false;
                }
                
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 获取字符串属性
    /// </summary>
    protected string GetStringProperty(string key, string defaultValue = "")
    {
        return GetProperty<string>(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// 获取整数属性
    /// </summary>
    protected int GetIntProperty(string key, int defaultValue = 0)
    {
        return GetProperty<int>(key, defaultValue);
    }

    /// <summary>
    /// 获取长整数属性
    /// </summary>
    protected long GetLongProperty(string key, long defaultValue = 0)
    {
        return GetProperty<long>(key, defaultValue);
    }

    /// <summary>
    /// 获取日期时间属性
    /// </summary>
    protected DateTime? GetDateTimeProperty(string key)
    {
        var value = GetProperty<string>(key);
        if (string.IsNullOrEmpty(value)) return null;

        if (DateTime.TryParse(value, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// 获取布尔属性
    /// </summary>
    protected bool GetBoolProperty(string key, bool defaultValue = false)
    {
        return GetProperty<bool>(key, defaultValue);
    }
}

/// <summary>
/// Confluence 空间实体
/// </summary>
public class ConfluenceSpace : ConfluenceEntity
{
    /// <summary>
    /// 空间名称
    /// </summary>
    public string Name => GetStringProperty("name");

    /// <summary>
    /// 空间键
    /// </summary>
    public string Key => GetStringProperty("key");

    /// <summary>
    /// 描述ID（需要从 SpaceDescription 获取）
    /// </summary>
    public long? DescriptionId => GetLongProperty("description.id");

    /// <summary>
    /// 主页ID
    /// </summary>
    public long? HomePageId => GetLongProperty("homePage.id");

    /// <summary>
    /// 创建者用户键
    /// </summary>
    public string? CreatorKey => GetStringProperty("creator.key");

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreationDate => GetDateTimeProperty("creationDate");

    /// <summary>
    /// 最后修改者用户键
    /// </summary>
    public string? LastModifierKey => GetStringProperty("lastModifier.key");

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModificationDate => GetDateTimeProperty("lastModificationDate");
}

/// <summary>
/// Confluence 页面实体
/// </summary>
public class ConfluencePage : ConfluenceEntity
{
    /// <summary>
    /// 页面标题
    /// </summary>
    public string Title => GetStringProperty("title");

    /// <summary>
    /// 版本号
    /// </summary>
    public int Version => GetIntProperty("version", 1);

    /// <summary>
    /// 创建者用户键
    /// </summary>
    public string? CreatorKey => GetStringProperty("creator.key");

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreationDate => GetDateTimeProperty("creationDate");

    /// <summary>
    /// 最后修改者用户键
    /// </summary>
    public string? LastModifierKey => GetStringProperty("lastModifier.key");

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModificationDate => GetDateTimeProperty("lastModificationDate");

    /// <summary>
    /// 内容状态 (current, draft, etc.)
    /// </summary>
    public string ContentStatus => GetStringProperty("contentStatus", "current");

    /// <summary>
    /// 所属空间ID
    /// </summary>
    public long? SpaceId => GetLongProperty("space.id");

    /// <summary>
    /// 父页面ID
    /// </summary>
    public long? ParentId => GetLongProperty("parent.id");

    /// <summary>
    /// 正文内容ID（需要从 BodyContent 获取）
    /// </summary>
    public long? BodyContentId => GetLongProperty("bodyContents.id");
}

/// <summary>
/// Confluence 用户实体
/// </summary>
public class ConfluenceUser : ConfluenceEntity
{
    /// <summary>
    /// 用户键（Confluence 中的唯一标识）
    /// </summary>
    public string Key => GetStringProperty("key");

    /// <summary>
    /// 用户名
    /// </summary>
    public string Name => GetStringProperty("name");

    /// <summary>
    /// 小写用户名
    /// </summary>
    public string LowerName => GetStringProperty("lowerName");

    /// <summary>
    /// 全名
    /// </summary>
    public string FullName => GetStringProperty("fullName");

    /// <summary>
    /// 邮箱地址
    /// </summary>
    public string? EmailAddress => GetStringProperty("emailAddress");

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool Active => GetBoolProperty("active", true);
}

/// <summary>
/// Confluence 附件实体
/// </summary>
public class ConfluenceAttachment : ConfluenceEntity
{
    /// <summary>
    /// 附件标题（文件名）
    /// </summary>
    public string Title => GetStringProperty("title");

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long PageSize => GetLongProperty("pageSize");

    /// <summary>
    /// 所属页面ID
    /// </summary>
    public long? PageId => GetLongProperty("page.id");

    /// <summary>
    /// 创建者用户键
    /// </summary>
    public string? CreatorKey => GetStringProperty("creator.key");

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreationDate => GetDateTimeProperty("creationDate");

    /// <summary>
    /// 内容类型（MIME类型）
    /// </summary>
    public string? ContentType => GetStringProperty("contentType");

    /// <summary>
    /// 版本
    /// </summary>
    public int Version => GetIntProperty("version", 1);
}

/// <summary>
/// Confluence 评论实体
/// </summary>
public class ConfluenceComment : ConfluenceEntity
{
    /// <summary>
    /// 评论内容
    /// </summary>
    public string Content => GetStringProperty("content");

    /// <summary>
    /// 所属页面ID
    /// </summary>
    public long? PageId => GetLongProperty("page.id");

    /// <summary>
    /// 创建者用户键
    /// </summary>
    public string? CreatorKey => GetStringProperty("creator.key");

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime? CreationDate => GetDateTimeProperty("creationDate");

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime? LastModificationDate => GetDateTimeProperty("lastModificationDate");

    /// <summary>
    /// 是否已删除
    /// </summary>
    public bool IsDeleted => GetBoolProperty("isDeleted");
}

/// <summary>
/// Confluence 正文内容实体
/// </summary>
public class ConfluenceBodyContent : ConfluenceEntity
{
    /// <summary>
    /// 正文内容（存储格式）
    /// </summary>
    public string Body => GetStringProperty("body");

    /// <summary>
    /// 内容类型（storage, wiki, etc.）
    /// </summary>
    public string? Contenttype => GetStringProperty("contenttype", "storage");

    /// <summary>
    /// 所属页面ID
    /// </summary>
    public long? PageId => GetLongProperty("page.id");
}

/// <summary>
/// Confluence 空间描述实体
/// </summary>
public class ConfluenceSpaceDescription : ConfluenceEntity
{
    /// <summary>
    /// 描述内容
    /// </summary>
    public string Body => GetStringProperty("body");

    /// <summary>
    /// 所属空间ID
    /// </summary>
    public long? SpaceId => GetLongProperty("space.id");
}
