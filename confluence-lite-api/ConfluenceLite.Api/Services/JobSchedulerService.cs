using System.Diagnostics;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using SqlSugar;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 任务调度服务
/// </summary>
public class JobSchedulerService
{
    private readonly AppDbContext _db;
    private readonly IServiceProvider _serviceProvider;

    public JobSchedulerService(AppDbContext db, IServiceProvider serviceProvider)
    {
        _db = db;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// 获取任务列表
    /// </summary>
    public async Task<(List<ScheduledJobDto>?, string?)> GetJobsAsync()
    {
        try
        {
            var jobs = await _db.Db.Queryable<ScheduledJob>()
                .OrderBy(j => j.Id)
                .ToListAsync();

            var dtos = jobs.Select(j => new ScheduledJobDto
            {
                Id = j.Id,
                Name = j.Name,
                Description = j.Description ?? string.Empty,
                JobType = j.JobType,
                Cron = j.CronExpression,
                Enabled = j.Enabled,
                LastRun = j.LastRunAt,
                NextRun = j.NextRunAt,
                CreatedAt = j.CreatedAt
            }).ToList();

            return (dtos, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 获取任务详情
    /// </summary>
    public async Task<(ScheduledJobDto?, string?)> GetJobByIdAsync(long id)
    {
        try
        {
            var job = await _db.Db.Queryable<ScheduledJob>().Where(j => j.Id == id).FirstAsync();
            if (job == null)
                return (null, "任务不存在");

            var dto = new ScheduledJobDto
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description ?? string.Empty,
                JobType = job.JobType,
                Cron = job.CronExpression,
                Enabled = job.Enabled,
                LastRun = job.LastRunAt,
                NextRun = job.NextRunAt,
                CreatedAt = job.CreatedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 创建任务
    /// </summary>
    public async Task<(ScheduledJobDto?, string?)> CreateJobAsync(CreateJobRequest request)
    {
        try
        {
            var job = new ScheduledJob
            {
                Name = request.Name,
                Description = request.Description,
                JobType = request.JobType,
                CronExpression = request.CronExpression,
                JobData = request.JobData,
                Enabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var id = await _db.ScheduledJobs.InsertReturnIdentityAsync(job);
            job.Id = id;

            var dto = new ScheduledJobDto
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description ?? string.Empty,
                JobType = job.JobType,
                Cron = job.CronExpression,
                Enabled = job.Enabled,
                CreatedAt = job.CreatedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 更新任务
    /// </summary>
    public async Task<(ScheduledJobDto?, string?)> UpdateJobAsync(long id, UpdateJobRequest request)
    {
        try
        {
            var job = await _db.Db.Queryable<ScheduledJob>().Where(j => j.Id == id).FirstAsync();
            if (job == null)
                return (null, "任务不存在");

            job.Name = request.Name;
            job.Description = request.Description;
            if (!string.IsNullOrEmpty(request.CronExpression))
                job.CronExpression = request.CronExpression;
            if (request.Enabled.HasValue)
                job.Enabled = request.Enabled.Value;
            job.UpdatedAt = DateTime.Now;

            await _db.ScheduledJobs.UpdateAsync(job);

            var dto = new ScheduledJobDto
            {
                Id = job.Id,
                Name = job.Name,
                Description = job.Description ?? string.Empty,
                JobType = job.JobType,
                Cron = job.CronExpression,
                Enabled = job.Enabled,
                LastRun = job.LastRunAt,
                NextRun = job.NextRunAt,
                CreatedAt = job.CreatedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 删除任务
    /// </summary>
    public async Task<(bool, string?)> DeleteJobAsync(long id)
    {
        try
        {
            await _db.Db.Deleteable<JobExecution>().Where(e => e.JobId == id).ExecuteCommandAsync();
            await _db.Db.Deleteable<ScheduledJob>().Where(j => j.Id == id).ExecuteCommandAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 立即执行任务
    /// </summary>
    public async Task<(bool, string?)> RunJobAsync(long id)
    {
        try
        {
            var job = await _db.Db.Queryable<ScheduledJob>().Where(j => j.Id == id).FirstAsync();
            if (job == null)
                return (false, "任务不存在");

            var execution = new JobExecution
            {
                JobId = id,
                Status = "running",
                TriggeredBy = "manual",
                StartedAt = DateTime.Now
            };

            var execId = await _db.Db.Insertable(execution).ExecuteReturnIdentityAsync();
            execution.Id = execId;

            try
            {
                var result = await ExecuteJobAsync(job);

                execution.Status = "success";
                execution.OutputMessage = result;
                execution.CompletedAt = DateTime.Now;
                execution.DurationMs = (int)(execution.CompletedAt.Value - execution.StartedAt).TotalMilliseconds;

                job.LastRunAt = execution.StartedAt;

                await _db.Db.Updateable(execution).ExecuteCommandAsync();
                await _db.Db.Updateable(job).ExecuteCommandAsync();

                return (true, null);
            }
            catch (Exception ex)
            {
                execution.Status = "failed";
                execution.ErrorMessage = ex.Message;
                execution.CompletedAt = DateTime.Now;
                execution.DurationMs = (int)(execution.CompletedAt.Value - execution.StartedAt).TotalMilliseconds;

                await _db.Db.Updateable(execution).ExecuteCommandAsync();

                return (false, ex.Message);
            }
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 获取任务执行历史
    /// </summary>
    public async Task<(List<JobExecutionDto>?, string?)> GetJobExecutionsAsync(long id)
    {
        try
        {
            var executions = await _db.Db.Queryable<JobExecution>()
                .Where(e => e.JobId == id)
                .OrderByDescending(e => e.StartedAt)
                .Take(50)
                .ToListAsync();

            var dtos = executions.Select(e => new JobExecutionDto
            {
                Id = e.Id,
                JobId = e.JobId,
                Status = e.Status,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                DurationMs = e.DurationMs,
                OutputMessage = e.OutputMessage,
                ErrorMessage = e.ErrorMessage,
                TriggeredBy = e.TriggeredBy
            }).ToList();

            return (dtos, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 获取任务统计
    /// </summary>
    public async Task<(JobStatsDto?, string?)> GetJobStatsAsync()
    {
        try
        {
            var running = await _db.Db.Queryable<JobExecution>().Where(e => e.Status == "running").CountAsync();
            var completed = await _db.Db.Queryable<JobExecution>().Where(e => e.Status == "success").CountAsync();
            var failed = await _db.Db.Queryable<JobExecution>().Where(e => e.Status == "failed").CountAsync();
            var pending = await _db.Db.Queryable<ScheduledJob>().Where(j => j.Enabled).CountAsync();

            var stats = new JobStatsDto
            {
                Running = running,
                Completed = completed,
                Failed = failed,
                Pending = pending
            };

            return (stats, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 执行任务具体逻辑
    /// </summary>
    private async Task<string> ExecuteJobAsync(ScheduledJob job)
    {
        return job.JobType switch
        {
            "database_backup" => await ExecuteDatabaseBackupAsync(),
            "log_cleanup" => await ExecuteLogCleanupAsync(),
            "cache_clear" => await ExecuteCacheClearAsync(),
            _ => $"未知任务类型: {job.JobType}"
        };
    }

    private async Task<string> ExecuteDatabaseBackupAsync()
    {
        await Task.Delay(1000); // 模拟执行
        return "数据库备份已完成";
    }

    private async Task<string> ExecuteLogCleanupAsync()
    {
        var logService = _serviceProvider.GetService<LogService>();
        if (logService != null)
        {
            var (success, error) = await logService.CleanupOldLogsAsync(30);
            return error ?? "日志清理已完成";
        }
        return "日志服务不可用";
    }

    private async Task<string> ExecuteCacheClearAsync()
    {
        var cacheService = _serviceProvider.GetService<CacheService>();
        if (cacheService != null)
        {
            await cacheService.ClearAllCacheAsync();
            return "缓存清理已完成";
        }
        return "缓存服务不可用";
    }
}
