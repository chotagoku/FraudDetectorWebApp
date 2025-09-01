using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.DTOs
{
    public class ConfigurationResponseDto
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; }
        public bool RequiresRestart { get; set; }
        public bool IsAdvanced { get; set; }
        public object? ValidationRules { get; set; } // Parsed JSON
        public string? DefaultValue { get; set; }
        public string[]? AllowedValues { get; set; } // Parsed JSON array
        public string Section { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class ConfigurationUpdateDto
    {
        [Required]
        public string Value { get; set; } = string.Empty;
        
        public string? UpdatedBy { get; set; }
    }

    public class ConfigurationCreateDto
    {
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DataType { get; set; } = "string";

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public bool IsReadOnly { get; set; } = false;
        public bool RequiresRestart { get; set; } = false;
        public bool IsAdvanced { get; set; } = false;
        public string? ValidationRules { get; set; }
        public string? DefaultValue { get; set; }
        public string? AllowedValues { get; set; }
        
        [StringLength(50)]
        public string Section { get; set; } = "General";
        
        public int DisplayOrder { get; set; } = 0;
        public string? CreatedBy { get; set; }
    }

    public class ConfigurationBulkUpdateDto
    {
        [Required]
        public Dictionary<string, string> Configurations { get; set; } = new();
        
        public string? UpdatedBy { get; set; }
        
        public bool ApplyImmediately { get; set; } = true;
    }

    public class ConfigurationSectionDto
    {
        public string SectionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ConfigurationResponseDto> Configurations { get; set; } = new();
        public int TotalConfigurations { get; set; }
        public int ReadOnlyConfigurations { get; set; }
        public int RequireRestartConfigurations { get; set; }
    }

    public class ConfigurationSearchDto
    {
        public string? SearchTerm { get; set; }
        public string? Category { get; set; }
        public string? Section { get; set; }
        public bool? IsAdvanced { get; set; }
        public bool? RequiresRestart { get; set; }
        public bool? IsReadOnly { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "DisplayOrder";
        public string SortDirection { get; set; } = "asc";
    }

    public class ConfigurationValidationDto
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public object? ValidationRules { get; set; }
        public string[]? AllowedValues { get; set; }
    }

    public class ConfigurationValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public object? ParsedValue { get; set; }
    }

    public class ConfigurationExportDto
    {
        public string Format { get; set; } = "json"; // json, xml, yaml, csv
        public string[]? Categories { get; set; }
        public string[]? Sections { get; set; }
        public bool IncludeAdvanced { get; set; } = true;
        public bool IncludeReadOnly { get; set; } = true;
        public bool IncludeMetadata { get; set; } = true;
    }

    public class ConfigurationImportDto
    {
        [Required]
        public string Data { get; set; } = string.Empty;
        
        public string Format { get; set; } = "json";
        public bool OverwriteExisting { get; set; } = false;
        public bool ValidateOnly { get; set; } = false;
        public string? ImportedBy { get; set; }
    }

    public class ConfigurationImportResultDto
    {
        public bool Success { get; set; }
        public int TotalConfigurations { get; set; }
        public int ImportedConfigurations { get; set; }
        public int SkippedConfigurations { get; set; }
        public int FailedConfigurations { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<ConfigurationResponseDto> ImportedItems { get; set; } = new();
    }

    public class SystemHealthDto
    {
        public bool IsHealthy { get; set; }
        public Dictionary<string, object> SystemInfo { get; set; } = new();
        public Dictionary<string, bool> ServiceStatus { get; set; } = new();
        public Dictionary<string, string> ConfigurationSummary { get; set; } = new();
        public List<string> HealthChecks { get; set; } = new();
        public DateTime CheckedAt { get; set; }
    }

    public class ConfigurationTemplateDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, ConfigurationCreateDto> Configurations { get; set; } = new();
        public string[]? Prerequisites { get; set; }
        public string[]? PostInstallSteps { get; set; }
    }

    // Built-in configuration templates
    public static class ConfigurationTemplates
    {
        public const string Development = "development";
        public const string Production = "production";
        public const string Testing = "testing";
        public const string HighPerformance = "high_performance";
        public const string HighSecurity = "high_security";
        public const string Minimal = "minimal";
    }

    // Configuration data types
    public static class ConfigurationDataTypes
    {
        public const string String = "string";
        public const string Integer = "int";
        public const string Boolean = "bool";
        public const string Decimal = "decimal";
        public const string TimeSpan = "timespan";
        public const string DateTime = "datetime";
        public const string Json = "json";
        public const string Array = "array";
    }
}
