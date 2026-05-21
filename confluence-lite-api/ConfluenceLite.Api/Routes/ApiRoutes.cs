namespace ConfluenceLite.Api.Routes;

/// <summary>
/// API 路由统一注册入口
/// </summary>
public static class ApiRoutes
{
    public static void RegisterRoutes(WebApplication app)
    {
        app.MapSystemRoutes();
        app.MapSetupRoutes();
        app.MapAuthRoutes();
        app.MapUserRoutes();
        app.MapUserGroupRoutes();
        app.MapWorkspaceRoutes();
        app.MapPageRoutes();
        app.MapAttachmentRoutes();
        app.MapUploadRoutes();
        app.MapActivityRoutes();
        app.MapOfficeRoutes();
        app.MapSearchRoutes();
    }
}
