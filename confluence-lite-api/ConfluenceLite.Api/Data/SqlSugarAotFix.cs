using SqlSugar;

namespace ConfluenceLite.Api.Data;

/// <summary>
/// 修复 SimpleClient.GetByIdAsync 在 Native AOT 下的运行时错误
/// 原方法使用 object 参数触发动态绑定，导致 AOT 模式下失败
/// </summary>
public static class SqlSugarAotFix
{
    /// <summary>
    /// AOT 兼容的 GetByIdAsync 实现（long 主键）
    /// </summary>
    public static Task<T> GetByIdAsync<T>(this SimpleClient<T> client, long id) where T : class, new()
    {
        return client.Context.Queryable<T>().InSingleAsync(id);
    }

    /// <summary>
    /// AOT 兼容的 GetByIdAsync 实现（Guid 主键）
    /// </summary>
    public static Task<T> GetByIdAsync<T>(this SimpleClient<T> client, Guid id) where T : class, new()
    {
        return client.Context.Queryable<T>().InSingleAsync(id);
    }

    /// <summary>
    /// AOT 兼容的 GetByIdAsync 实现（int 主键）
    /// </summary>
    public static Task<T> GetByIdAsync<T>(this SimpleClient<T> client, int id) where T : class, new()
    {
        return client.Context.Queryable<T>().InSingleAsync(id);
    }
}
