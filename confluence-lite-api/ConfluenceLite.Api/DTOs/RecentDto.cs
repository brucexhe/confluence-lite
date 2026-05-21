namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 最近访问 DTO
/// </summary>
public class RecentDto
{
    public long PageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SpaceKey { get; set; } = string.Empty;
    public string SpaceName { get; set; } = string.Empty;
    public string? CreatorName { get; set; }
    public DateTime VisitedAt { get; set; }
}
