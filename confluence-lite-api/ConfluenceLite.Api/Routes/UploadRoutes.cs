using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Routes;

public static class UploadRoutes
{
    public static void MapUploadRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/upload")
            .WithTags("Uploads");

        // POST /api/upload - 通用文件上传
        group.MapPost("/", async (HttpContext context, UploadService uploadService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var file = context.Request.Form.Files["file"];
            if (file == null || file.Length == 0)
                return Results.BadRequest(ApiResponse<string>.Fail("No file uploaded"));

            var (filePath, error) = await uploadService.UploadFileAsync(
                file,
                currentUser.UserId,
                "images"
            );

            if (error != null || filePath == null)
                return Results.BadRequest(ApiResponse<string>.Fail(error ?? "Upload failed"));

            return Results.Ok(ApiResponse<string>.Ok(filePath, "Upload successful"));
        })
        .DisableAntiforgery();
    }
}
