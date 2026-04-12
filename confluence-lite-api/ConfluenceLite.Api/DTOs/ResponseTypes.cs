namespace ConfluenceLite.Api.DTOs;

/// <summary>
/// 健康检查响应
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = "Healthy";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Version { get; set; } = "1.0.0";
    public bool AotEnabled { get; set; } = true;
}

/// <summary>
/// API 信息响应
/// </summary>
public class ApiInfoResponse
{
    public string Name { get; set; } = "Confluence Lite API";
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = "轻量级知识库笔记 API - Native AOT";
    public ApiEndpoints Endpoints { get; set; } = new();
}

/// <summary>
/// API 端点信息
/// </summary>
public class ApiEndpoints
{
    public string Auth { get; set; } = "/api/user";
    public string Workspaces { get; set; } = "/api/workspace";
    public string Pages { get; set; } = "/api/page";
    public string Health { get; set; } = "/health";
}
