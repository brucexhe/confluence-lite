using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.Middleware;
using ConfluenceLite.Api.Attributes;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 审计日志服务 - 单例后台服务，使用Channel异步批量写入
/// </summary>
public class AuditLogService : BackgroundService, IAuditLogService
{
    private readonly Channel<AuditLogEntry> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly AuditLogOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditLogService(IServiceProvider serviceProvider, IOptions<AuditLogOptions> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;

        var channelOptions = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        };
        _channel = Channel.CreateUnbounded<AuditLogEntry>(channelOptions);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public ValueTask EnqueueAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return ValueTask.CompletedTask;

        return _channel.Writer.WriteAsync(entry, cancellationToken);
    }

    public ValueTask EnqueueChangeAsync(
        HttpContext httpContext,
        string entityType,
        object oldValue,
        object newValue,
        string? entityId = null)
    {
        var currentUser = httpContext.Items["CurrentUser"] as CurrentUser;

        var actionType = $"{entityType}.config.update";
        var entry = new AuditLogEntry
        {
            ActorId = currentUser?.IsAuthenticated == true ? currentUser.UserId : null,
            ActorUsername = currentUser?.Username,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = long.TryParse(entityId, out var id) ? id : null,
            IpAddress = GetIpAddress(httpContext),
            UserAgent = GetUserAgent(httpContext),
            Timestamp = DateTime.UtcNow
        };

        // Generate diff
        var diffGenerator = _serviceProvider.GetRequiredService<IAuditDiffGenerator>();
        entry.Details = diffGenerator.GenerateDiff(oldValue, newValue, httpContext.Request.Path, httpContext.Request.Method);

        return EnqueueAsync(entry);
    }

    public ValueTask EnqueueActionAsync(
        HttpContext httpContext,
        string actionType,
        string entityType,
        string? entityId = null,
        string? details = null)
    {
        var currentUser = httpContext.Items["CurrentUser"] as CurrentUser;

        var entry = new AuditLogEntry
        {
            ActorId = currentUser?.IsAuthenticated == true ? currentUser.UserId : null,
            ActorUsername = currentUser?.Username,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = long.TryParse(entityId, out var id) ? id : null,
            Details = details,
            IpAddress = GetIpAddress(httpContext),
            UserAgent = GetUserAgent(httpContext),
            Timestamp = DateTime.UtcNow
        };

        return EnqueueAsync(entry);
    }

    public Task FlushAsync()
    {
        // Signal completion and wait for channel to drain
        _channel.Writer.Complete();
        return Task.CompletedTask;
    }

    public int GetQueueSize()
    {
        // Estimated queue size (not exact due to concurrent access)
        return 0;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[AuditLog] Audit log service started");

        var batch = new List<AuditLogEntry>(_options.BatchSize);
        var reader = _channel.Reader;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Wait for items or timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.FlushIntervalSeconds));

                try
                {
                    var hasItem = await reader.WaitToReadAsync(cts.Token);
                    if (!hasItem) break;

                    // Read as many items as possible
                    while (reader.TryRead(out var entry) && batch.Count < _options.BatchSize)
                    {
                        batch.Add(entry);
                    }

                    // Process batch
                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch, stoppingToken);
                        batch.Clear();
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Timeout - process any pending items
                    if (batch.Count > 0)
                    {
                        await ProcessBatchAsync(batch, stoppingToken);
                        batch.Clear();
                    }
                }
            }

            // Process remaining items before shutdown
            while (reader.TryRead(out var entry))
            {
                batch.Add(entry);
            }
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch, stoppingToken);
            }

            Console.WriteLine("[AuditLog] Audit log service stopped");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLog] Error in audit log service: {ex.Message}");
        }
    }

    private async Task ProcessBatchAsync(List<AuditLogEntry> batch, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var auditLogs = batch.Select(entry => new AuditLog
            {
                ActorId = entry.ActorId,
                ActionType = entry.ActionType,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Details = entry.Details,
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent,
                CreatedAt = entry.Timestamp
            }).ToList();

            await dbContext.Db.Insertable(auditLogs).ExecuteCommandAsync(cancellationToken);

            Console.WriteLine($"[AuditLog] Wrote {batch.Count} audit log entries");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuditLog] Failed to write audit logs: {ex.Message}");
        }
    }

    private static string? GetIpAddress(HttpContext context)
    {
        // Check for forwarded headers (proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private static string? GetUserAgent(HttpContext context)
    {
        return context.Request.Headers["User-Agent"].FirstOrDefault();
    }
}
