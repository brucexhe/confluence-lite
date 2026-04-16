using System.Security.Cryptography;
using System.Text.Json;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.Models;
using ConfluenceLite.Api.DTOs;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 附件上传服务 - Native AOT 兼容
/// </summary>
public class UploadService
{
    private readonly AppDbContext _db;
    private readonly AttachmentOptions _options;
    private readonly string _uploadRootPath;

    public UploadService(AppDbContext db, AppConfiguration appConfig)
    {
        _db = db;
        _options = appConfig.Attachment;

        // 获取项目根目录
        var contentRoot = Directory.GetCurrentDirectory();
        _uploadRootPath = Path.Combine(contentRoot, "wwwroot", _options.UploadPath);

        // 确保上传目录存在
        EnsureUploadDirectoryExists();
    }

    /// <summary>
    /// 确保上传目录存在
    /// </summary>
    private void EnsureUploadDirectoryExists()
    {
        if (!Directory.Exists(_uploadRootPath))
        {
            Directory.CreateDirectory(_uploadRootPath);
        }
    }

    /// <summary>
    /// 获取当前年月路径
    /// </summary>
    private string GetYearMonthPath()
    {
        var now = DateTime.Now;
        return Path.Combine(now.Year.ToString(), now.Month.ToString("00"));
    }

