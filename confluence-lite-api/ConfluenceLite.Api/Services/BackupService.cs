using System.Data;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using Npgsql;
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
    private readonly IServiceProvider _serviceProvider;
    private readonly string _backupPath;

    public BackupService(AppDbContext db, IHostEnvironment env, IConfiguration config, IServiceProvider serviceProvider)
    {
        _db = db;
        _env = env;
        _config = config;
        _serviceProvider = serviceProvider;
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

            var backupId = id;
            var options = request.Options;

            // 在后台执行备份，使用独立的 DI scope
            _ = Task.Run(async () =>
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scopedConfig = scope.ServiceProvider.GetRequiredService<IConfiguration>();
                await ExecuteBackupAsync(backupId, options, scopedDb, scopedConfig);
            });

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
    /// 执行备份 — 创建真实 ZIP 文件
    /// </summary>
    private async Task ExecuteBackupAsync(long backupId, List<string> options, AppDbContext db, IConfiguration config)
    {
        var backup = await db.Db.Queryable<SystemBackup>().Where(b => b.Id == backupId).FirstAsync();
        if (backup == null) return;

        try
        {
            var fileName = $"{backup.Name}.zip";
            var filePath = Path.Combine(_backupPath, fileName);

            if (!Directory.Exists(_backupPath))
                Directory.CreateDirectory(_backupPath);

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create, leaveOpen: false);

            // 写入 manifest
            var manifest = new BackupManifest
            {
                BackupName = backup.Name,
                BackupDate = DateTime.Now,
                Options = options,
                Version = 1
            };
            var manifestJson = JsonSerializer.Serialize(manifest);
            var manifestEntry = archive.CreateEntry("backup-manifest.json");
            await using (var manifestStream = manifestEntry.Open())
            {
                await using var writer = new StreamWriter(manifestStream, Encoding.UTF8);
                await writer.WriteAsync(manifestJson);
            }

            // 备份数据库
            if (options.Contains("database"))
            {
                await BackupDatabaseToArchiveAsync(archive, db);
            }

            // 备份附件
            if (options.Contains("attachments"))
            {
                BackupAttachmentsToArchive(archive);
            }

            // 备份配置
            if (options.Contains("config"))
            {
                BackupConfigToArchive(archive);
            }

            archive.Dispose();
            fileStream.Dispose();

            backup.Status = "completed";
            backup.CompletedAt = DateTime.Now;
            backup.FilePath = filePath;
            backup.FileSize = new FileInfo(filePath).Length;

            await db.Db.Updateable(backup).ExecuteCommandAsync();

            // 应用保留策略
            var retentionDays = config.GetValue<int>("App:Backup:RetentionDays", 30);
            await ApplyRetentionPolicyAsync(retentionDays, db);
        }
        catch (Exception ex)
        {
            backup.Status = "failed";
            backup.ErrorMessage = ex.Message;
            await db.Db.Updateable(backup).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// 备份数据库到 ZIP — 导出每张表为 JSON
    /// </summary>
    private async Task BackupDatabaseToArchiveAsync(ZipArchive archive, AppDbContext db)
    {
        var tables = db.Db.DbMaintenance.GetTableInfoList();
        var tableNames = new List<string>();

        foreach (var table in tables)
        {
            try
            {
                var tableName = table.Name;
                tableNames.Add(tableName);

                var dataTable = db.Db.Ado.GetDataTable($"SELECT * FROM \"{tableName}\"");
                var rows = new List<Dictionary<string, object?>>();
                foreach (DataRow row in dataTable.Rows)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        dict[col.ColumnName] = row.IsNull(col) ? null : row[col];
                    }
                    rows.Add(dict);
                }

                var json = JsonSerializer.Serialize(rows);
                var entry = archive.CreateEntry($"database/{tableName}.json");
                await using var entryStream = entry.Open();
                await using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Backup] Error backing up table {table.Name}: {ex.Message}");
            }
        }

        // 写入表顺序列表
        var tableListJson = JsonSerializer.Serialize(tableNames);
        var tableListEntry = archive.CreateEntry("database/_table_order.json");
        await using (var tlStream = tableListEntry.Open())
        {
            await using var tlWriter = new StreamWriter(tlStream, Encoding.UTF8);
            await tlWriter.WriteAsync(tableListJson);
        }
    }

    /// <summary>
    /// 备份附件到 ZIP
    /// </summary>
    private void BackupAttachmentsToArchive(ZipArchive archive)
    {
        var uploadPath = Path.Combine(_env.ContentRootPath, "wwwroot",
            _config.GetValue<string>("App:Attachment:UploadPath") ?? "uploads");

        if (!Directory.Exists(uploadPath)) return;

        foreach (var filePath in Directory.GetFiles(uploadPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(uploadPath, filePath);
            var entry = archive.CreateEntry($"attachments/{relativePath.Replace('\\', '/')}");
            using var entryStream = entry.Open();
            using var fileStream = File.OpenRead(filePath);
            fileStream.CopyTo(entryStream);
        }
    }

    /// <summary>
    /// 备份配置到 ZIP
    /// </summary>
    private void BackupConfigToArchive(ZipArchive archive)
    {
        var runtimeConfigPath = Path.Combine(_env.ContentRootPath, "data", "appsettings.runtime.json");
        if (!File.Exists(runtimeConfigPath)) return;

        var entry = archive.CreateEntry("config/appsettings.runtime.json");
        using var entryStream = entry.Open();
        using var fileStream = File.OpenRead(runtimeConfigPath);
        fileStream.CopyTo(entryStream);
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

            using var archive = ZipFile.OpenRead(backup.FilePath);

            // 读取 manifest
            var manifestEntry = archive.GetEntry("backup-manifest.json");
            if (manifestEntry == null)
                return (false, "无效的备份文件：缺少 manifest");

            BackupManifest? manifest;
            await using (var manifestStream = manifestEntry.Open())
            {
                using var reader = new StreamReader(manifestStream, Encoding.UTF8);
                var json = await reader.ReadToEndAsync();
                manifest = JsonSerializer.Deserialize<BackupManifest>(json);
            }

            var options = manifest?.Options ?? new List<string> { "database", "attachments", "config" };

            // 还原数据库
            if (options.Contains("database"))
            {
                await RestoreDatabaseFromArchiveAsync(archive);
            }

            // 还原附件
            if (options.Contains("attachments"))
            {
                RestoreAttachmentsFromArchive(archive);
            }

            // 还原配置
            if (options.Contains("config"))
            {
                RestoreConfigFromArchive(archive);
            }

            return (true, "备份还原成功");
        }
        catch (Exception ex)
        {
            return (false, $"还原失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从 ZIP 还原数据库
    /// </summary>
    private async Task RestoreDatabaseFromArchiveAsync(ZipArchive archive)
    {
        var tableOrderEntry = archive.GetEntry("database/_table_order.json");
        if (tableOrderEntry == null) return;

        List<string> tableOrder;
        await using (var stream = tableOrderEntry.Open())
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            tableOrder = JsonSerializer.Deserialize<List<string>>(json) ?? new();
        }

        var db = _db.Db;

        // 反向清空表（处理外键约束）
        var reversed = tableOrder.AsEnumerable().Reverse().ToList();
        foreach (var tableName in reversed)
        {
            try
            {
                db.Ado.ExecuteCommand($"TRUNCATE TABLE \"{tableName}\" CASCADE");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Restore] Truncate {tableName} warning: {ex.Message}");
            }
        }

        // 按序恢复数据
        foreach (var tableName in tableOrder)
        {
            var entry = archive.GetEntry($"database/{tableName}.json");
            if (entry == null) continue;

            try
            {
                await using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream, Encoding.UTF8);
                var json = await reader.ReadToEndAsync();
                var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                if (rows == null || rows.Count == 0) continue;

                foreach (var row in rows)
                {
                    var columns = string.Join(", ", row.Keys.Select(k => $"\"{k}\""));
                    var paramNames = string.Join(", ", row.Keys.Select((_, i) => $"${i + 1}"));
                    var sql = $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({paramNames})";

                    var parameters = row.Values.Select((v, i) =>
                    {
                        object value = v.ValueKind switch
                        {
                            JsonValueKind.Null => DBNull.Value,
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Number => v.GetDecimal(),
                            JsonValueKind.String => v.GetString()!,
                            _ => v.GetRawText()
                        };
                        return new NpgsqlParameter($"@p{i}", value);
                    }).ToArray();

                    db.Ado.ExecuteCommand(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Restore] Error restoring table {tableName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 从 ZIP 还原附件
    /// </summary>
    private void RestoreAttachmentsFromArchive(ZipArchive archive)
    {
        var uploadPath = Path.Combine(_env.ContentRootPath, "wwwroot",
            _config.GetValue<string>("App:Attachment:UploadPath") ?? "uploads");

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("attachments/")) continue;

            var relativePath = entry.FullName["attachments/".Length..];
            if (string.IsNullOrEmpty(relativePath)) continue;

            var targetPath = Path.Combine(uploadPath, relativePath);
            var targetDir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            using var entryStream = entry.Open();
            using var fileStream = File.Create(targetPath);
            entryStream.CopyTo(fileStream);
        }
    }

    /// <summary>
    /// 从 ZIP 还原配置
    /// </summary>
    private void RestoreConfigFromArchive(ZipArchive archive)
    {
        var entry = archive.GetEntry("config/appsettings.runtime.json");
        if (entry == null) return;

        var targetPath = Path.Combine(_env.ContentRootPath, "data", "appsettings.runtime.json");

        using var entryStream = entry.Open();
        using var fileStream = File.Create(targetPath);
        entryStream.CopyTo(fileStream);
    }

    /// <summary>
    /// 应用备份保留策略
    /// </summary>
    private async Task ApplyRetentionPolicyAsync(int retentionDays, AppDbContext db)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-retentionDays);
            var oldBackups = await db.Db.Queryable<SystemBackup>()
                .Where(b => b.Status == "completed" && b.CompletedAt < cutoffDate)
                .ToListAsync();

            foreach (var backup in oldBackups)
            {
                if (!string.IsNullOrEmpty(backup.FilePath) && File.Exists(backup.FilePath))
                {
                    File.Delete(backup.FilePath);
                }
                await db.Db.Deleteable<SystemBackup>()
                    .Where(b => b.Id == backup.Id)
                    .ExecuteCommandAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Backup] Retention policy error: {ex.Message}");
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

    private class BackupManifest
    {
        public string? BackupName { get; set; }
        public DateTime BackupDate { get; set; }
        public List<string>? Options { get; set; }
        public int Version { get; set; }
    }
}
