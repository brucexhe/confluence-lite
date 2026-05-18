namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 导入任务DTO
/// </summary>
public class ImportTaskDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public ImportProgressDto? Progress { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long CreatedById { get; set; }
}

/// <summary>
/// 导入进度DTO
/// </summary>
public class ImportProgressDto
{
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int FailedItems { get; set; }
    public int ProgressPercent { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
    public Dictionary<string, int> EntityCounts { get; set; } = new();
}

/// <summary>
/// 导入选项请求DTO
/// </summary>
public class ImportOptionsRequest
{
    public bool ImportUsers { get; set; } = true;
    public bool ImportSpaces { get; set; } = true;
    public bool ImportPages { get; set; } = true;
    public bool ImportAttachments { get; set; } = true;
    public bool ImportComments { get; set; } = true;
    public bool OverwriteExisting { get; set; } = true;
}

/// <summary>
/// 导入验证结果DTO
/// </summary>
public class ImportValidationResult
{
    public bool IsValid { get; set; }
    public string? Error { get; set; }
    public string? Version { get; set; }
    public int EntityCount { get; set; }
    public Dictionary<string, int> EntityTypeCounts { get; set; } = new();
}
