using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;

namespace ConfluenceLite.Api.Routes;

public static class SetupRoutes
{
    public static void MapSetupRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/setup")
            .WithTags("Setup");

        group.MapGet("/status", (SetupService setupService) =>
        {
            var installed = setupService.IsInstalled();
            return Results.Ok(ApiResponse<SetupStatusResponse>.Ok(
                new SetupStatusResponse { Installed = installed }));
        });

        group.MapPost("/test-connection", async (
            DatabaseConfigRequest request,
            SetupService setupService) =>
        {
            if (setupService.IsInstalled())
                return Results.BadRequest(ApiResponse<TestConnectionResponse>.Fail("系统已安装"));

            var (result, error) = await setupService.TestConnectionAsync(request);
            if (error != null)
                return Results.BadRequest(ApiResponse<TestConnectionResponse>.Fail(error));

            return Results.Ok(ApiResponse<TestConnectionResponse>.Ok(result!));
        });

        group.MapPost("/install", async (
            SetupRequest request,
            SetupService setupService) =>
        {
            if (setupService.IsInstalled())
                return Results.BadRequest(ApiResponse<SetupResponse>.Fail("系统已安装"));

            var (result, error) = await setupService.InstallAsync(request);
            if (error != null || result == null)
                return Results.BadRequest(ApiResponse<SetupResponse>.Fail(error ?? "安装失败"));

            return Results.Ok(ApiResponse<SetupResponse>.Ok(result, "安装成功"));
        });
    }
}
