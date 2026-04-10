using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Controllers;

/// <summary>
/// 用户管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TokenService _tokenService;

    public UserController(UserService userService, TokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="request">登录请求</param>
    /// <returns>登录响应</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<LoginResponse>.Fail("请求参数无效"));
        }

        var (user, error) = await _userService.LoginAsync(request);

        if (user == null || error != null)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail(error ?? "登录失败"));
        }

        var token = _tokenService.GenerateToken(user.Id, user.Username);

        var response = new LoginResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = 1440, // 24小时
            User = user
        };

        return Ok(ApiResponse<LoginResponse>.Ok(response, "登录成功"));
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="request">创建用户请求</param>
    /// <returns>创建的用户信息</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Fail("请求参数无效"));
        }

        var (user, error) = await _userService.CreateUserAsync(request);

        if (user == null || error != null)
        {
            return BadRequest(ApiResponse<UserDto>.Fail(error ?? "创建用户失败"));
        }

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ApiResponse<UserDto>.Ok(user, "用户创建成功"));
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns>当前用户信息</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("未授权"));
        }

        var user = await _userService.GetUserByIdAsync(currentUser.UserId);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("用户不存在"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>用户信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(long id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(ApiResponse<UserDto>.Fail("用户不存在"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    /// <returns>用户列表</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserList([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var request = new PagedRequest { Page = page, PageSize = pageSize };
        var result = await _userService.GetUserListAsync(request);

        return Ok(ApiResponse<PagedResponse<UserDto>>.Ok(result));
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>更新后的用户信息</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateUser(long id, [FromBody] UpdateUserRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<UserDto>.Fail("未授权"));
        }

        // 只能更新自己的信息
        if (currentUser.UserId != id)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<UserDto>.Fail("请求参数无效"));
        }

        var (user, error) = await _userService.UpdateUserAsync(id, request);

        if (user == null || error != null)
        {
            return BadRequest(ApiResponse<UserDto>.Fail(error ?? "更新用户失败"));
        }

        return Ok(ApiResponse<UserDto>.Ok(user, "更新成功"));
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="request">修改密码请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<bool>.Fail("未授权"));
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<bool>.Fail("请求参数无效"));
        }

        var (success, error) = await _userService.ChangePasswordAsync(currentUser.UserId, request);

        if (!success || error != null)
        {
            return BadRequest(ApiResponse<bool>.Fail(error ?? "修改密码失败"));
        }

        return Ok(ApiResponse<bool>.Ok(true, "密码修改成功"));
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteUser(long id)
    {
        var currentUser = HttpContext.Items["CurrentUser"] as CurrentUser;
        if (currentUser == null || !currentUser.IsAuthenticated)
        {
            return Unauthorized(ApiResponse<bool>.Fail("未授权"));
        }

        // 只能删除自己的账户
        if (currentUser.UserId != id)
        {
            return Forbid();
        }

        var (success, error) = await _userService.DeleteUserAsync(id);

        if (!success || error != null)
        {
            return NotFound(ApiResponse<bool>.Fail(error ?? "删除用户失败"));
        }

        return Ok(ApiResponse<bool>.Ok(true, "用户已删除"));
    }
}
