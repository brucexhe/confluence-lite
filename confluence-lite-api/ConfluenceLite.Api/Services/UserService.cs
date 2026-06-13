using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;
using Npgsql;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 用户服务 - Native AOT 兼容
/// </summary>
public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<(UserDto? user, string? error)> LoginAsync(LoginRequest request)
    {
        var user = await _db.Db.Queryable<User>()
            .Where(u => u.Username == request.Username && u.Status == 1)
            .FirstAsync();

        if (user == null)
        {
            return (null, "用户名或密码错误");
        }

        // 检查账户是否锁定
        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now)
        {
            var remaining = user.LockedUntil.Value - DateTime.Now;
            return (null, $"账户已锁定，请 {(int)Math.Ceiling(remaining.TotalMinutes)} 分钟后再试");
        }

        if (!PasswordService.VerifyPassword(request.Password, user.PasswordHash))
        {
            // 密码错误，增加失败计数
            user.FailedLoginAttempts++;

            // 连续失败 5 次锁定 15 分钟
            if (user.FailedLoginAttempts >= 10)
            {
                user.LockedUntil = DateTime.Now.AddMinutes(15);
            }

            user.UpdatedAt = DateTime.Now;
            await _db.Db.Updateable(user)
                .UpdateColumns(u => new { u.FailedLoginAttempts, u.LockedUntil, u.UpdatedAt })
                .ExecuteCommandAsync();

            return (null, "用户名或密码错误");
        }

        // 登录成功，重置失败计数
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;
        await _db.Db.Updateable(user)
            .UpdateColumns(u => new { u.FailedLoginAttempts, u.LockedUntil, u.LastLoginAt, u.UpdatedAt })
            .ExecuteCommandAsync();

        return (MapToDto(user), null);
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    public async Task<(UserDto? user, string? error)> CreateUserAsync(CreateUserRequest request)
    {
        // 检查用户名是否存在
        var exists = await _db.Db.Queryable<User>()
            .Where(u => u.Username == request.Username)
            .AnyAsync();

        if (exists)
        {
            return (null, "用户名已存在");
        }

        // 如果指定了邮箱，检查邮箱是否存在
        if (!string.IsNullOrEmpty(request.Email))
        {
            var emailExists = await _db.Db.Queryable<User>()
                .Where(u => u.Email == request.Email)
                .AnyAsync();

            if (emailExists)
            {
                return (null, "邮箱已被使用");
            }
        }

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = PasswordService.HashPassword(request.Password),
            DisplayName = request.DisplayName ?? request.Username,
            Status = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        int userId;
        try
        {
            userId = await _db.Db.Insertable(user).ExecuteReturnIdentityAsync();
        }
        catch (NpgsqlException ex) when (ex.SqlState == "23505")
        {
            return (null, "用户名已存在");
        }
        user.Id = userId;

        return (MapToDto(user), null);
    }

    /// <summary>
    /// 获取用户信息
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(long id)
    {
        var user = await _db.Users.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    /// <summary>
    /// 获取用户列表
    /// </summary>
    public async Task<PagedResponse<UserDto>> GetUserListAsync(PagedRequest request)
    {
        var total = await _db.Db.Queryable<User>().CountAsync();

        var users = await _db.Db.Queryable<User>()
            .OrderBy(u => u.Id)
            .Skip(request.Skip)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResponse<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    /// <summary>
    /// 更新用户
    /// </summary>
    public async Task<(UserDto? user, string? error)> UpdateUserAsync(long id, UpdateUserRequest request)
    {
        var user = await _db.Users.GetByIdAsync(id);
        if (user == null)
        {
            return (null, "用户不存在");
        }

        // 如果更新邮箱，检查是否重复
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var emailExists = await _db.Db.Queryable<User>()
                .Where(u => u.Email == request.Email && u.Id != id)
                .AnyAsync();

            if (emailExists)
            {
                return (null, "邮箱已被使用");
            }
            user.Email = request.Email;
        }

        if (request.DisplayName != null)
        {
            user.DisplayName = request.DisplayName;
        }

        if (request.Status.HasValue)
        {
            user.Status = request.Status.Value;
        }

        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }

        user.UpdatedAt = DateTime.Now;

        await _db.Users.UpdateAsync(user);

        return (MapToDto(user), null);
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    public async Task<(bool success, string? error)> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "用户不存在");
        }

        if (!PasswordService.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            return (false, "原密码错误");
        }

        user.PasswordHash = PasswordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.Now;

        await _db.Users.UpdateAsync(user);

        return (true, null);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<(bool success, string? error)> DeleteUserAsync(long id)
    {
        var user = await _db.Users.GetByIdAsync(id);
        if (user == null)
        {
            return (false, "用户不存在");
        }

        await _db.Users.DeleteAsync(user);
        return (true, null);
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            Status = user.Status,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
