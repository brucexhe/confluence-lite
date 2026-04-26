namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 站点配置
/// </summary>
public class SiteConfigDto
{
    public string SiteName { get; set; } = "Confluence Lite";
    public string SiteDescription { get; set; } = "";
    public string SiteLogo { get; set; } = "";
    public string SiteDomain { get; set; } = "";
    public string DefaultLanguage { get; set; } = "zh-CN";
    public string DefaultHomePage { get; set; } = "";
    public string Timezone { get; set; } = "Asia/Shanghai";
    public bool AllowRegistration { get; set; } = true;
}
