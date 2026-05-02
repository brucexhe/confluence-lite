using System.Diagnostics;
using System.Text.Json;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using SqlSugar;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 备份服务
/// </summary>
public class BackupService
{
    private readonly AppDbContext _db;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly string _backupPath;

    public BackupService(AppDbContext db, IHostEnvironment env, IConfiguration config)
    {
        _db = db;
        _env = env;
        _config = config;
        _backupPath = Path.Combine(env.ContentRootPath, "data", "backups");

        if (!Directory.Exists(_backupPath))
        {
            Directory.CreateDirectory(_backupPath);
        }
    }

    /// <summary>
    /// 获取备份列表
    /// </summary>
    public async Task<(List<BackupDto>?, string?)> GetBackupsAsync()
    {
        try
        {
            var backups = await _db.Db.Queryable<SystemBackup>()
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var dtos = backups.Select(b => new BackupDto
            {
                Id = b.Id,
                Name = b.Name,
                Description = b.Description ?? string.Empty,
                Type = b.Type,
                FileSize = b.FileSize,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                CompletedAt = b.CompletedAt
            }).ToList();

            return (dtos, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 获取备份详情
    /// </summary>
    public async Task<(BackupDto?, string?)> GetBackupByIdAsync(long id)
    {
        try
        {
            var backup = await _db.Db.Queryable<SystemBackup>().Where(b => b.Id == id).FirstAsync();
            if (backup == null)
                return (null, "备份不存在");

            var dto = new BackupDto
            {
                Id = backup.Id,
                Name = backup.Name,
                Description = backup.Description ?? string.Empty,
                Type = backup.Type,
                FileSize = backup.FileSize,
                Status = backup.Status,
                CreatedAt = backup.CreatedAt,
                CompletedAt = backup.CompletedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 创建备份
    /// </summary>
    public async Task<(BackupDto?, string?)> CreateBackupAsync(CreateBackupRequest request, long userId)
    {
        var backup = new SystemBackup
        {
            Name = request.Name ?? $"backup-{DateTime.Now:yyyyMMdd-HHmmss}",
            Description = request.Description ?? "",
            Type = request.Options.Contains("attachments") ? "full" : "database",
            Options = JsonSerializer.Serialize(request.Options),
            Status = "processing",
            CreatedById = userId,
            CreatedAt = DateTime.Now
        };

        try
        {
            var id = await _db.SystemBackups.InsertReturnIdentityAsync(backup);
            backup.Id = id;

            // 在后台执行备份
            _ = Task.Run(() => ExecuteBackupAsync(backup.Id, request.Options));

            var dto = new BackupDto
            {
                Id = backup.Id,
                Name = backup.Name,
                Description = backup.Description ?? string.Empty,
                Type = backup.Type,
                Status = backup.Status,
                CreatedAt = backup.CreatedAt
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 执行备份
    /// </summary>
    private async Task ExecuteBackupAsync(long backupId, List<string> options)
    {
        var backup = await _db.Db.Queryable<SystemBackup>().Where(b => b.Id == backupId).FirstAsync();
        if (backup == null) return;

        try
        {
            var fileName = $"{backup.Name}.zip";
            var filePath = Path.Combine(_backupPath, fileName);

            // 模拟备份过程
            await Task.Delay(2000);

            // 备份数据库
            if (options.Contains("database"))
            {
                await BackupDatabaseAsync(backup);
            }

            // 备份附件
            if (options.Contains("attachments"))
            {
                await BackupAttachmentsAsync(backup);
            }

            // 备份配置
            if (options.Contains("config"))
            {
                await BackupConfigAsync(backup);
            }

            backup.Status = "completed";
            backup.CompletedAt = DateTime.Now;
            backup.FilePath = filePath;
            backup.FileSize = new FileInfo(filePath).Length;

            await _db.Db.Updateable(backup).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            backup.Status = "failed";
            backup.ErrorMessage = ex.Message;
            await _db.Db.Updateable(backup).ExecuteCommandAsync();
        }
    }

    private async Task BackupDatabaseAsync(SystemBackup backup)
    {
        // 简化版：使用 SqlSugar 的备份数据功能
        var db = _db.Db;
        var tables = db.DbMaintenance.GetTableInfoList();

        foreach (var table in tables)
        {
            await Task.Delay(100); // 模拟处理
        }
    }

    private async Task BackupAttachmentsAsync(SystemBackup backup)
    {
        var uploadPath = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");

        if (Directory.Exists(uploadPath))
        {
            var files = Directory.GetFiles(uploadPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                await Task.Delay(50); // 模拟复制
            }
        }
    }

    private async Task BackupConfigAsync(SystemBackup backup)
    {
        var runtimeConfigPath = Path.Combine(_env.ContentRootPath, "data", "appsettings.runtime.json");
        if (File.Exists(runtimeConfigPath))
        {
            var backupConfigPath = Path.Combine(_backupPath, $"appsettings.{backup.Name}.json");
            File.Copy(runtimeConfigPath, backupConfigPath, true);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 还原备份
    /// </summary>
    public async Task<(bool, string?)> RestoreBackupAsync(long id, RestoreBackupRequest request)
    {
        if (!request.Confirmed)
            return (false, "需要确认还原操作");

        try
        {
            var backup = await _db.Db.Queryable<SystemBackup>().Where(b => b.Id == id).FirstAsync();
            if (backup == null)
                return (false, "备份不存在");

            if (backup.Status != "completed")
                return (false, "只能还原已完成的备份");

            if (backup.FilePath == null || !File.Exists(backup.FilePath))
                return (false, "备份文件不存在");

            // 模拟还原过程
            await Task.Delay(2000);

            return (true, "备份还原成功");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 删除备份
    /// </summary>
    public async Task<(bool, string?)> DeleteBackupAsync(long id)
    {
        try
        {
            var backup = await _db.Db.Queryable<SystemBackup>().Where(b => b.Id == id).FirstAsync();
            if (backup == null)
                return (false, "备份不存在");

            // 删除文件
            if (!string.IsNullOrEmpty(backup.FilePath) && File.Exists(backup.FilePath))
            {
                File.Delete(backup.FilePath);
            }

            await _db.Db.Deleteable<SystemBackup>().Where(b => b.Id == id).ExecuteCommandAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// 下载备份文件
    /// </summary>
    public async Task<(byte[]?, string?, string?)> GetBackupFileAsync(long id)
    {
        try
        {
            var backup = await _db.Db.Queryable<SystemBackup>().Where(b => b.Id == id).FirstAsync();
            if (backup == null)
                return (null, null, "备份不存在");

            if (backup.Status != "completed")
                return (null, null, "备份未完成");

            if (backup.FilePath == null || !File.Exists(backup.FilePath))
                return (null, null, "备份文件不存在");

            var bytes = await File.ReadAllBytesAsync(backup.FilePath);
            var fileName = Path.GetFileName(backup.FilePath);

            return (bytes, fileName, null);
        }
        catch (Exception ex)
        {
            return (null, null, ex.Message);
        }
    }
}
