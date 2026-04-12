using Microsoft.AspNetCore.Mvc;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Controllers;

/// <summary>
/// 页面管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PageController : ControllerBase
{
    private readonly PageService _pageService;
    private readonly CommentService _commentService;

    public PageController(PageService pageService, CommentService commentService)
    {
        _pageService = pageService;
        _commentService = commentService;
    }

    /// <summary>
    /// 创建页面
    /// </summary>
    /// <param name="request">创建页面请求</param>
    /// <returns>创建的页面信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreatePage([FromBody] CreatePageRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<PageDto>.Fail("未授权，请先登录"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<PageDto>.Fail("请求参数无效"));
        }

        var (page, error) = await _pageService.CreatePageAsync(currentUser.UserId, request);

        if (page == null || error != null)
        {
            return BadRequest(ApiResponse<PageDto>.Fail(error ?? "创建页面失败"));
        }

        return CreatedAtAction(nameof(GetPage), new { id = page.Id }, ApiResponse<PageDto>.Ok(page, "页面创建成功"));
    }

    /// <summary>
    /// 获取页面信息
    /// </summary>
    /// <param name="id">页面ID</param>
    /// <returns>页面信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPage(long id)
    {
        var page = await _pageService.GetPageByIdAsync(id);
        if (page == null)
        {
            return NotFound(ApiResponse<PageDto>.Fail("页面不存在"));
        }

        return Ok(ApiResponse<PageDto>.Ok(page));
    }

    /// <summary>
    /// 获取工作空间的页面列表
    /// </summary>
    /// <param name="workspaceId">工作空间ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>页面列表</returns>
    [HttpGet("workspace/{workspaceId}")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPagesByWorkspace(long workspaceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var request = new PagedRequest { Page = page, PageSize = pageSize };
        var result = await _pageService.GetPagesByWorkspaceAsync(workspaceId, request);

        return Ok(ApiResponse<PagedResponse<PageDto>>.Ok(result));
    }

    /// <summary>
    /// 获取工作空间的页面树
    /// </summary>
    /// <param name="workspaceId">工作空间ID</param>
    /// <returns>页面树</returns>
    [HttpGet("workspace/{workspaceId}/tree")]
    [ProducesResponseType(typeof(ApiResponse<List<PageTreeNodeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPageTree(long workspaceId)
    {
        var tree = await _pageService.GetPageTreeAsync(workspaceId);

        return Ok(ApiResponse<List<PageTreeNodeDto>>.Ok(tree));
    }

    /// <summary>
    /// 获取页面的子页面列表
    /// </summary>
    /// <param name="parentId">父页面ID</param>
    /// <returns>子页面列表</returns>
    [HttpGet("{parentId}/children")]
    [ProducesResponseType(typeof(ApiResponse<List<PageDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChildPages(long parentId)
    {
        var children = await _pageService.GetChildPagesAsync(parentId);

        return Ok(ApiResponse<List<PageDto>>.Ok(children));
    }

    /// <summary>
    /// 更新页面
    /// </summary>
    /// <param name="id">页面ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的页面信息</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<PageDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePage(long id, [FromBody] UpdatePageRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<PageDto>.Fail("未授权，请先登录"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<PageDto>.Fail("请求参数无效"));
        }

        var (page, error) = await _pageService.UpdatePageAsync(id, currentUser.UserId, request);

        if (page == null || error != null)
        {
            return BadRequest(ApiResponse<PageDto>.Fail(error ?? "更新页面失败"));
        }

        return Ok(ApiResponse<PageDto>.Ok(page, "更新成功"));
    }

    /// <summary>
    /// 删除页面
    /// </summary>
    /// <param name="id">页面ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeletePage(long id)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<bool>.Fail("未授权，请先登录"));
        }

        var (success, error) = await _pageService.DeletePageAsync(id, currentUser.UserId);

        if (!success || error != null)
        {
            return NotFound(ApiResponse<bool>.Fail(error ?? "删除页面失败"));
        }

        return Ok(ApiResponse<bool>.Ok(true, "页面已删除"));
    }

    // ========== 版本历史接口 ==========

    /// <summary>
    /// 获取页面的版本列表
    /// </summary>
    [HttpGet("{pageId}/versions")]
    [ProducesResponseType(typeof(ApiResponse<List<PageVersionListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPageVersions(long pageId)
    {
        var versions = await _pageService.GetPageVersionsAsync(pageId);
        return Ok(ApiResponse<List<PageVersionListDto>>.Ok(versions));
    }

    /// <summary>
    /// 获取单个版本详情
    /// </summary>
    [HttpGet("versions/{versionId}")]
    [ProducesResponseType(typeof(ApiResponse<PageVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PageVersionDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageVersion(long versionId)
    {
        var version = await _pageService.GetPageVersionAsync(versionId);
        if (version == null)
        {
            return NotFound(ApiResponse<PageVersionDto>.Fail("版本不存在"));
        }
        return Ok(ApiResponse<PageVersionDto>.Ok(version));
    }

    /// <summary>
    /// 删除页面版本
    /// </summary>
    [HttpDelete("versions/{versionId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeletePageVersion(long versionId)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<bool>.Fail("未授权，请先登录"));
        }

        var (success, error) = await _pageService.DeletePageVersionAsync(versionId);
        if (!success || error != null)
        {
            return NotFound(ApiResponse<bool>.Fail(error ?? "删除版本失败"));
        }
        return Ok(ApiResponse<bool>.Ok(true, "版本已删除"));
    }

    // ========== 评论相关接口 ==========

    /// <summary>
    /// 获取页面的评论列表
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <returns>评论列表</returns>
    [HttpGet("{pageId}/comments")]
    [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetComments(long pageId)
    {
        var comments = await _commentService.GetCommentsByPageAsync(pageId);

        return Ok(ApiResponse<List<CommentDto>>.Ok(comments));
    }

    /// <summary>
    /// 创建评论
    /// </summary>
    /// <param name="pageId">页面ID</param>
    /// <param name="request">创建评论请求</param>
    /// <returns>创建的评论信息</returns>
    [HttpPost("{pageId}/comments")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateComment(long pageId, [FromBody] CreateCommentRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<CommentDto>.Fail("未授权，请先登录"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CommentDto>.Fail("请求参数无效"));
        }

        // 确保pageId匹配
        request.PageId = pageId;

        var (comment, error) = await _commentService.CreateCommentAsync(currentUser.UserId, request);

        if (comment == null || error != null)
        {
            return BadRequest(ApiResponse<CommentDto>.Fail(error ?? "创建评论失败"));
        }

        return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, ApiResponse<CommentDto>.Ok(comment, "评论创建成功"));
    }

    /// <summary>
    /// 获取评论信息
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <returns>评论信息</returns>
    [HttpGet("comments/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetComment(long id)
    {
        var comment = await _commentService.GetCommentByIdAsync(id);
        if (comment == null)
        {
            return NotFound(ApiResponse<CommentDto>.Fail("评论不存在"));
        }

        return Ok(ApiResponse<CommentDto>.Ok(comment));
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的评论信息</returns>
    [HttpPut("comments/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<CommentDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateComment(long id, [FromBody] UpdateCommentRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<CommentDto>.Fail("未授权，请先登录"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CommentDto>.Fail("请求参数无效"));
        }

        var (comment, error) = await _commentService.UpdateCommentAsync(id, currentUser.UserId, request);

        if (comment == null || error != null)
        {
            return BadRequest(ApiResponse<CommentDto>.Fail(error ?? "更新评论失败"));
        }

        return Ok(ApiResponse<CommentDto>.Ok(comment, "更新成功"));
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    /// <param name="id">评论ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("comments/{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteComment(long id)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<bool>.Fail("未授权，请先登录"));
        }

        var (success, error) = await _commentService.DeleteCommentAsync(id, currentUser.UserId);

        if (!success || error != null)
        {
            return NotFound(ApiResponse<bool>.Fail(error ?? "删除评论失败"));
        }

        return Ok(ApiResponse<bool>.Ok(true, "评论已删除"));
    }
}
