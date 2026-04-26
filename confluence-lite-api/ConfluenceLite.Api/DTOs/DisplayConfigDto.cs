namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 显示配置
/// </summary>
public class DisplayConfigDto
{
    public string DefaultTheme { get; set; } = "light";
    public string PrimaryColor { get; set; } = "#0052cc";
    public bool CompactMode { get; set; }
    public int PageSize { get; set; } = 20;
    public string PageTreeExpandMode { get; set; } = "first";
    public bool ShowPageViews { get; set; } = true;
    public bool ShowAuthorInfo { get; set; } = true;
    public bool ShowLastModified { get; set; } = true;
    public string DefaultEditorMode { get; set; } = "visual";
    public int AutoSaveInterval { get; set; } = 60;
    public bool EnableSpellCheck { get; set; } = true;
    public int DefaultSidebarWidth { get; set; } = 260;
    public bool ShowSpaceIcon { get; set; } = true;
    public bool AllowCollapseSidebar { get; set; } = true;
}
