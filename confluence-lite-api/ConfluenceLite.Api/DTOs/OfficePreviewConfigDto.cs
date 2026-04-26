namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// Office 预览配置
/// </summary>
public class OfficePreviewConfigDto
{
    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gotenberg 服务地址
    /// </summary>
    public string BaseUrl { get; set; } = "http://gotenberg:3000";

    /// <summary>
    /// 请求超时时间(秒)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}
