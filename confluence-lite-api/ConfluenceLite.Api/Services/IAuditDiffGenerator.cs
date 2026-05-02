namespace ConfluenceLite.Api.Services;

/// <summary>
/// 审计日志 Diff 生成器接口
/// </summary>
public interface IAuditDiffGenerator
{
    /// <summary>
    /// 生成两个对象之间的差异（JSON格式）
    /// </summary>
    string GenerateDiff(object? oldValue, object? newValue, string? requestPath = null, string? requestMethod = null);

    /// <summary>
    /// 生成两个对象之间的差异（泛型版本）
    /// </summary>
    string GenerateDiff<T>(T? oldValue, T? newValue, string? requestPath = null, string? requestMethod = null);
}
