using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Routes;

public static class AttachmentRoutes
{
    public static void MapAttachmentRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/attachment")
            .WithTags("Attachments");

        // ========== 上传附件 ==========
        group.MapPost("/upload", async (
            HttpRequest request,
            long pageId,
            string? comment,
            HttpContext context,
            UploadService uploadService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            // 验证页面ID
            if (pageId <= 0)
                return Results.BadRequest(ApiResponse<AttachmentDto>.Fail("无效的页面ID"));

            // 获取上传的文件
            var form = await request.ReadFormAsync();
            var file = form.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
                return Results.BadRequest(ApiResponse<AttachmentDto>.Fail("请选择要上传的文件"));

            // 上传文件
            var (attachment, error) = await uploadService.UploadAttachmentAsync(
                pageId,
                currentUser.UserId,
                file,
                comment);

            if (attachment == null || error != null)
                return Results.BadRequest(ApiResponse<AttachmentDto>.Fail(error ?? "上传失败"));

            return Results.Ok(ApiResponse<AttachmentDto>.Ok(attachment, "上传成功"));
        })
        .DisableAntiforgery(); // 允许文件上传

        // ========== 获取页面附件列表 ==========
        group.MapGet("/page/{pageId}", async (
            long pageId,
            HttpContext context,
            UploadService uploadService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            if (!await uploadService.CanAccessPageAsync(pageId, currentUser))
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            var attachments = await uploadService.GetPageAttachmentsAsync(pageId);
            return Results.Ok(ApiResponse<List<AttachmentListItemDto>>.Ok(attachments));
        });

        // ========== 获取附件详情 ==========
        group.MapGet("/{id}", async (
            long id,
            HttpContext context,
            UploadService uploadService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var attachment = await uploadService.GetAttachmentByIdAsync(id);
            if (attachment == null)
                return Results.NotFound(ApiResponse<AttachmentDto>.Fail("附件不存在"));

            if (!await uploadService.CanAccessPageAsync(attachment.PageId, currentUser))
                return Results.Json(new ForbiddenResponse(), AppJsonContext.Default.ForbiddenResponse, statusCode: 403);

            return Results.Ok(ApiResponse<AttachmentDto>.Ok(attachment));
        });

        // ========== 鉴权下载附件 ==========
        group.MapGet("/{id}/download", async (
            long id,
            HttpContext context,
            UploadService uploadService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (stream, contentType, fileName, error) = await uploadService.GetAttachmentDownloadAsync(id, currentUser);
            if (stream == null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error ?? "附件不存在或无权访问"));

            return Results.File(stream, contentType, fileName);
        });

        // ========== 删除附件 ==========
        group.MapDelete("/{id}", async (
            long id,
            HttpContext context,
            UploadService uploadService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            if (currentUser == null || !currentUser.IsAuthenticated)
                return Results.Unauthorized();

            var (success, error) = await uploadService.DeleteAttachmentAsync(id, currentUser.UserId);
            if (!success || error != null)
                return Results.NotFound(ApiResponse<bool>.Fail(error ?? "删除失败"));

            return Results.Ok(ApiResponse<bool>.Ok(true, "附件已删除"));
        });

        // ========== 下载附件 (通过静态文件服务处理，这里仅作为参考) ==========
        // 实际文件访问路径: /uploads/attachments/{year}/{month}/{filename}
        // 在 Program.cs 中通过 UseStaticFiles 配置
    }
}
