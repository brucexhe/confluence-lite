using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 用户组 DTO
/// </summary>
public class UserGroupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 创建用户组请求
/// </summary>
public class CreateUserGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 更新用户组请求
/// </summary>
public class UpdateUserGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 添加用户组成员请求
/// </summary>
public class AddGroupMembersRequest
{
    public List<long> UserIds { get; set; } = new();
}

/// <summary>
/// 用户组成员 DTO
/// </summary>
public class UserGroupMemberDto
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// 用户组成员列表响应
/// </summary>
public class UserGroupMembersResponse
{
    public long GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<UserGroupMemberDto> Members { get; set; } = new();
    public int Total { get; set; }
}
