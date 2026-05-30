namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 移动页面请求
/// </summary>
public class MovePageRequest
{
    /// <summary>
    /// 目标父页面ID（null表示移动到顶级）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 目标排序位置（在同级兄弟中的索引）
    /// </summary>
    public int SortOrder { get; set; }
}
