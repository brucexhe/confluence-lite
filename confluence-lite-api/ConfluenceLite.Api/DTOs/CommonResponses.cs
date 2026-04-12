namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 通用API响应
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// 分页请求
/// </summary>
public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>
    /// 页码 (从1开始)
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 20 : (value > 100 ? 100 : value);
    }

    /// <summary>
    /// 计算跳过的记录数
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}

/// <summary>
/// 分页响应
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 总记录数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNext => Page < TotalPages;
}
