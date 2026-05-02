using Microsoft.AspNetCore.Http;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 审计日志服务接口
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// 将审计日志条目加入队列（异步非阻塞）
    /// </summary>
    ValueTask EnqueueAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录配置变更审计日志（自动生成diff）
    /// </summary>
    ValueTask EnqueueChangeAsync(
        HttpContext httpContext,
        string entityType,
        object oldValue,
        object newValue,
        string? entityId = null);

    /// <summary>
    /// 记录操作审计日志（无diff）
    /// </summary>
    ValueTask EnqueueActionAsync(
        HttpContext httpContext,
        string actionType,
        string entityType,
        string? entityId = null,
        string? details = null);

    /// <summary>
    /// 手动刷新队列（用于健康检查或关闭时）
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// 获取队列大小（用于监控）
    /// </summary>
    int GetQueueSize();
}
