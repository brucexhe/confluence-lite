namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 缓存统计 DTO
/// </summary>
public class CacheStatsDto
{
    public int KeyCount { get; set; }
    public long MemoryUsed { get; set; }
    public long MemoryTotal { get; set; }
    public double HitRate { get; set; }
}

/// <summary>
/// 缓存类型 DTO
/// </summary>
public class CacheTypeDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public long Memory { get; set; }
}

/// <summary>
/// 缓存键 DTO
/// </summary>
public class CacheKeyDto
{
    public string Key { get; set; } = string.Empty;
    public long Size { get; set; }
    public string? Ttl { get; set; }
}

/// <summary>
/// 清空缓存请求
/// </summary>
public class ClearCacheRequest
{
    public string Type { get; set; } = string.Empty;
}
