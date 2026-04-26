using ConfluenceLite.Api.Services;

namespace ConfluenceLite.Api.Routes;

public static class OfficeRoutes
{
    public static void MapOfficeRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/office")
            .WithTags("Office");

        // ========== 预览 Office 文件(PDF) ==========
        group.MapGet("/preview", async (
            string? path,
            OfficePreviewService officePreviewService) =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return Results.BadRequest("缺少 path 参数");

            try
            {
                var pdfBytes = await officePreviewService.GetOrConvertPdfAsync(path);
                return Results.File(pdfBytes, "application/pdf");
            }
            catch (FileNotFoundException ex)
            {
                return Results.NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 503);
            }
        });
    }
}
