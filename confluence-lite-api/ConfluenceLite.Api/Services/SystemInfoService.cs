using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Models;
using SqlSugar;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 系统信息服务
/// </summary>
public class SystemInfoService
{
    private readonly AppDbContext _db;
    private readonly IHostEnvironment _env;
    private readonly DateTime _startTime;

    public SystemInfoService(AppDbContext db, IHostEnvironment env)
    {
        _db = db;
        _env = env;
        _startTime = Process.GetCurrentProcess().StartTime;
    }

    /// <summary>
    /// 获取系统信息
    /// </summary>
    public async Task<(SystemInfoDto?, string?)> GetSystemInfoAsync()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var assemblyName = assembly.GetName();
            var version = assemblyName.Version?.ToString() ?? "1.0.0";

            var process = Process.GetCurrentProcess();
            var startTime = process.StartTime;
            var uptime = (DateTime.Now - startTime).TotalSeconds;

            // CPU 信息
            var cpuInfo = new CpuInfoDto
            {
                Cores = Environment.ProcessorCount,
                Usage = CalculateCpuUsage()
            };

            // 内存信息
            var memoryInfo = new MemoryInfoDto
            {
                Used = process.WorkingSet64,
                Total = GetTotalPhysicalMemory(),
                Free = GetTotalPhysicalMemory() - process.WorkingSet64
            };

            // 数据库信息
            var dbInfo = await GetDatabaseInfoAsync();

            var dto = new SystemInfoDto
            {
                Version = version,
                BuildTime = GetBuildTime(assembly),
                Environment = _env.EnvironmentName,
                StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
                UptimeSeconds = uptime,
                Hostname = Environment.MachineName,
                Platform = RuntimeInformation.OSDescription,
                Arch = RuntimeInformation.OSArchitecture.ToString(),
                Cpu = cpuInfo,
                Memory = memoryInfo,
                Database = dbInfo
            };

            return (dto, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 获取系统统计数据
    /// </summary>
    public async Task<(SystemStatsDto?, string?)> GetStatsAsync()
    {
        try
        {
            var stats = new SystemStatsDto
            {
                UserCount = await _db.Db.Queryable<User>().CountAsync(),
                WorkspaceCount = await _db.Db.Queryable<Workspace>().CountAsync(),
                PageCount = await _db.Db.Queryable<Page>().CountAsync(),
                AttachmentCount = await _db.Db.Queryable<Attachment>().CountAsync()
            };

            return (stats, null);
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// 计算 CPU 使用率（简化版）
    /// </summary>
    private double CalculateCpuUsage()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var currentTime = process.TotalProcessorTime;
            Thread.Sleep(100);
            var newTime = process.TotalProcessorTime;

            var cpuUsage = (newTime - currentTime).TotalMilliseconds / (Environment.ProcessorCount * 100) * 100;
            return Math.Min(100, Math.Max(0, cpuUsage));
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 获取总物理内存
    /// </summary>
    private long GetTotalPhysicalMemory()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows 使用性能计数器
                return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            }
            else
            {
                // Linux/Mac 读取 /proc/meminfo
                return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            }
        }
        catch
        {
            return 8L * 1024 * 1024 * 1024; // 默认 8GB
        }
    }

    /// <summary>
    /// 获取构建时间
    /// </summary>
    [UnconditionalSuppressMessage("SingleFile", "IL3000:Avoid accessing Assembly.Location", Justification = "We handle the empty location case for Native AOT.")]
    private string GetBuildTime(System.Reflection.Assembly assembly)
    {
        try
        {
            var filePath = assembly.Location;
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".exe");
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(AppContext.BaseDirectory, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".dll");
                }
            }

            if (File.Exists(filePath))
            {
                var buildDate = System.IO.File.GetLastWriteTime(filePath);
                return buildDate.ToString("yyyy-MM-dd HH:mm:ss");
            }
            
            return "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// 获取数据库信息
    /// </summary>
    private async Task<DatabaseInfoDto> GetDatabaseInfoAsync()
    {
        try
        {
            var dbType = _db.Db.CurrentConnectionConfig.DbType;
            var dbInfo = new DatabaseInfoDto
            {
                Type = dbType.ToString(),
                Connected = true
            };

            try
            {
                // 获取数据库版本
                switch (dbType)
                {
                    case DbType.PostgreSQL:
                        var versionResult = await _db.Db.Ado.GetScalarAsync("SELECT version()");
                        dbInfo.Version = versionResult?.ToString()?.Split(',')[0] ?? "Unknown";
                        break;
                    case DbType.SqlServer:
                        var sqlVersion = await _db.Db.Ado.GetScalarAsync("SELECT @@VERSION");
                        dbInfo.Version = sqlVersion?.ToString()?.Split('\n')[0] ?? "Unknown";
                        break;
                    case DbType.MySql:
                        var mysqlVersion = await _db.Db.Ado.GetScalarAsync("SELECT VERSION()");
                        dbInfo.Version = mysqlVersion?.ToString() ?? "Unknown";
                        break;
                    default:
                        dbInfo.Version = "Unknown";
                        break;
                }

                // 获取数据库名称
                var dbNameResult = await _db.Db.Ado.GetScalarAsync("SELECT CURRENT_DATABASE()");
                dbInfo.Name = dbNameResult?.ToString() ?? "Unknown";
            }
            catch
            {
                dbInfo.Connected = false;
            }

            return dbInfo;
        }
        catch
        {
            return new DatabaseInfoDto
            {
                Type = "Unknown",
                Connected = false
            };
        }
    }
}
