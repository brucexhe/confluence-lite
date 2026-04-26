using System.Collections.Concurrent;
using System.Security.Cryptography;
using ConfluenceLite.Api.Data;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// Office 文件预览服务 - 调用 Gotenberg 将 Office 文件转为 PDF
/// </summary>
public class OfficePreviewService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppConfiguration _appConfig;
    private readonly string _wwwrootPath;
    private readonly string _cacheDir;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".docx", ".xlsx", ".pptx"
    };

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _conversionLocks = new();

    public OfficePreviewService(IHttpClientFactory httpClientFactory, AppConfiguration appConfig)
    {
        _httpClientFactory = httpClientFactory;
        _appConfig = appConfig;

        var contentRoot = Directory.GetCurrentDirectory();
        _wwwrootPath = Path.Combine(contentRoot, "wwwroot");
        _cacheDir = Path.Combine(contentRoot, "data", "cache", "office");

        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
    }

    public async Task<byte[]> GetOrConvertPdfAsync(string relativePath)
    {
        if (!_appConfig.Gotenberg.Enabled)
            throw new InvalidOperationException("Office 预览功能未启用");

        // 安全校验：禁止路径穿越
        if (relativePath.Contains(".."))
            throw new ArgumentException("非法路径");

        var ext = Path.GetExtension(relativePath);
        if (!SupportedExtensions.Contains(ext))
            throw new ArgumentException($"不支持的文件类型: {ext}");

        var fullPath = Path.Combine(_wwwrootPath, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("源文件不存在", fullPath);

        // 计算缓存 key
        var hash = await ComputeFileHashAsync(fullPath);
        var cachePath = Path.Combine(_cacheDir, $"{hash}.pdf");

        // 缓存命中
        if (File.Exists(cachePath))
            return await File.ReadAllBytesAsync(cachePath);

        // 并发控制：同一文件只转换一次
        var semaphore = _conversionLocks.GetOrAdd(hash, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        try
        {
            // double-check
            if (File.Exists(cachePath))
                return await File.ReadAllBytesAsync(cachePath);

            var pdfBytes = await ConvertViaGotenbergAsync(fullPath);
            await File.WriteAllBytesAsync(cachePath, pdfBytes);
            return pdfBytes;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<byte[]> ConvertViaGotenbergAsync(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        await using var fileStream = File.OpenRead(filePath);

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "files", fileName);

        var client = _httpClientFactory.CreateClient("Gotenberg");
        client.Timeout = TimeSpan.FromSeconds(_appConfig.Gotenberg.TimeoutSeconds);

        var baseUrl = _appConfig.Gotenberg.BaseUrl.TrimEnd('/');
        var response = await client.PostAsync($"{baseUrl}/forms/libreoffice/convert", content);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Gotenberg 转换失败 (HTTP {(int)response.StatusCode}): {errorBody}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }

    private static async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).Substring(0, 32).ToLowerInvariant();
    }
}
