using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using System.Text;
using SqlSugar;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 日志服务
/// </summary>
public class LogService
{
    private readonly AppDbContext _db;

    public LogService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取日志列表（分页、筛选）
    /// </summary>
    public async Task<(PagedResponse<LogEntryDto>?, string?)> GetLogsAsync(LogQueryRequest request)
    {
        try
        {
            var query = _db.Db.Queryable<AuditLog>()
                .LeftJoin<User>((log, user) => log.ActorId == user.Id);

            // 按操作类型筛选
            if (!string.IsNullOrEmpty(request.Level))
            {
                query = query.Where((log, user) => log.ActionType == request.Level);
            }

            // 按日期范围筛选
            if (request.StartDate.HasValue)
            {
                query = query.Where((log, user) => log.CreatedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.AddDays(1);
                query = query.Where((log, user) => log.CreatedAt < endDate);
            }

            // 搜索内容
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                query = query.Where((log, user) =>
                    (log.EntityType != null && log.EntityType.Contains(request.SearchText)) ||
                    (log.Details != null && log.Details.Contains(request.SearchText))
                );
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending((log, user) => log.Id)
                .Select((log, user) => new LogEntryDto
                {
                    Id = log.Id,
                    Level = log.ActionType,
                    Message = $"{log.ActionType} - {log.EntityType}",
                    Details = log.Details,
                    Source = log.IpAddress ?? "System",
                    Timestamp = log.CreatedAt
                })
                .ToPageListAsync(request.Page, request.PageSize);

            var response = new PagedResponse<LogEntryDto>
            {
                Items = items,
                Total = total,
                Page = request.Page,
                PageSize = request.PageSize
            };

            return (response, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 导出日志为 TXT
    /// </summary>
    public async Task<(byte[]?, string?)> ExportLogsAsync(LogExportRequest request)
    {
        try
        {
            var query = _db.Db.Queryable<AuditLog>()
                .LeftJoin<User>((log, user) => log.ActorId == user.Id);

            // 应用筛选条件
            if (!string.IsNullOrEmpty(request.Level))
            {
                query = query.Where((log, user) => log.ActionType == request.Level);
            }

            if (request.StartDate.HasValue)
            {
                query = query.Where((log, user) => log.CreatedAt >= request.StartDate.Value);
            }

            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value.AddDays(1);
                query = query.Where((log, user) => log.CreatedAt < endDate);
            }

            // 限制导出数量
            var logs = await query
                .OrderByDescending((log, user) => log.Id)
                .Select((log, user) => new LogEntryDto
                {
                    Id = log.Id,
                    Level = log.ActionType,
                    Message = $"{log.ActionType} - {log.EntityType}",
                    Details = log.Details,
                    Source = log.IpAddress ?? "System",
                    Timestamp = log.CreatedAt
                })
                .Take(10000)
                .ToListAsync();

            // 构建 TXT 格式内容
            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("系统日志导出");
            sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"记录数: {logs.Count}");
            sb.AppendLine("========================================");
            sb.AppendLine();

            foreach (var log in logs)
            {
                sb.AppendLine($"[{log.Timestamp:yyyy-MM-dd HH:mm:ss}] {log.Level}");
                sb.AppendLine($"来源: {log.Source}");
                sb.AppendLine($"消息: {log.Message}");
                if (!string.IsNullOrEmpty(log.Details))
                {
                    sb.AppendLine($"详情: {log.Details}");
                }
                sb.AppendLine(new string('-', 80));
                sb.AppendLine();
            }

            var content = Encoding.UTF8.GetBytes(sb.ToString());
            return (content, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 清理旧日志
    /// </summary>
    public async Task<(bool, string?)> CleanupOldLogsAsync(int daysToKeep)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            var deletedCount = await _db.Db.Deleteable<AuditLog>()
                .Where(log => log.CreatedAt < cutoffDate)
                .ExecuteCommandAsync();

            return (true, $"已清理 {deletedCount} 条旧日志");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 获取日志统计信息
    /// </summary>
    public async Task<(LogStatsDto?, string?)> GetLogStatsAsync()
    {
        try
        {
            var total = await _db.Db.Queryable<AuditLog>().CountAsync();
            var today = DateTime.Now.Date;
            var todayCount = await _db.Db.Queryable<AuditLog>().Where(log => log.CreatedAt >= today).CountAsync();

            var stats = new LogStatsDto
            {
                Total = total,
                Today = todayCount
            };

            return (stats, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}

/// <summary>
/// 日志统计 DTO
/// </summary>
public class LogStatsDto
{
    public int Total { get; set; }
    public int Today { get; set; }
}
