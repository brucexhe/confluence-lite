using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using ConfluenceLite.Api.DTOs;
using ConfluenceLite.Api.Attributes;
using ConfluenceLite.Api.Mappers;

namespace ConfluenceLite.Api.Services;

/// <summary>
/// 审计日志 Diff 生成器实现 - AOT 兼容
/// </summary>
public class AuditDiffGenerator : IAuditDiffGenerator
{
    private readonly JsonSerializerOptions _jsonOptions;

    public AuditDiffGenerator()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            // ChangeDetail.OldValue/NewValue 现在是 string?，source generator 可以正确处理
            TypeInfoResolver = AppJsonContext.Default
        };
    }

    public string GenerateDiff(object? oldValue, object? newValue, string? requestPath = null, string? requestMethod = null)
    {
        var details = new AuditChangeDetails
        {
            RequestPath = requestPath,
            RequestMethod = requestMethod,
            Changes = new Dictionary<string, ChangeDetail>()
        };

        if (oldValue == null && newValue == null)
        {
            return JsonSerializer.Serialize(details, _jsonOptions);
        }

        // Get type for reflection
        var type = newValue?.GetType() ?? oldValue?.GetType();
        if (type == null)
        {
            return JsonSerializer.Serialize(details, _jsonOptions);
        }

        // Get all properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Skip indexer properties
            if (property.GetIndexParameters().Length > 0)
                continue;

            var oldVal = oldValue == null ? null : property.GetValue(oldValue);
            var newVal = newValue == null ? null : property.GetValue(newValue);

            // Check if value changed
            if (!AreValuesEqual(oldVal, newVal))
            {
                var propertyName = GetPropertyName(property);
                var changeDetail = new ChangeDetail
                {
                    OldValue = SanitizeValue(property, oldVal),
                    NewValue = SanitizeValue(property, newVal)
                };

                details.Changes![propertyName] = changeDetail;
            }
        }

        return JsonSerializer.Serialize(details, _jsonOptions);
    }

    public string GenerateDiff<T>(T? oldValue, T? newValue, string? requestPath = null, string? requestMethod = null)
    {
        return GenerateDiff((object?)oldValue, (object?)newValue, requestPath, requestMethod);
    }

    private static bool AreValuesEqual(object? oldValue, object? newValue)
    {
        if (oldValue == null && newValue == null)
            return true;

        if (oldValue == null || newValue == null)
            return false;

        // Handle collections differently if needed
        if (oldValue is System.Collections.IEnumerable oldEnum && newValue is System.Collections.IEnumerable newEnum)
        {
            // For simplicity, compare as strings
            return string.Equals(oldValue.ToString(), newValue.ToString());
        }

        return Equals(oldValue, newValue);
    }

    private static string GetPropertyName(PropertyInfo property)
    {
        // Use JsonPropertyName attribute if present
        var jsonNameAttr = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        if (!string.IsNullOrEmpty(jsonNameAttr?.Name))
        {
            return jsonNameAttr.Name;
        }

        // Otherwise use property name with camelCase
        return char.ToLower(property.Name[0]) + property.Name.Substring(1);
    }

    private static string? SanitizeValue(PropertyInfo property, object? value)
    {
        if (value == null)
            return null;

        // Check for SensitiveData attribute
        var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>();
        if (sensitiveAttr != null)
        {
            return sensitiveAttr.Mask;
        }

        // Check for property names that commonly contain sensitive data
        var propertyName = property.Name.ToLower();
        if (propertyName.Contains("password") ||
            propertyName.Contains("secret") ||
            propertyName.Contains("token") ||
            (propertyName.Contains("key") && propertyName.Contains("api")))
        {
            return "***";
        }

        // 统一转换为字符串，避免反射序列化 object?
        return value.ToString();
    }
}
