using SqlSugar;
using System.Text.Json.Serialization;

namespace ConfluenceLite.Api.Models;

/// <summary>
/// 导入任务实体
/// </summary>
[SugarTable("import_tasks")]
public class ImportTask
{
    /// <summary>
    /// 任务ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 导入任务名称
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = false)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 源文件路径
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = false)]
    public string SourceFile { get; set; } = string.Empty;

    /// <summary>
    /// 任务状态: pending, processing, completed, failed, cancelled
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 导入选项（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "json")]
    public string? Options { get; set; }

    /// <summary>
    /// 进度信息（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "json")]
    public string? Progress { get; set; }

    /// <summary>
    /// 错误消息
    /// </summary>
    [SugarColumn(ColumnDataType = "text")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// 创建者用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long CreatedById { get; set; }

    /// <summary>
    /// 导航属性 - 创建者
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public User? CreatedBy { get; set; }
}

/// <summary>
/// 导入进度信息
/// </summary>
public class ImportProgress
{
    /// <summary>
    /// 总项目数
    /// </summary>
    [JsonPropertyName("totalItems")]
    public int TotalItems { get; set; }

    /// <summary>
    /// 已处理项目数
    /// </summary>
    [JsonPropertyName("processedItems")]
    public int ProcessedItems { get; set; }

    /// <summary>
    /// 失败项目数
    /// </summary>
    [JsonPropertyName("failedItems")]
    public int FailedItems { get; set; }

    /// <summary>
    /// 当前步骤描述
    /// </summary>
    [JsonPropertyName("currentStep")]
    public string CurrentStep { get; set; } = string.Empty;

    /// <summary>
    /// 各实体类型计数
    /// </summary>
    [JsonPropertyName("entityCounts")]
    public Dictionary<string, int> EntityCounts { get; set; } = new();

    /// <summary>
    /// 进度百分比
    /// </summary>
    [JsonIgnore]
    public int ProgressPercent => TotalItems > 0 ? (int)((ProcessedItems / (double)TotalItems) * 100) : 0;
}

/// <summary>
/// 导入选项
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// 导入用户数据
    /// </summary>
    [JsonPropertyName("importUsers")]
    public bool ImportUsers { get; set; } = true;

    /// <summary>
    /// 导入空间数据
    /// </summary>
    [JsonPropertyName("importSpaces")]
    public bool ImportSpaces { get; set; } = true;

    /// <summary>
    /// 导入页面数据
    /// </summary>
    [JsonPropertyName("importPages")]
    public bool ImportPages { get; set; } = true;

    /// <summary>
    /// 导入附件数据
    /// </summary>
    [JsonPropertyName("importAttachments")]
    public bool ImportAttachments { get; set; } = true;

    /// <summary>
    /// 导入评论数据
    /// </summary>
    [JsonPropertyName("importComments")]
    public bool ImportComments { get; set; } = true;

    /// <summary>
    /// 覆盖现有数据
    /// </summary>
    [JsonPropertyName("overwriteExisting")]
    public bool OverwriteExisting { get; set; } = true;
}
