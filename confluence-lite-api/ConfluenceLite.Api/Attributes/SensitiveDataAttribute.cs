namespace ConfluenceLite.Api.Attributes;

/// <summary>
/// 标记敏感数据字段，审计日志中自动脱敏
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class SensitiveDataAttribute : Attribute
{
    /// <summary>
    /// 脱敏后的显示文本
    /// </summary>
    public string Mask { get; set; } = "***";
}
