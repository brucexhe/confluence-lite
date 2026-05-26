namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 备份配置 DTO
/// </summary>
public class BackupConfigDto
{
    public bool Enabled { get; set; }
    public int IntervalDays { get; set; } = 1;
    public List<string> Content { get; set; } = new() { "database", "attachments", "config" };
    public int RetentionDays { get; set; } = 30;
}
