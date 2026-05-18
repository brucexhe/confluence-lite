using SqlSugar;
using ConfluenceLite.Api.Models;

namespace ConfluenceLite.Api.Data;

/// <summary>
/// 数据库上下文 - Native AOT 兼容
/// </summary>
public class AppDbContext
{
    private ISqlSugarClient _db;
    private readonly ConnectionConfig _config;

    public AppDbContext(ISqlSugarClient db, ConnectionConfig config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// 获取数据库客户端
    /// </summary>
    public ISqlSugarClient Db => _db;

    /// <summary>
    /// 更新连接字符串并重建客户端（安装向导完成后调用）
    /// </summary>
    public void UpdateConnection(string connectionString)
    {
        _config.ConnectionString = connectionString;
        _db = new SqlSugarClient(_config);
    }

    /// <summary>
    /// 创建新的数据库客户端 (安装向导使用，连接到用户配置的数据库)
    /// </summary>
    public static ISqlSugarClient CreateClient(string connectionString, bool enableSqlLog = true)
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = SqlSugar.DbType.PostgreSQL,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            AopEvents = new AopEvents
            {
                OnLogExecuting = (sql, p) =>
                {
                    if (enableSqlLog)
                    {
                        Console.WriteLine($"[SQL] {sql}");
                    }
                }
            }
        });
    }

    /// <summary>
    /// 初始化数据库表结构
    /// </summary>
    public void InitializeDatabase()
    {
        _db.CodeFirst.InitTables(
            typeof(User),
            typeof(Workspace),
            typeof(Page),
            typeof(PageComment),
            typeof(UserGroup),
            typeof(UserGroupMember),
            typeof(WorkspacePermission),
            typeof(WorkspaceCategory),
            typeof(PageVersion),
            typeof(PageRestriction),
            typeof(PageLabel),
            typeof(PageTemplate),
            typeof(Attachment),
            typeof(Draft),
            typeof(ContentProperty),
            typeof(Notification),
            typeof(Watcher),
            typeof(Mention),
            typeof(Share),
            typeof(SearchHistory),
            typeof(ActivityEvent),
            typeof(AuditLog),
            typeof(UserFavorite),
            typeof(ScheduledJob),
            typeof(JobExecution),
            typeof(SystemBackup),
            typeof(ImportTask)
        );
    }

    /// <summary>
    /// 获取用户表的SimpleClient
    /// </summary>
    public SimpleClient<User> Users => new(_db);

    /// <summary>
    /// 获取工作空间表的SimpleClient
    /// </summary>
    public SimpleClient<Workspace> Workspaces => new(_db);

    /// <summary>
    /// 获取页面表的SimpleClient
    /// </summary>
    public SimpleClient<Page> Pages => new(_db);

    /// <summary>
    /// 获取页面评论表的SimpleClient
    /// </summary>
    public SimpleClient<PageComment> PageComments => new(_db);

    /// <summary>
    /// 获取用户组表的SimpleClient
    /// </summary>
    public SimpleClient<UserGroup> UserGroups => new(_db);

    /// <summary>
    /// 获取用户组成员表的SimpleClient
    /// </summary>
    public SimpleClient<UserGroupMember> UserGroupMembers => new(_db);

    /// <summary>
    /// 获取工作空间权限表的SimpleClient
    /// </summary>
    public SimpleClient<WorkspacePermission> WorkspacePermissions => new(_db);

    /// <summary>
    /// 获取工作空间分类表的SimpleClient
    /// </summary>
    public SimpleClient<WorkspaceCategory> WorkspaceCategories => new(_db);

    /// <summary>
    /// 获取页面版本历史表的SimpleClient
    /// </summary>
    public SimpleClient<PageVersion> PageVersions => new(_db);

    /// <summary>
    /// 获取页面限制表的SimpleClient
    /// </summary>
    public SimpleClient<PageRestriction> PageRestrictions => new(_db);

    /// <summary>
    /// 获取页面标签表的SimpleClient
    /// </summary>
    public SimpleClient<PageLabel> PageLabels => new(_db);

    /// <summary>
    /// 获取页面模板表的SimpleClient
    /// </summary>
    public SimpleClient<PageTemplate> PageTemplates => new(_db);

    /// <summary>
    /// 获取附件表的SimpleClient
    /// </summary>
    public SimpleClient<Attachment> Attachments => new(_db);

    /// <summary>
    /// 获取草稿表的SimpleClient
    /// </summary>
    public SimpleClient<Draft> Drafts => new(_db);

    /// <summary>
    /// 获取内容属性表的SimpleClient
    /// </summary>
    public SimpleClient<ContentProperty> ContentProperties => new(_db);

    /// <summary>
    /// 获取通知表的SimpleClient
    /// </summary>
    public SimpleClient<Notification> Notifications => new(_db);

    /// <summary>
    /// 获取关注表的SimpleClient
    /// </summary>
    public SimpleClient<Watcher> Watchers => new(_db);

    /// <summary>
    /// 获取提及表的SimpleClient
    /// </summary>
    public SimpleClient<Mention> Mentions => new(_db);

    /// <summary>
    /// 获取分享表的SimpleClient
    /// </summary>
    public SimpleClient<Share> Shares => new(_db);

    /// <summary>
    /// 获取搜索历史表的SimpleClient
    /// </summary>
    public SimpleClient<SearchHistory> SearchHistories => new(_db);

    /// <summary>
    /// 获取活动事件表的SimpleClient
    /// </summary>
    public SimpleClient<ActivityEvent> ActivityEvents => new(_db);

    /// <summary>
    /// 获取审计日志表的SimpleClient
    /// </summary>
    public SimpleClient<AuditLog> AuditLogs => new(_db);

    /// <summary>
    /// 获取用户收藏表的SimpleClient
    /// </summary>
    public SimpleClient<UserFavorite> UserFavorites => new(_db);

    /// <summary>
    /// 获取定时任务表的SimpleClient
    /// </summary>
    public SimpleClient<ScheduledJob> ScheduledJobs => new(_db);

    /// <summary>
    /// 获取任务执行历史表的SimpleClient
    /// </summary>
    public SimpleClient<JobExecution> JobExecutions => new(_db);

    /// <summary>
    /// 获取系统备份表的SimpleClient
    /// </summary>
    public SimpleClient<SystemBackup> SystemBackups => new(_db);

    /// <summary>
    /// 获取导入任务表的SimpleClient
    /// </summary>
    public SimpleClient<ImportTask> ImportTasks => new(_db);
}
