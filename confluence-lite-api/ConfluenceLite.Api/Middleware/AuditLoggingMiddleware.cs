using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Data;

namespace ConfluenceLite.Api.Middleware;

/// <summary>
/// 审计日志中间件 - 自动记录系统配置变更
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuditLogService _auditService;
    private readonly AuditLogOptions _options;

    // 需要审计的路径模式 (方法 + 路径前缀)
    private static readonly Dictionary<string, (string Method, string Path, string ActionType, string EntityType)[]> AuditedEndpoints = new()
    {
        ["/api/system"] = new[]
        {
            ("PUT", "/api/system/security-config", "security.config.update", "security"),
            ("PUT", "/api/system/auth-config", "auth.config.update", "auth"),
            ("PUT", "/api/system/mail-config", "mail.config.update", "mail"),
            ("PUT", "/api/system/site-config", "site.config.update", "site"),
            ("PUT", "/api/system/office-preview-config", "office.config.update", "office"),
            ("PUT", "/api/system/display-config", "display.config.update", "display"),
        }
    };

    public AuditLoggingMiddleware(
        RequestDelegate next,
        IAuditLogService auditService,
        IOptions<AuditLogOptions> options)
    {
        _next = next;
        _auditService = auditService;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.Enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Check if this is an audited endpoint
        var auditConfig = FindAuditConfig(path, method);
        if (auditConfig == null)
        {
            await _next(context);
            return;
        }

        var (actionType, entityType) = auditConfig.Value;

        // For config updates, we'll log them in the route handler
        // The middleware just marks the context for potential audit
        context.Items["Audit_ActionType"] = actionType;
        context.Items["Audit_EntityType"] = entityType;

        await _next(context);

        // Note: For backup/cache operations that don't go through config update handlers,
        // we could add logging here based on response status
        if (path.StartsWith("/api/system/backup") && method is "POST" or "DELETE")
        {
            await LogBackupOperation(context, path, method);
        }
        else if (path.StartsWith("/api/system/cache") && method == "POST")
        {
            await LogCacheOperation(context, path, method);
        }
    }

    private static (string ActionType, string EntityType)? FindAuditConfig(string path, string method)
    {
        foreach (var (basePath, endpoints) in AuditedEndpoints)
        {
            if (!path.StartsWith(basePath))
                continue;

            foreach (var (endpointMethod, endpointPath, actionType, entityType) in endpoints)
            {
                if (method == endpointMethod && path.StartsWith(endpointPath))
                {
                    return (actionType, entityType);
                }
            }
        }

        return null;
    }

    private async Task LogBackupOperation(HttpContext context, string path, string method)
    {
        // Extract backup ID from path if available
        var segments = path.Split('/');
        string? entityId = null;
        if (segments.Length > 4 && long.TryParse(segments[4], out var backupId))
        {
            entityId = backupId.ToString();
        }

        var actionType = method switch
        {
            "POST" when path.EndsWith("/restore") => "backup.restore",
            "POST" => "backup.create",
            "DELETE" => "backup.delete",
            _ => null
        };

        if (actionType != null)
        {
            await _auditService.EnqueueActionAsync(
                context,
                actionType,
                "backup",
                entityId);
        }
    }

    private async Task LogCacheOperation(HttpContext context, string path, string method)
    {
        var actionType = path.EndsWith("clear-all") ? "cache.clear_all" : "cache.clear";

        await _auditService.EnqueueActionAsync(
            context,
            actionType,
            "cache",
            null);
    }
}