    /// <summary>
    /// 计算文件SHA256哈希
    /// </summary>
    private async Task<string> CalculateFileHashAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        stream.Position = 0; // 重置流位置
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// 验证文件扩展名
    /// </summary>
    private (bool valid, string? error) ValidateFileExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return (false, "文件没有扩展名");
        }

        if (!_options.AllowedExtensions.Contains(extension))
        {
            return (false, $"不支持的文件类型: {extension}");
        }

        return (true, null);
    }

    /// <summary>
    /// 上传附件
    /// </summary>
    public async Task<(AttachmentDto? attachment, string? error)> UploadAttachmentAsync(
        long pageId,
        long creatorId,
        IFormFile file,
        string? comment = null)
    {
        // 验证文件扩展名
        var (validExt, extError) = ValidateFileExtension(file.FileName);
        if (!validExt)
        {
            return (null, extError);
        }

        // 验证文件大小
        if (file.Length > _options.MaxFileSizeBytes)
        {
            return (null, $"文件大小超过限制 ({_options.MaxFileSizeBytes / 1024 / 1024}MB)");
        }

        if (file.Length == 0)
        {
            return (null, "文件为空");
        }

        // 验证页面是否存在
        var page = await _db.Pages.GetByIdAsync(pageId);
        if (page == null)
        {
            return (null, "页面不存在");
        }

        // 计算文件哈希
        await using var fileStream = file.OpenReadStream();
        var fileHash = await CalculateFileHashAsync(fileStream);

        // 检查是否已存在相同哈希的文件（去重）- 如果存在则直接返回现有附件
        var existingAttachment = await _db.Db.Queryable<Attachment>()
            .Where(a => a.FileHash == fileHash && a.PageId == pageId && !a.IsDeleted)
            .FirstAsync();

        if (existingAttachment != null)
        {
            // 文件已存在，返回现有附件（复用）
            return (await MapToDtoAsync(existingAttachment), null);
        }

        // 生成年月目录
        var yearMonthPath = GetYearMonthPath();
        var targetDir = Path.Combine(_uploadRootPath, yearMonthPath);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // 生成唯一文件名
        //var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var originalFileName = Path.GetFileName(file.FileName);
        var uniqueFileName = $"{fileHash}_{originalFileName}";
        // storagePath 需要包含 attachments 前缀，以便前端 /uploads/ 能正确访问
        var storagePath = Path.Combine("attachments", yearMonthPath, uniqueFileName).Replace("\\", "/");

        // 保存文件（storagePath 包含 attachments/，需要去掉前缀才能与 _uploadRootPath 拼接）
        var relativePath = storagePath["attachments/".Length..];
        var fullPath = Path.Combine(_uploadRootPath, relativePath);
        await using (var fileStream2 = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream2);
        }

        // 保存到数据库
        var attachment = new Attachment
        {
            PageId = pageId,
            FileName = originalFileName,
            FileSize = file.Length,
            ContentType = file.ContentType,
            StoragePath = storagePath,
            FileHash = fileHash,
            CreatorId = creatorId,
            Comment = comment,
            Version = 1,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var attachmentId = await _db.Db.Insertable(attachment).ExecuteReturnIdentityAsync();
        attachment.Id = attachmentId;

        return (await MapToDtoAsync(attachment), null);
    }

    /// <summary>
    /// 获取页面附件列表
    /// </summary>
    public async Task<List<AttachmentListItemDto>> GetPageAttachmentsAsync(long pageId)
    {
        var attachments = await _db.Db.Queryable<Attachment>()
            .Where(a => a.PageId == pageId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var dtos = new List<AttachmentListItemDto>();
        foreach (var attachment in attachments)
        {
            dtos.Add(new AttachmentListItemDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                CreatedAt = attachment.CreatedAt
            });
        }

        return dtos;
    }

    /// <summary>
    /// 获取附件详情
    /// </summary>
    public async Task<AttachmentDto?> GetAttachmentByIdAsync(long id)
    {
        var attachment = await _db.Attachments.GetByIdAsync(id);
        if (attachment == null || attachment.IsDeleted)
        {
            return null;
        }

        return await MapToDtoAsync(attachment);
    }

    /// <summary>
    /// 删除附件
    /// </summary>
    public async Task<(bool success, string? error)> DeleteAttachmentAsync(long id, long userId)
    {
        var attachment = await _db.Attachments.GetByIdAsync(id);
        if (attachment == null)
        {
            return (false, "附件不存在");
        }

        // 检查权限：只有上传者可以删除
        if (attachment.CreatorId != userId)
        {
            return (false, "无权限删除此附件");
        }

        // 软删除数据库记录
        attachment.IsDeleted = true;
        await _db.Attachments.UpdateAsync(attachment);

        // 可选：同时删除物理文件
        // storagePath 包含 "attachments/" 前缀，但 _uploadRootPath 已经包含了，需要去掉
        var relativePath = attachment.StoragePath.StartsWith("attachments/")
            ? attachment.StoragePath["attachments/".Length..]
            : attachment.StoragePath;
        var fullPath = Path.Combine(_uploadRootPath, relativePath);
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                // 记录错误但继续
                Console.WriteLine($"[UploadService] 删除文件失败: {ex.Message}");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// 获取附件物理文件路径
    /// </summary>
    public string? GetAttachmentFilePath(long id)
    {
        var attachment = _db.Attachments.GetById(id);
        if (attachment == null || attachment.IsDeleted)
        {
            return null;
        }

        // storagePath 包含 "attachments/" 前缀，但 _uploadRootPath 已经包含了
        // 所以需要去掉 "attachments/" 前缀才能正确拼接物理路径
        var relativePath = attachment.StoragePath.StartsWith("attachments/")
            ? attachment.StoragePath["attachments/".Length..]
            : attachment.StoragePath;
        var fullPath = Path.Combine(_uploadRootPath, relativePath);
        return File.Exists(fullPath) ? fullPath : null;
    }

    /// <summary>
    /// 映射到DTO
    /// </summary>
    private async Task<AttachmentDto> MapToDtoAsync(Attachment attachment)
    {
        var creator = await _db.Users.GetByIdAsync(attachment.CreatorId);

        return new AttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType,
            StoragePath = attachment.StoragePath,
            PageId = attachment.PageId,
            CreatorId = attachment.CreatorId,
            CreatedAt = attachment.CreatedAt,
            Creator = creator == null ? null : new UserSummaryDto
            {
                Id = creator.Id,
                Username = creator.Username,
                DisplayName = creator.DisplayName
            }
        };
    }
}
