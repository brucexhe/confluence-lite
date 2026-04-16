using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using ConfluenceLite.Api.Data;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Mappers;

namespace ConfluenceLite.Api.Routes;

/// <summary>
/// 活动记录路由
/// </summary>
public static class ActivityRoutes
{
    public static void MapActivityRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/activity")
            .WithTags("Activity");

        // 获取最近活动列表
        group.MapGet("/recent", async (
            [FromServices] AppDbContext db,
            [FromServices] ILogger<Program> logger,
            [FromQuery] long? workspaceId,
            [FromQuery] string? type,
            [FromQuery] int count = 20,
            [FromQuery] int offset = 0) =>
        {
            try
            {
                logger.LogInformation("GetRecentActivities called with workspaceId: {WorkspaceId}, count: {Count}", workspaceId, count);

                // 限制返回数量
                if (count < 1) count = 20;
                if (count > 100) count = 100;

                var activityDtos = new List<ActivityDto>();

                // 获取最近更新的页面
                var pageQuery = db.Db.Queryable<ConfluenceLite.Api.Models.Page>()
                    .LeftJoin<ConfluenceLite.Api.Models.Workspace>((p, w) => p.WorkspaceId == w.Id)
                    .LeftJoin<ConfluenceLite.Api.Models.User>((p, w, c) => p.CreatorId == c.Id)
                    .Where((p, w, c) => !p.IsDeleted);

                if (workspaceId.HasValue)
                {
                    pageQuery = pageQuery.Where((p, w, c) => p.WorkspaceId == workspaceId.Value);
                }

                var pages = await pageQuery
                    .OrderByDescending(p => p.UpdatedAt)
                    .Select((p, w, c) => new ActivityDto
                    {
                        Id = p.Id,
                        Type = "page_updated",
                        PageTitle = p.Title,
                        PageId = p.Id,
                        CreatedAt = p.UpdatedAt,
                        WorkspaceId = w.Id,
                        WorkspaceName = w.Name,
                        WorkspaceKey = w.Key,
                        User = new UserSummaryDto
                        {
                            Id = c.Id,
                            DisplayName = c.DisplayName,
                            Username = c.Username
                        }
                    })
                    .Take(count)
                    .ToListAsync();

                activityDtos.AddRange(pages);

                // 获取最近的评论（PageComment 没有 IsDeleted 字段）
                var commentQuery = db.Db.Queryable<ConfluenceLite.Api.Models.PageComment>()
                    .LeftJoin<ConfluenceLite.Api.Models.Page>((c, p) => c.PageId == p.Id)
                    .LeftJoin<ConfluenceLite.Api.Models.Workspace>((c, p, w) => p.WorkspaceId == w.Id)
                    .LeftJoin<ConfluenceLite.Api.Models.User>((c, p, w, u) => c.UserId == u.Id)
                    .Where((c, p, w, u) => !p.IsDeleted); // 只检查页面的 IsDeleted

                if (workspaceId.HasValue)
                {
                    commentQuery = commentQuery.Where((c, p, w, u) => p.WorkspaceId == workspaceId.Value);
                }

                var comments = await commentQuery
                    .OrderByDescending(c => c.CreatedAt)
                    .Select((c, p, w, u) => new ActivityDto
                    {
                        Id = c.Id,
                        Type = "comment_added",
                        PageTitle = p.Title,
                        PageId = p.Id, // 页面ID，不是评论ID
                        CreatedAt = c.CreatedAt,
                        WorkspaceId = w.Id,
                        WorkspaceName = w.Name,
                        WorkspaceKey = w.Key,
                        User = new UserSummaryDto
                        {
                            Id = u.Id,
                            DisplayName = u.DisplayName,
                            Username = u.Username
                        }
                    })
                    .Take(count)
                    .ToListAsync();

                activityDtos.AddRange(comments);

                // 设置描述并排序
                foreach (var activity in activityDtos)
                {
                    activity.Description = GetActivityDescription(activity.Type, activity.PageTitle);
                }

                // 合并、排序并分页
                var sortedActivities = activityDtos
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip(offset)
                    .Take(count)
                    .ToList();

                logger.LogInformation("GetRecentActivities returning {Count} activities", sortedActivities.Count);
                return Results.Ok(sortedActivities);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in GetRecentActivities");
                return Results.Problem($"Error: {ex.Message}", statusCode: 500);
            }
        })
        .WithName("GetRecentActivities");
    }

    private static string GetActivityDescription(string type, string? pageTitle)
    {
        return type switch
        {
            "page_updated" => $"updated page \"{pageTitle}\"",
            "page_created" => $"created page \"{pageTitle}\"",
            "page_deleted" => $"deleted page \"{pageTitle}\"",
            "comment_added" => $"commented on \"{pageTitle}\"",
            _ => "performed an action"
        };
    }
}
