namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 搜索建议项
/// </summary>
public record SearchSuggestionDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "page"; // page, attachment
    public string? ContentType { get; set; }
    public string? SpaceKey { get; set; }
}

/// <summary>
/// 搜索结果项
/// </summary>
public record SearchResultDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; } // 包含高亮的片段或摘要
    public string Type { get; set; } = "page"; // page, attachment
    public string? ContentType { get; set; }
    public string? SpaceName { get; set; }
    public string? SpaceKey { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatorName { get; set; }
}
