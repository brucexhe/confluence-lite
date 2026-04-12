namespace ConfluenceLite.Api.Routes;

/// <summary>
/// API 路由统一注册入口
/// </summary>
public static class ApiRoutes
{
    public static void RegisterRoutes(WebApplication app)
    {
        app.MapSetupRoutes();
        app.MapUserRoutes();
        app.MapWorkspaceRoutes();
        app.MapPageRoutes();
    }
}
