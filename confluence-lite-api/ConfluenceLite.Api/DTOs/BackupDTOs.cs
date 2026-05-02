namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 备份 DTO
/// </summary>
public class BackupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// 创建备份请求
/// </summary>
public class CreateBackupRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string> Options { get; set; } = new(); // database, attachments, config
}

/// <summary>
/// 还原备份请求
/// </summary>
public class RestoreBackupRequest
{
    public bool Confirmed { get; set; }
}
