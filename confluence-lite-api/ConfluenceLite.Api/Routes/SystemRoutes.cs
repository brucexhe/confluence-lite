using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;
using ConfluenceLite.Api.Services;
using ConfluenceLite.Api.Middleware;

namespace ConfluenceLite.Api.Routes;

/// <summary>
/// 系统路由
/// </summary>
public static class SystemRoutes
{
    public static void MapSystemRoutes(this WebApplication app)
    {
        // ========== 健康检查 ==========
        app.MapGet("/health", () =>
        {
            return Results.Json(new HealthResponse(), AppJsonContext.Default.HealthResponse);
        });

        var group = app.MapGroup("/api/system")
            .WithTags("System");

        // ========== Office 预览配置 ==========
        group.MapGet("/office-preview-config", (AppConfiguration appConfig) =>
        {
            var dto = new OfficePreviewConfigDto
            {
                Enabled = appConfig.Gotenberg.Enabled,
                BaseUrl = appConfig.Gotenberg.BaseUrl,
                TimeoutSeconds = appConfig.Gotenberg.TimeoutSeconds
            };
            return Results.Ok(ApiResponse<OfficePreviewConfigDto>.Ok(dto));
        });

        group.MapPut("/office-preview-config", async (
            OfficePreviewConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env,
            IAuditLogService auditService,
            HttpContext context) =>
        {
            // 保存旧值用于审计
            var oldValue = new OfficePreviewConfigDto
            {
                Enabled = appConfig.Gotenberg.Enabled,
                BaseUrl = appConfig.Gotenberg.BaseUrl,
                TimeoutSeconds = appConfig.Gotenberg.TimeoutSeconds
            };

            // 更新内存中的配置（单例）
            appConfig.Gotenberg.Enabled = dto.Enabled;
            appConfig.Gotenberg.BaseUrl = dto.BaseUrl;
            appConfig.Gotenberg.TimeoutSeconds = dto.TimeoutSeconds;

            // 持久化到 appsettings.runtime.json
            SaveGotenbergConfig(env, dto);

            // 记录审计日志
            await auditService.EnqueueChangeAsync(context, "office", oldValue, dto);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        group.MapPost("/office-preview-config/test", async (
            OfficePreviewConfigDto dto,
            IHttpClientFactory httpClientFactory) =>
        {
            if (string.IsNullOrWhiteSpace(dto.BaseUrl))
                return Results.Ok(ApiResponse<bool>.Fail("请先配置服务地址"));

            try
            {
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var baseUrl = dto.BaseUrl.TrimEnd('/');
                var response = await client.GetAsync($"{baseUrl}/health");
                if (response.IsSuccessStatusCode)
                    return Results.Ok(ApiResponse<bool>.Ok(true, "连接成功"));
                return Results.Ok(ApiResponse<bool>.Fail($"连接失败 (HTTP {(int)response.StatusCode})"));
            }
            catch (Exception ex)
            {
                return Results.Ok(ApiResponse<bool>.Fail($"连接失败: {ex.Message}"));
            }
        });

        // ========== 站点配置 ==========

        group.MapGet("/site-config", (AppConfiguration appConfig) =>
        {
            var dto = new SiteConfigDto
            {
                SiteName = appConfig.SiteSettings.SiteName,
                SiteDescription = appConfig.SiteSettings.SiteDescription,
                SiteLogo = appConfig.SiteSettings.SiteLogo,
                SiteDomain = appConfig.SiteSettings.SiteDomain,
                DefaultLanguage = appConfig.SiteSettings.DefaultLanguage,
                DefaultHomePage = appConfig.SiteSettings.DefaultHomePage,
                Timezone = appConfig.SiteSettings.Timezone,
                AllowRegistration = appConfig.SiteSettings.AllowRegistration
            };
            return Results.Ok(ApiResponse<SiteConfigDto>.Ok(dto));
        });

        group.MapPut("/site-config", async (
            SiteConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env,
            IAuditLogService auditService,
            HttpContext context) =>
        {
            // 保存旧值用于审计
            var oldValue = new SiteConfigDto
            {
                SiteName = appConfig.SiteSettings.SiteName,
                SiteDescription = appConfig.SiteSettings.SiteDescription,
                SiteLogo = appConfig.SiteSettings.SiteLogo,
                SiteDomain = appConfig.SiteSettings.SiteDomain,
                DefaultLanguage = appConfig.SiteSettings.DefaultLanguage,
                DefaultHomePage = appConfig.SiteSettings.DefaultHomePage,
                Timezone = appConfig.SiteSettings.Timezone,
                AllowRegistration = appConfig.SiteSettings.AllowRegistration
            };

            appConfig.SiteSettings.SiteName = dto.SiteName;
            appConfig.SiteSettings.SiteDescription = dto.SiteDescription;
            appConfig.SiteSettings.SiteLogo = dto.SiteLogo;
            appConfig.SiteSettings.SiteDomain = dto.SiteDomain;
            appConfig.SiteSettings.DefaultLanguage = dto.DefaultLanguage;
            appConfig.SiteSettings.DefaultHomePage = dto.DefaultHomePage;
            appConfig.SiteSettings.Timezone = dto.Timezone;
            appConfig.SiteSettings.AllowRegistration = dto.AllowRegistration;

            SaveSiteSettingsConfig(env, dto);

            // 记录审计日志
            await auditService.EnqueueChangeAsync(context, "site", oldValue, dto);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        // ========== 显示配置 ==========

        group.MapGet("/display-config", (AppConfiguration appConfig) =>
        {
            var dto = new DisplayConfigDto
            {
                DefaultTheme = appConfig.DisplaySettings.DefaultTheme,
                PrimaryColor = appConfig.DisplaySettings.PrimaryColor,
                CompactMode = appConfig.DisplaySettings.CompactMode,
                PageSize = appConfig.DisplaySettings.PageSize,
                PageTreeExpandMode = appConfig.DisplaySettings.PageTreeExpandMode,
                ShowPageViews = appConfig.DisplaySettings.ShowPageViews,
                ShowAuthorInfo = appConfig.DisplaySettings.ShowAuthorInfo,
                ShowLastModified = appConfig.DisplaySettings.ShowLastModified,
                DefaultEditorMode = appConfig.DisplaySettings.DefaultEditorMode,
                AutoSaveInterval = appConfig.DisplaySettings.AutoSaveInterval,
                EnableSpellCheck = appConfig.DisplaySettings.EnableSpellCheck,
                DefaultSidebarWidth = appConfig.DisplaySettings.DefaultSidebarWidth,
                ShowSpaceIcon = appConfig.DisplaySettings.ShowSpaceIcon,
                AllowCollapseSidebar = appConfig.DisplaySettings.AllowCollapseSidebar
            };
            return Results.Ok(ApiResponse<DisplayConfigDto>.Ok(dto));
        });

        group.MapPut("/display-config", async (
            DisplayConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env,
            IAuditLogService auditService,
            HttpContext context) =>
        {
            // 保存旧值用于审计
            var oldValue = new DisplayConfigDto
            {
                DefaultTheme = appConfig.DisplaySettings.DefaultTheme,
                PrimaryColor = appConfig.DisplaySettings.PrimaryColor,
                CompactMode = appConfig.DisplaySettings.CompactMode,
                PageSize = appConfig.DisplaySettings.PageSize,
                PageTreeExpandMode = appConfig.DisplaySettings.PageTreeExpandMode,
                ShowPageViews = appConfig.DisplaySettings.ShowPageViews,
                ShowAuthorInfo = appConfig.DisplaySettings.ShowAuthorInfo,
                ShowLastModified = appConfig.DisplaySettings.ShowLastModified,
                DefaultEditorMode = appConfig.DisplaySettings.DefaultEditorMode,
                AutoSaveInterval = appConfig.DisplaySettings.AutoSaveInterval,
                EnableSpellCheck = appConfig.DisplaySettings.EnableSpellCheck,
                DefaultSidebarWidth = appConfig.DisplaySettings.DefaultSidebarWidth,
                ShowSpaceIcon = appConfig.DisplaySettings.ShowSpaceIcon,
                AllowCollapseSidebar = appConfig.DisplaySettings.AllowCollapseSidebar
            };

            appConfig.DisplaySettings.DefaultTheme = dto.DefaultTheme;
            appConfig.DisplaySettings.PrimaryColor = dto.PrimaryColor;
            appConfig.DisplaySettings.CompactMode = dto.CompactMode;
            appConfig.DisplaySettings.PageSize = dto.PageSize;
            appConfig.DisplaySettings.PageTreeExpandMode = dto.PageTreeExpandMode;
            appConfig.DisplaySettings.ShowPageViews = dto.ShowPageViews;
            appConfig.DisplaySettings.ShowAuthorInfo = dto.ShowAuthorInfo;
            appConfig.DisplaySettings.ShowLastModified = dto.ShowLastModified;
            appConfig.DisplaySettings.DefaultEditorMode = dto.DefaultEditorMode;
            appConfig.DisplaySettings.AutoSaveInterval = dto.AutoSaveInterval;
            appConfig.DisplaySettings.EnableSpellCheck = dto.EnableSpellCheck;
            appConfig.DisplaySettings.DefaultSidebarWidth = dto.DefaultSidebarWidth;
            appConfig.DisplaySettings.ShowSpaceIcon = dto.ShowSpaceIcon;
            appConfig.DisplaySettings.AllowCollapseSidebar = dto.AllowCollapseSidebar;

            SaveDisplaySettingsConfig(env, dto);

            // 记录审计日志
            await auditService.EnqueueChangeAsync(context, "display", oldValue, dto);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        // ========== 安全配置 ==========

        group.MapGet("/security-config", (AppConfiguration appConfig) =>
        {
            var s = appConfig.SecuritySettings;
            var dto = new SecurityConfigDto
            {
                AllowPublicRegistration = s.AllowPublicRegistration,
                RequireEmailVerification = s.RequireEmailVerification,
                DefaultUserRole = s.DefaultUserRole,
                MinPasswordLength = s.MinPasswordLength,
                PasswordComplexity = s.PasswordComplexity,
                PasswordExpireDays = s.PasswordExpireDays,
                SessionTimeout = s.SessionTimeout,
                AllowConcurrentSessions = s.AllowConcurrentSessions,
                AllowRememberMe = s.AllowRememberMe,
                IpWhitelist = s.IpWhitelist,
                EnableTwoFactor = s.EnableTwoFactor
            };
            return Results.Ok(ApiResponse<SecurityConfigDto>.Ok(dto));
        });

        group.MapPut("/security-config", async (
            SecurityConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env,
            IAuditLogService auditService,
            HttpContext context) =>
        {
            var s = appConfig.SecuritySettings;

            // 保存旧值用于审计
            var oldValue = new SecurityConfigDto
            {
                AllowPublicRegistration = s.AllowPublicRegistration,
                RequireEmailVerification = s.RequireEmailVerification,
                DefaultUserRole = s.DefaultUserRole,
                MinPasswordLength = s.MinPasswordLength,
                PasswordComplexity = s.PasswordComplexity,
                PasswordExpireDays = s.PasswordExpireDays,
                SessionTimeout = s.SessionTimeout,
                AllowConcurrentSessions = s.AllowConcurrentSessions,
                AllowRememberMe = s.AllowRememberMe,
                IpWhitelist = s.IpWhitelist,
                EnableTwoFactor = s.EnableTwoFactor
            };

            s.AllowPublicRegistration = dto.AllowPublicRegistration;
            s.RequireEmailVerification = dto.RequireEmailVerification;
            s.DefaultUserRole = dto.DefaultUserRole;
            s.MinPasswordLength = dto.MinPasswordLength;
            s.PasswordComplexity = dto.PasswordComplexity;
            s.PasswordExpireDays = dto.PasswordExpireDays;
            s.SessionTimeout = dto.SessionTimeout;
            s.AllowConcurrentSessions = s.AllowConcurrentSessions;
            s.AllowRememberMe = dto.AllowRememberMe;
            s.IpWhitelist = dto.IpWhitelist;
            s.EnableTwoFactor = dto.EnableTwoFactor;

            SaveSecuritySettingsConfig(env, dto);

            // 记录审计日志
            await auditService.EnqueueChangeAsync(context, "security", oldValue, dto);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        // ========== 邮件配置 ==========

        group.MapGet("/mail-config", (AppConfiguration appConfig) =>
        {
            var m = appConfig.MailSettings;
            var dto = new MailConfigDto
            {
                Enabled = m.Enabled,
                SmtpHost = m.SmtpHost,
                SmtpPort = m.SmtpPort,
                Encryption = m.Encryption,
                FromEmail = m.FromEmail,
                FromName = m.FromName,
                Username = m.Username,
                Password = "", // 不返回已保存的密码
                NotifyOnRegister = m.NotifyOnRegister,
                AdminEmail = m.AdminEmail,
                EmailSignature = m.EmailSignature
            };
            return Results.Ok(ApiResponse<MailConfigDto>.Ok(dto));
        });

        group.MapPut("/mail-config", async (
            MailConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env,
            IAuditLogService auditService,
            HttpContext context) =>
        {
            var m = appConfig.MailSettings;

            // 保存旧值用于审计（密码不返回）
            var oldValue = new MailConfigDto
            {
                Enabled = m.Enabled,
                SmtpHost = m.SmtpHost,
                SmtpPort = m.SmtpPort,
                Encryption = m.Encryption,
                FromEmail = m.FromEmail,
                FromName = m.FromName,
                Username = m.Username,
                Password = "", // 不记录旧密码
                NotifyOnRegister = m.NotifyOnRegister,
                AdminEmail = m.AdminEmail,
                EmailSignature = m.EmailSignature
            };

            m.Enabled = dto.Enabled;
            m.SmtpHost = dto.SmtpHost;
            m.SmtpPort = dto.SmtpPort;
            m.Encryption = dto.Encryption;
            m.FromEmail = dto.FromEmail;
            m.FromName = dto.FromName;
            m.Username = dto.Username;
            if (!string.IsNullOrEmpty(dto.Password))
                m.Password = dto.Password;
            m.NotifyOnRegister = dto.NotifyOnRegister;
            m.AdminEmail = dto.AdminEmail;
            m.EmailSignature = dto.EmailSignature;

            SaveMailSettingsConfig(env, dto, m.Password);

            // 记录审计日志（新值中的密码会被脱敏）
            var newValueForAudit = new MailConfigDto
            {
                Enabled = dto.Enabled,
                SmtpHost = dto.SmtpHost,
                SmtpPort = dto.SmtpPort,
                Encryption = dto.Encryption,
                FromEmail = dto.FromEmail,
                FromName = dto.FromName,
                Username = dto.Username,
                Password = !string.IsNullOrEmpty(dto.Password) ? "***" : "",
                NotifyOnRegister = dto.NotifyOnRegister,
                AdminEmail = dto.AdminEmail,
                EmailSignature = dto.EmailSignature
            };
            await auditService.EnqueueChangeAsync(context, "mail", oldValue, newValueForAudit);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        group.MapPost("/mail-config/test", (MailConfigDto dto) =>
        {
            // TODO: 实现邮件发送测试
            return Results.Ok(ApiResponse<bool>.Ok(true, "测试功能暂未实现"));
        });

        // ========== 身份验证配置 ==========

        group.MapGet("/auth-config", (AppConfiguration appConfig) =>
        {
            var a = appConfig.AuthSettings;
            var dto = new AuthConfigDto
            {
                PasswordEnabled = a.PasswordEnabled,
                EmailLoginEnabled = a.EmailLoginEnabled,
                OidcEnabled = a.OidcEnabled,
                OidcProviderName = a.OidcProviderName,
                OidcDiscoveryUrl = a.OidcDiscoveryUrl,
                OidcClientId = a.OidcClientId,
                OidcClientSecret = "", // 不返回已保存的密钥
                OidcScopes = a.OidcScopes,
                OidcAutoCreateUser = a.OidcAutoCreateUser,
                OidcDefaultRole = a.OidcDefaultRole,
                LdapEnabled = a.LdapEnabled,
                LdapUrl = a.LdapUrl,
                LdapBindDn = a.LdapBindDn,
                LdapBindPassword = "", // 不返回已保存的密码
                LdapBaseDn = a.LdapBaseDn,
                LdapUserFilter = a.LdapUserFilter
            };
            return Results.Ok(ApiResponse<AuthConfigDto>.Ok(dto));
        });

        group.MapPut("/auth-config", async (
            AuthConfigDto dto,
            AppConfiguration appConfig,
            IHostEnvironment env,
            IAuditLogService auditService,
            HttpContext context) =>
        {
            var a = appConfig.AuthSettings;

            // 保存旧值用于审计（敏感字段不返回）
            var oldValue = new AuthConfigDto
            {
                PasswordEnabled = a.PasswordEnabled,
                EmailLoginEnabled = a.EmailLoginEnabled,
                OidcEnabled = a.OidcEnabled,
                OidcProviderName = a.OidcProviderName,
                OidcDiscoveryUrl = a.OidcDiscoveryUrl,
                OidcClientId = a.OidcClientId,
                OidcClientSecret = "", // 不记录旧密钥
                OidcScopes = a.OidcScopes,
                OidcAutoCreateUser = a.OidcAutoCreateUser,
                OidcDefaultRole = a.OidcDefaultRole,
                LdapEnabled = a.LdapEnabled,
                LdapUrl = a.LdapUrl,
                LdapBindDn = a.LdapBindDn,
                LdapBindPassword = "", // 不记录旧密码
                LdapBaseDn = a.LdapBaseDn,
                LdapUserFilter = a.LdapUserFilter
            };

            a.PasswordEnabled = dto.PasswordEnabled;
            a.EmailLoginEnabled = dto.EmailLoginEnabled;
            a.OidcEnabled = dto.OidcEnabled;
            a.OidcProviderName = dto.OidcProviderName;
            a.OidcDiscoveryUrl = dto.OidcDiscoveryUrl;
            a.OidcClientId = dto.OidcClientId;
            if (!string.IsNullOrEmpty(dto.OidcClientSecret))
                a.OidcClientSecret = dto.OidcClientSecret;
            a.OidcScopes = dto.OidcScopes;
            a.OidcAutoCreateUser = dto.OidcAutoCreateUser;
            a.OidcDefaultRole = dto.OidcDefaultRole;
            a.LdapEnabled = dto.LdapEnabled;
            a.LdapUrl = dto.LdapUrl;
            a.LdapBindDn = dto.LdapBindDn;
            if (!string.IsNullOrEmpty(dto.LdapBindPassword))
                a.LdapBindPassword = dto.LdapBindPassword;
            a.LdapBaseDn = dto.LdapBaseDn;
            a.LdapUserFilter = dto.LdapUserFilter;

            SaveAuthSettingsConfig(env, dto, a.OidcClientSecret, a.LdapBindPassword);

            // 记录审计日志（敏感字段会被脱敏）
            var newValueForAudit = new AuthConfigDto
            {
                PasswordEnabled = dto.PasswordEnabled,
                EmailLoginEnabled = dto.EmailLoginEnabled,
                OidcEnabled = dto.OidcEnabled,
                OidcProviderName = dto.OidcProviderName,
                OidcDiscoveryUrl = dto.OidcDiscoveryUrl,
                OidcClientId = dto.OidcClientId,
                OidcClientSecret = !string.IsNullOrEmpty(dto.OidcClientSecret) ? "***" : "",
                OidcScopes = dto.OidcScopes,
                OidcAutoCreateUser = dto.OidcAutoCreateUser,
                OidcDefaultRole = dto.OidcDefaultRole,
                LdapEnabled = dto.LdapEnabled,
                LdapUrl = dto.LdapUrl,
                LdapBindDn = dto.LdapBindDn,
                LdapBindPassword = !string.IsNullOrEmpty(dto.LdapBindPassword) ? "***" : "",
                LdapBaseDn = dto.LdapBaseDn,
                LdapUserFilter = dto.LdapUserFilter
            };
            await auditService.EnqueueChangeAsync(context, "auth", oldValue, newValueForAudit);

            return Results.Ok(ApiResponse<bool>.Ok(true, "配置已保存"));
        });

        group.MapPost("/auth-config/test", (AuthConfigDto dto) =>
        {
            // TODO: 实现认证连接测试
            return Results.Ok(ApiResponse<bool>.Ok(true, "测试功能暂未实现"));
        });

        // ========== 系统信息 ==========

        group.MapGet("/info", async (SystemInfoService systemInfoService) =>
        {
            var (data, error) = await systemInfoService.GetSystemInfoAsync();

            if (error != null)
                return Results.BadRequest(ApiResponse<SystemInfoDto>.Fail(error));

            return Results.Ok(ApiResponse<SystemInfoDto>.Ok(data!));
        });

        group.MapGet("/stats", async (SystemInfoService systemInfoService) =>
        {
            var (data, error) = await systemInfoService.GetStatsAsync();

            if (error != null)
                return Results.BadRequest(ApiResponse<SystemStatsDto>.Fail(error));

            return Results.Ok(ApiResponse<SystemStatsDto>.Ok(data!));
        });

        // ========== 日志管理 ==========

        group.MapGet("/logs", async (
            LogService logService,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? level = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? searchText = null) =>
        {
            var request = new LogQueryRequest
            {
                Page = page,
                PageSize = pageSize,
                Level = level,
                StartDate = startDate,
                EndDate = endDate,
                SearchText = searchText
            };

            var (data, error) = await logService.GetLogsAsync(request);

            if (error != null)
                return Results.BadRequest(ApiResponse<PagedResponse<LogEntryDto>>.Fail(error));

            return Results.Ok(ApiResponse<PagedResponse<LogEntryDto>>.Ok(data!));
        });

        group.MapGet("/logs/export", async (
            LogService logService,
            [FromQuery] string? level = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null) =>
        {
            var request = new LogExportRequest
            {
                Level = level,
                StartDate = startDate,
                EndDate = endDate,
                Format = "txt"
            };

            var (data, error) = await logService.ExportLogsAsync(request);

            if (error != null)
                return Results.BadRequest(ApiResponse<byte[]>.Fail(error));

            return Results.File(data!, "text/plain", $"logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        });

        group.MapDelete("/logs/cleanup", async (
            LogService logService,
            [FromQuery] int daysToKeep = 30) =>
        {
            var (success, error) = await logService.CleanupOldLogsAsync(daysToKeep);

            if (error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error));

            return Results.Ok(ApiResponse<bool>.Ok(true, error ?? "清理完成"));
        });

        // ========== 缓存管理 ==========

        group.MapGet("/cache/stats", async (CacheService cacheService) =>
        {
            var stats = await cacheService.GetStatsAsync();
            return Results.Ok(ApiResponse<CacheStatsDto>.Ok(stats));
        });

        group.MapGet("/cache/types", async (CacheService cacheService) =>
        {
            var types = await cacheService.GetCacheTypesAsync();
            return Results.Ok(ApiResponse<List<CacheTypeDto>>.Ok(types));
        });

        group.MapGet("/cache/{type}/keys", async (
            CacheService cacheService,
            string type) =>
        {
            var keys = await cacheService.GetCacheKeysAsync(type);
            return Results.Ok(ApiResponse<List<CacheKeyDto>>.Ok(keys));
        });

        group.MapPost("/cache/clear-all", async (CacheService cacheService) =>
        {
            await cacheService.ClearAllCacheAsync();
            return Results.Ok(ApiResponse<bool>.Ok(true, "缓存已清空"));
        });

        group.MapPost("/cache/{type}/clear", async (
            CacheService cacheService,
            string type) =>
        {
            await cacheService.ClearCacheByTypeAsync(type);
            return Results.Ok(ApiResponse<bool>.Ok(true, $"{type} 缓存已清空"));
        });

        group.MapDelete("/cache/{type}/keys/{key}", async (
            CacheService cacheService,
            string type,
            string key) =>
        {
            await cacheService.RemoveKeyAsync(type, key);
            return Results.Ok(ApiResponse<bool>.Ok(true, "缓存键已删除"));
        });

        // ========== 作业管理 ==========

        group.MapGet("/jobs", async (JobSchedulerService jobService) =>
        {
            var (jobs, error) = await jobService.GetJobsAsync();

            if (error != null)
                return Results.BadRequest(ApiResponse<List<ScheduledJobDto>>.Fail(error));

            return Results.Ok(ApiResponse<List<ScheduledJobDto>>.Ok(jobs!));
        });

        group.MapGet("/jobs/stats", async (JobSchedulerService jobService) =>
        {
            var (stats, error) = await jobService.GetJobStatsAsync();

            if (error != null)
                return Results.BadRequest(ApiResponse<JobStatsDto>.Fail(error));

            return Results.Ok(ApiResponse<JobStatsDto>.Ok(stats!));
        });

        group.MapGet("/jobs/{id}", async (
            long id,
            JobSchedulerService jobService) =>
        {
            var (job, error) = await jobService.GetJobByIdAsync(id);

            if (error != null)
                return Results.NotFound(ApiResponse<ScheduledJobDto>.Fail(error));

            return Results.Ok(ApiResponse<ScheduledJobDto>.Ok(job!));
        });

        group.MapPost("/jobs/{id}/run", async (
            long id,
            JobSchedulerService jobService) =>
        {
            var (success, error) = await jobService.RunJobAsync(id);

            if (error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error));

            return Results.Ok(ApiResponse<bool>.Ok(true, "任务已启动"));
        });

        group.MapGet("/jobs/{id}/logs", async (
            long id,
            JobSchedulerService jobService) =>
        {
            var (executions, error) = await jobService.GetJobExecutionsAsync(id);

            if (error != null)
                return Results.BadRequest(ApiResponse<List<JobExecutionDto>>.Fail(error));

            return Results.Ok(ApiResponse<List<JobExecutionDto>>.Ok(executions!));
        });

        // ========== 备份管理 ==========

        group.MapGet("/backup/list", async (BackupService backupService) =>
        {
            var (backups, error) = await backupService.GetBackupsAsync();

            if (error != null)
                return Results.BadRequest(ApiResponse<List<BackupDto>>.Fail(error));

            return Results.Ok(ApiResponse<List<BackupDto>>.Ok(backups!));
        });

        group.MapPost("/backup", async (
            HttpContext context,
            CreateBackupRequest request,
            BackupService backupService) =>
        {
            var currentUser = context.Items["CurrentUser"] as CurrentUser;
            var (backup, error) = await backupService.CreateBackupAsync(request, currentUser?.UserId ?? 0);

            if (error != null)
                return Results.BadRequest(ApiResponse<BackupDto>.Fail(error));

            return Results.Ok(ApiResponse<BackupDto>.Ok(backup!, "备份任务已创建"));
        });

        group.MapGet("/backup/{id}/download", async (
            long id,
            BackupService backupService) =>
        {
            var (data, fileName, error) = await backupService.GetBackupFileAsync(id);

            if (error != null)
                return Results.BadRequest(ApiResponse<byte[]>.Fail(error));

            return Results.File(data!, "application/zip", fileName!);
        });

        group.MapPost("/backup/{id}/restore", async (
            long id,
            RestoreBackupRequest request,
            BackupService backupService) =>
        {
            var (success, error) = await backupService.RestoreBackupAsync(id, request);

            if (error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error));

            return Results.Ok(ApiResponse<bool>.Ok(true, "备份还原成功"));
        });

        group.MapDelete("/backup/{id}", async (
            long id,
            BackupService backupService) =>
        {
            var (success, error) = await backupService.DeleteBackupAsync(id);

            if (error != null)
                return Results.BadRequest(ApiResponse<bool>.Fail(error));

            return Results.Ok(ApiResponse<bool>.Ok(true, "备份已删除"));
        });

        // ========== 公开端点（无需认证） ==========

        app.MapGet("/api/siteinfo", (AppConfiguration appConfig, SetupService setupService) =>
        {
            var a = appConfig.AuthSettings;
            var dto = new SiteInfoDto
            {
                Installed = setupService.IsInstalled(),
                SiteName = appConfig.SiteSettings.SiteName,
                SiteLogo = appConfig.SiteSettings.SiteLogo,
                AllowRegistration = appConfig.SiteSettings.AllowRegistration,
                PasswordEnabled = a.PasswordEnabled,
                EmailLoginEnabled = a.EmailLoginEnabled,
                OidcEnabled = a.OidcEnabled,
                OidcProviderName = a.OidcProviderName,
                LdapEnabled = a.LdapEnabled
            };
            return Results.Ok(ApiResponse<SiteInfoDto>.Ok(dto));
        });
    }

    /// <summary>
    /// 保存 Gotenberg 配置到 appsettings.runtime.json（AOT 兼容）
    /// </summary>
    private static void SaveGotenbergConfig(IHostEnvironment env, OfficePreviewConfigDto dto)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        // 读取现有配置
        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        // 确保 App.Gotenberg 路径存在
        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        var gotenbergNode = new JsonObject
        {
            ["Enabled"] = dto.Enabled,
            ["BaseUrl"] = dto.BaseUrl,
            ["TimeoutSeconds"] = dto.TimeoutSeconds
        };

        appNode!["Gotenberg"] = gotenbergNode;
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    private static void SaveSiteSettingsConfig(IHostEnvironment env, SiteConfigDto dto)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        appNode!["SiteSettings"] = new JsonObject
        {
            ["SiteName"] = dto.SiteName,
            ["SiteDescription"] = dto.SiteDescription,
            ["SiteLogo"] = dto.SiteLogo,
            ["SiteDomain"] = dto.SiteDomain,
            ["DefaultLanguage"] = dto.DefaultLanguage,
            ["DefaultHomePage"] = dto.DefaultHomePage,
            ["Timezone"] = dto.Timezone,
            ["AllowRegistration"] = dto.AllowRegistration
        };
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    private static void SaveDisplaySettingsConfig(IHostEnvironment env, DisplayConfigDto dto)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        appNode!["DisplaySettings"] = new JsonObject
        {
            ["DefaultTheme"] = dto.DefaultTheme,
            ["PrimaryColor"] = dto.PrimaryColor,
            ["CompactMode"] = dto.CompactMode,
            ["PageSize"] = dto.PageSize,
            ["PageTreeExpandMode"] = dto.PageTreeExpandMode,
            ["ShowPageViews"] = dto.ShowPageViews,
            ["ShowAuthorInfo"] = dto.ShowAuthorInfo,
            ["ShowLastModified"] = dto.ShowLastModified,
            ["DefaultEditorMode"] = dto.DefaultEditorMode,
            ["AutoSaveInterval"] = dto.AutoSaveInterval,
            ["EnableSpellCheck"] = dto.EnableSpellCheck,
            ["DefaultSidebarWidth"] = dto.DefaultSidebarWidth,
            ["ShowSpaceIcon"] = dto.ShowSpaceIcon,
            ["AllowCollapseSidebar"] = dto.AllowCollapseSidebar
        };
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    private static void SaveSecuritySettingsConfig(IHostEnvironment env, SecurityConfigDto dto)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        appNode!["SecuritySettings"] = new JsonObject
        {
            ["AllowPublicRegistration"] = dto.AllowPublicRegistration,
            ["RequireEmailVerification"] = dto.RequireEmailVerification,
            ["DefaultUserRole"] = dto.DefaultUserRole,
            ["MinPasswordLength"] = dto.MinPasswordLength,
            ["PasswordComplexity"] = dto.PasswordComplexity,
            ["PasswordExpireDays"] = dto.PasswordExpireDays,
            ["SessionTimeout"] = dto.SessionTimeout,
            ["AllowConcurrentSessions"] = dto.AllowConcurrentSessions,
            ["AllowRememberMe"] = dto.AllowRememberMe,
            ["IpWhitelist"] = dto.IpWhitelist,
            ["EnableTwoFactor"] = dto.EnableTwoFactor
        };
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    private static void SaveMailSettingsConfig(IHostEnvironment env, MailConfigDto dto, string savedPassword)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        appNode!["MailSettings"] = new JsonObject
        {
            ["Enabled"] = dto.Enabled,
            ["SmtpHost"] = dto.SmtpHost,
            ["SmtpPort"] = dto.SmtpPort,
            ["Encryption"] = dto.Encryption,
            ["FromEmail"] = dto.FromEmail,
            ["FromName"] = dto.FromName,
            ["Username"] = dto.Username,
            ["Password"] = !string.IsNullOrEmpty(dto.Password) ? dto.Password : savedPassword,
            ["NotifyOnRegister"] = dto.NotifyOnRegister,
            ["AdminEmail"] = dto.AdminEmail,
            ["EmailSignature"] = dto.EmailSignature
        };
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }

    private static void SaveAuthSettingsConfig(IHostEnvironment env, AuthConfigDto dto, string savedOidcSecret, string savedLdapPassword)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        if (!Directory.Exists(dataDir))
            Directory.CreateDirectory(dataDir);

        var configPath = Path.Combine(dataDir, "appsettings.runtime.json");

        JsonObject? root;
        if (File.Exists(configPath))
        {
            var existingJson = File.ReadAllText(configPath);
            root = JsonNode.Parse(existingJson)?.AsObject();
        }
        else
        {
            root = new JsonObject();
        }

        var appNode = root!.TryGetPropertyValue("App", out var appVal)
            ? appVal?.AsObject() ?? new JsonObject()
            : new JsonObject();

        appNode!["AuthSettings"] = new JsonObject
        {
            ["PasswordEnabled"] = dto.PasswordEnabled,
            ["EmailLoginEnabled"] = dto.EmailLoginEnabled,
            ["OidcEnabled"] = dto.OidcEnabled,
            ["OidcProviderName"] = dto.OidcProviderName,
            ["OidcDiscoveryUrl"] = dto.OidcDiscoveryUrl,
            ["OidcClientId"] = dto.OidcClientId,
            ["OidcClientSecret"] = !string.IsNullOrEmpty(dto.OidcClientSecret) ? dto.OidcClientSecret : savedOidcSecret,
            ["OidcScopes"] = dto.OidcScopes,
            ["OidcAutoCreateUser"] = dto.OidcAutoCreateUser,
            ["OidcDefaultRole"] = dto.OidcDefaultRole,
            ["LdapEnabled"] = dto.LdapEnabled,
            ["LdapUrl"] = dto.LdapUrl,
            ["LdapBindDn"] = dto.LdapBindDn,
            ["LdapBindPassword"] = !string.IsNullOrEmpty(dto.LdapBindPassword) ? dto.LdapBindPassword : savedLdapPassword,
            ["LdapBaseDn"] = dto.LdapBaseDn,
            ["LdapUserFilter"] = dto.LdapUserFilter
        };
        root!["App"] = appNode;

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(configPath, root.ToJsonString(options));
    }
}
