namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 批量排序页面项
/// </summary>
public class BatchSortPageItem
{
    /// <summary>
    /// 页面ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 父页面ID（null表示顶级页面）
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 排序序号
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// 批量排序页面请求
/// </summary>
public class BatchSortPageRequest
{
    /// <summary>
    /// 工作空间ID
    /// </summary>
    public long WorkspaceId { get; set; }

    /// <summary>
    /// 排序项列表
    /// </summary>
    public List<BatchSortPageItem> Items { get; set; } = new();
}
