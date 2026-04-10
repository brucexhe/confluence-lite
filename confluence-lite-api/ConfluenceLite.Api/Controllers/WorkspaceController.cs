using Microsoft.AspNetCore.Mvc;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Controllers;

/// <summary>
/// 工作空间管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class WorkspaceController : ControllerBase
{
    private readonly WorkspaceService _workspaceService;

    public WorkspaceController(WorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    /// <summary>
    /// 创建工作空间
    /// </summary>
    /// <param name="request">创建工作空间请求</param>
    /// <returns>创建的工作空间信息</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<WorkspaceDto>.Fail("未授权，请先登录"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<WorkspaceDto>.Fail("请求参数无效"));
        }

        var (workspace, error) = await _workspaceService.CreateWorkspaceAsync(currentUser.UserId, request);

        if (workspace == null || error != null)
        {
            return BadRequest(ApiResponse<WorkspaceDto>.Fail(error ?? "创建工作空间失败"));
        }

        return CreatedAtAction(nameof(GetWorkspace), new { id = workspace.Id }, ApiResponse<WorkspaceDto>.Ok(workspace, "工作空间创建成功"));
    }

    /// <summary>
    /// 获取工作空间信息
    /// </summary>
    /// <param name="id">工作空间ID</param>
    /// <returns>工作空间信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkspace(long id)
    {
        var workspace = await _workspaceService.GetWorkspaceByIdAsync(id);
        if (workspace == null)
        {
            return NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));
        }

        return Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
    }

    /// <summary>
    /// 根据Key获取工作空间
    /// </summary>
    /// <param name="key">工作空间标识</param>
    /// <returns>工作空间信息</returns>
    [HttpGet("key/{key}")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkspaceByKey(string key)
    {
        var workspace = await _workspaceService.GetWorkspaceByKeyAsync(key);
        if (workspace == null)
        {
            return NotFound(ApiResponse<WorkspaceDto>.Fail("工作空间不存在"));
        }

        return Ok(ApiResponse<WorkspaceDto>.Ok(workspace));
    }

    /// <summary>
    /// 获取工作空间列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>工作空间列表</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<WorkspaceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkspaceList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var request = new PagedRequest { Page = page, PageSize = pageSize };
        var result = await _workspaceService.GetWorkspaceListAsync(request);

        return Ok(ApiResponse<PagedResponse<WorkspaceDto>>.Ok(result));
    }

    /// <summary>
    /// 获取当前用户的工作空间列表
    /// </summary>
    /// <returns>工作空间列表</returns>
    [HttpGet("my")]
    [ProducesResponseType(typeof(ApiResponse<List<WorkspaceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<WorkspaceDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyWorkspaces()
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<List<WorkspaceDto>>.Fail("未授权，请先登录"));
        }

        var workspaces = await _workspaceService.GetUserWorkspacesAsync(currentUser.UserId);

        return Ok(ApiResponse<List<WorkspaceDto>>.Ok(workspaces));
    }

    /// <summary>
    /// 更新工作空间
    /// </summary>
    /// <param name="id">工作空间ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的工作空间信息</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateWorkspace(long id, [FromBody] UpdateWorkspaceRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<WorkspaceDto>.Fail("未授权，请先登录"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<WorkspaceDto>.Fail("请求参数无效"));
        }

        var (workspace, error) = await _workspaceService.UpdateWorkspaceAsync(id, currentUser.UserId, request);

        if (workspace == null || error != null)
        {
            return BadRequest(ApiResponse<WorkspaceDto>.Fail(error ?? "更新工作空间失败"));
        }

        return Ok(ApiResponse<WorkspaceDto>.Ok(workspace, "更新成功"));
    }

    /// <summary>
    /// 删除工作空间
    /// </summary>
    /// <param name="id">工作空间ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteWorkspace(long id)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<bool>.Fail("未授权，请先登录"));
        }

        var (success, error) = await _workspaceService.DeleteWorkspaceAsync(id, currentUser.UserId);

        if (!success || error != null)
        {
            return NotFound(ApiResponse<bool>.Fail(error ?? "删除工作空间失败"));
        }

        return Ok(ApiResponse<bool>.Ok(true, "工作空间已删除"));
    }
}
