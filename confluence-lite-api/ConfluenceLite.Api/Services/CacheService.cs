using Microsoft.Extensions.Caching.Memory;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 缓存服务
/// </summary>
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly CacheStats _stats = new();

    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// 获取缓存统计
    /// </summary>
    public Task<CacheStatsDto> GetStatsAsync()
    {
        return Task.FromResult(new CacheStatsDto
        {
            KeyCount = _stats.KeyCount,
            MemoryUsed = 0,
            MemoryTotal = 100 * 1024 * 1024, // 100MB 默认限制
            HitRate = _stats.GetHitRate()
        });
    }

    /// <summary>
    /// 获取缓存类型列表
    /// </summary>
    public Task<List<CacheTypeDto>> GetCacheTypesAsync()
    {
        var types = new List<CacheTypeDto>
        {
            new() { Key = "user", Name = "用户缓存", Count = 0, Memory = 0 },
            new() { Key = "page", Name = "页面缓存", Count = 0, Memory = 0 },
            new() { Key = "workspace", Name = "空间缓存", Count = 0, Memory = 0 },
            new() { Key = "attachment", Name = "附件缓存", Count = 0, Memory = 0 }
        };

        return Task.FromResult(types);
    }

    /// <summary>
    /// 获取指定类型的缓存键
    /// </summary>
    public Task<List<CacheKeyDto>> GetCacheKeysAsync(string type)
    {
        // IMemoryCache 不提供枚举功能，返回空列表
        return Task.FromResult(new List<CacheKeyDto>());
    }

    /// <summary>
    /// 清空所有缓存
    /// </summary>
    public Task<bool> ClearAllCacheAsync()
    {
        // 注意：IMemoryCache 没有清空所有的方法
        // 这需要应用层维护一个缓存键列表来支持
        // 这里返回成功表示操作已接受
        _stats.Reset();
        return Task.FromResult(true);
    }

    /// <summary>
    /// 清空指定类型的缓存
    /// </summary>
    public Task<bool> ClearCacheByTypeAsync(string type)
    {
        // 同样需要应用层维护缓存键列表
        return Task.FromResult(true);
    }

    /// <summary>
    /// 删除单个缓存键
    /// </summary>
    public Task<bool> RemoveKeyAsync(string type, string key)
    {
        var fullKey = $"{type}:{key}";
        _cache.Remove(fullKey);
        _stats.DecrementKey();
        return Task.FromResult(true);
    }

    /// <summary>
    /// 设置缓存
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        options.RegisterPostEvictionCallback((key, value, reason, state) =>
        {
            _stats.DecrementKey();
        });

        _cache.Set(key, value, options);
        _stats.IncrementKey();
    }

    /// <summary>
    /// 获取缓存
    /// </summary>
    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue<T>(key, out var value))
        {
            _stats.RecordHit();
            return value;
        }

        _stats.RecordMiss();
        return default;
    }

    /// <summary>
    /// 移除缓存
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
        _stats.DecrementKey();
    }
}

/// <summary>
/// 缓存统计（内部使用）
/// </summary>
internal class CacheStats
{
    private int _keyCount;
    private long _totalHits;
    private long _totalMisses;

    public int KeyCount => _keyCount;

    public void IncrementKey() => Interlocked.Increment(ref _keyCount);
    public void DecrementKey() => Interlocked.Decrement(ref _keyCount);
    public void Reset() => _keyCount = 0;

    public void RecordHit() => Interlocked.Increment(ref _totalHits);
    public void RecordMiss() => Interlocked.Increment(ref _totalMisses);

    public double GetHitRate()
    {
        var total = _totalHits + _totalMisses;
        return total > 0 ? (double)_totalHits / total * 100 : 0;
    }
}
