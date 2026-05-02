namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 系统信息 DTO
/// </summary>
public class SystemInfoDto
{
    public string Version { get; set; } = string.Empty;
    public string BuildTime { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public double UptimeSeconds { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string Arch { get; set; } = string.Empty;
    public CpuInfoDto Cpu { get; set; } = new();
    public MemoryInfoDto Memory { get; set; } = new();
    public DatabaseInfoDto Database { get; set; } = new();
}

/// <summary>
/// CPU 信息
/// </summary>
public class CpuInfoDto
{
    public int Cores { get; set; }
    public double Usage { get; set; }
}

/// <summary>
/// 内存信息
/// </summary>
public class MemoryInfoDto
{
    public long Used { get; set; }
    public long Total { get; set; }
    public long Free { get; set; }
}

/// <summary>
/// 数据库信息
/// </summary>
public class DatabaseInfoDto
{
    public string Type { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 系统统计数据
/// </summary>
public class SystemStatsDto
{
    public int UserCount { get; set; }
    public int WorkspaceCount { get; set; }
    public int PageCount { get; set; }
    public int AttachmentCount { get; set; }
}
