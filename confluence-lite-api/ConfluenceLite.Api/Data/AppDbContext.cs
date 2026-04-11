using SqlSugar;
using ConfluenceLite.Api.Models;

namespace ConfluenceLite.Api.Data;

/// <summary>
/// 数据库上下文 - Native AOT 兼容
/// </summary>
public class AppDbContext
{
    private ISqlSugarClient _db;

    public AppDbContext(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取数据库客户端
    /// </summary>
    public ISqlSugarClient Db => _db;

    /// <summary>
    /// 重新配置数据库连接 (安装向导使用，无需重启)
    /// </summary>
    public void Reconfigure(string connectionString, bool enableSqlLog = true)
    {
        _db = new SqlSugarClient(new ConnectionConfig
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
            typeof(UserFavorite)
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
}
