namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 公开站点信息（供前端 Layout 使用，无需认证）
/// </summary>
public class SiteInfoDto
{
    public bool Installed { get; set; }
    public string SiteName { get; set; } = "Confluence Lite";
    public string SiteLogo { get; set; } = "";
    public bool AllowRegistration { get; set; } = true;
}
