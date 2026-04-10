using SqlSugar;
using ConfluenceLite.Api.Models;

namespace ConfluenceLite.Api.Data;

/// <summary>
/// 数据库上下文 - Native AOT 兼容
/// </summary>
public class AppDbContext
{
    private readonly ISqlSugarClient _db;

    public AppDbContext(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取数据库客户端
    /// </summary>
    public ISqlSugarClient Db => _db;

    /// <summary>
    /// 初始化数据库表结构
    /// </summary>
    public void InitializeDatabase()
    {
        _db.CodeFirst.InitTables(
            typeof(User),
            typeof(Workspace),
            typeof(Page),
            typeof(PageComment)
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
}
