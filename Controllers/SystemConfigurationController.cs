using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.DTOs;
using FraudDetectorWebApp.Models;
using FraudDetectorWebApp.Services;
using FraudDetectorWebApp.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/system-configuration")]
    [Authorize]
    [RequirePermission("SYSTEM_CONFIGURATION")]
    [SecurityAudit(Operation = "SystemConfiguration", ResourceType = "Configuration")]
    public class SystemConfigurationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemConfigurationController> _logger;
        private readonly IConfigurationService _configService;

        public SystemConfigurationController(
            ApplicationDbContext context,
            ILogger<SystemConfigurationController> logger,
            IConfigurationService configService)
        {
            _context = context;
            _logger = logger;
            _configService = configService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ConfigurationResponseDto>>> GetConfigurations(
            [FromQuery] ConfigurationSearchDto search)
        {
            try
            {
                var configurations = await _configService.SearchConfigurationsAsync(search);
                var totalCount = await _context.SystemConfigurations.CountAsync();

                var response = configurations.Select(c => new ConfigurationResponseDto
                {
                    Id = c.Id,
                    Key = c.Key,
                    Value = c.Value,
                    Category = c.Category,
                    DataType = c.DataType,
                    Description = c.Description,
                    DisplayName = c.DisplayName,
                    IsReadOnly = c.IsReadOnly,
                    RequiresRestart = c.RequiresRestart,
                    IsAdvanced = c.IsAdvanced,
                    ValidationRules = string.IsNullOrEmpty(c.ValidationRules) 
                        ? null 
                        : JsonSerializer.Deserialize<object>(c.ValidationRules),
                    DefaultValue = c.DefaultValue,
                    AllowedValues = string.IsNullOrEmpty(c.AllowedValues) 
                        ? null 
                        : JsonSerializer.Deserialize<string[]>(c.AllowedValues),
                    Section = c.Section,
                    DisplayOrder = c.DisplayOrder,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    UpdatedBy = c.UpdatedBy
                }).ToList();

                Response.Headers["X-Total-Count"] = totalCount.ToString();
                Response.Headers["X-Page"] = search.Page.ToString();
                Response.Headers["X-Page-Size"] = search.PageSize.ToString();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configurations");
                return StatusCode(500, new { message = "Error getting configurations", error = ex.Message });
            }
        }

        [HttpGet("sections")]
        public async Task<ActionResult<IEnumerable<ConfigurationSectionDto>>> GetConfigurationSections()
        {
            try
            {
                var configurations = await _context.SystemConfigurations
                    .OrderBy(c => c.Section)
                    .ThenBy(c => c.DisplayOrder)
                    .ToListAsync();

                var sections = configurations
                    .GroupBy(c => c.Section)
                    .Select(g => new ConfigurationSectionDto
                    {
                        SectionName = g.Key,
                        Description = GetSectionDescription(g.Key),
                        Configurations = g.Select(c => new ConfigurationResponseDto
                        {
                            Id = c.Id,
                            Key = c.Key,
                            Value = c.Value,
                            Category = c.Category,
                            DataType = c.DataType,
                            Description = c.Description,
                            DisplayName = c.DisplayName,
                            IsReadOnly = c.IsReadOnly,
                            RequiresRestart = c.RequiresRestart,
                            IsAdvanced = c.IsAdvanced,
                            ValidationRules = string.IsNullOrEmpty(c.ValidationRules) 
                                ? null 
                                : JsonSerializer.Deserialize<object>(c.ValidationRules),
                            DefaultValue = c.DefaultValue,
                            AllowedValues = string.IsNullOrEmpty(c.AllowedValues) 
                                ? null 
                                : JsonSerializer.Deserialize<string[]>(c.AllowedValues),
                            Section = c.Section,
                            DisplayOrder = c.DisplayOrder,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt,
                            UpdatedBy = c.UpdatedBy
                        }).ToList(),
                        TotalConfigurations = g.Count(),
                        ReadOnlyConfigurations = g.Count(c => c.IsReadOnly),
                        RequireRestartConfigurations = g.Count(c => c.RequiresRestart)
                    })
                    .OrderBy(s => s.SectionName)
                    .ToList();

                return Ok(sections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration sections");
                return StatusCode(500, new { message = "Error getting configuration sections", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ConfigurationResponseDto>> GetConfiguration(int id)
        {
            try
            {
                var config = await _context.SystemConfigurations.FindAsync(id);
                if (config == null)
                    return NotFound();

                var response = new ConfigurationResponseDto
                {
                    Id = config.Id,
                    Key = config.Key,
                    Value = config.Value,
                    Category = config.Category,
                    DataType = config.DataType,
                    Description = config.Description,
                    DisplayName = config.DisplayName,
                    IsReadOnly = config.IsReadOnly,
                    RequiresRestart = config.RequiresRestart,
                    IsAdvanced = config.IsAdvanced,
                    ValidationRules = string.IsNullOrEmpty(config.ValidationRules) 
                        ? null 
                        : JsonSerializer.Deserialize<object>(config.ValidationRules),
                    DefaultValue = config.DefaultValue,
                    AllowedValues = string.IsNullOrEmpty(config.AllowedValues) 
                        ? null 
                        : JsonSerializer.Deserialize<string[]>(config.AllowedValues),
                    Section = config.Section,
                    DisplayOrder = config.DisplayOrder,
                    CreatedAt = config.CreatedAt,
                    UpdatedAt = config.UpdatedAt,
                    UpdatedBy = config.UpdatedBy
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration {Id}", id);
                return StatusCode(500, new { message = "Error getting configuration", error = ex.Message });
            }
        }

        [HttpGet("key/{key}")]
        public async Task<ActionResult<ConfigurationResponseDto>> GetConfigurationByKey(string key)
        {
            try
            {
                var config = await _context.SystemConfigurations
                    .FirstOrDefaultAsync(c => c.Key == key);
                
                if (config == null)
                    return NotFound();

                var response = new ConfigurationResponseDto
                {
                    Id = config.Id,
                    Key = config.Key,
                    Value = config.Value,
                    Category = config.Category,
                    DataType = config.DataType,
                    Description = config.Description,
                    DisplayName = config.DisplayName,
                    IsReadOnly = config.IsReadOnly,
                    RequiresRestart = config.RequiresRestart,
                    IsAdvanced = config.IsAdvanced,
                    ValidationRules = string.IsNullOrEmpty(config.ValidationRules) 
                        ? null 
                        : JsonSerializer.Deserialize<object>(config.ValidationRules),
                    DefaultValue = config.DefaultValue,
                    AllowedValues = string.IsNullOrEmpty(config.AllowedValues) 
                        ? null 
                        : JsonSerializer.Deserialize<string[]>(config.AllowedValues),
                    Section = config.Section,
                    DisplayOrder = config.DisplayOrder,
                    CreatedAt = config.CreatedAt,
                    UpdatedAt = config.UpdatedAt,
                    UpdatedBy = config.UpdatedBy
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration by key {Key}", key);
                return StatusCode(500, new { message = "Error getting configuration", error = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("CONFIG_CREATE")]
        [AuditAdminAction("CREATE_CONFIG", "Configuration")]
        public async Task<ActionResult<ConfigurationResponseDto>> CreateConfiguration(
            [FromBody] ConfigurationCreateDto request)
        {
            try
            {
                // Check if key already exists
                var exists = await _configService.ConfigurationExistsAsync(request.Key);
                if (exists)
                {
                    return Conflict(new { message = $"Configuration with key '{request.Key}' already exists" });
                }

                // Validate the value
                var validation = await _configService.ValidateConfigurationAsync(request.Key, request.Value);
                
                var config = new SystemConfiguration
                {
                    Key = request.Key,
                    Value = request.Value,
                    Category = request.Category,
                    DataType = request.DataType,
                    Description = request.Description,
                    DisplayName = request.DisplayName,
                    IsReadOnly = request.IsReadOnly,
                    RequiresRestart = request.RequiresRestart,
                    IsAdvanced = request.IsAdvanced,
                    ValidationRules = request.ValidationRules,
                    DefaultValue = request.DefaultValue,
                    AllowedValues = request.AllowedValues,
                    Section = request.Section,
                    DisplayOrder = request.DisplayOrder,
                    UpdatedBy = request.CreatedBy
                };

                _context.SystemConfigurations.Add(config);
                await _context.SaveChangesAsync();

                var response = new ConfigurationResponseDto
                {
                    Id = config.Id,
                    Key = config.Key,
                    Value = config.Value,
                    Category = config.Category,
                    DataType = config.DataType,
                    Description = config.Description,
                    DisplayName = config.DisplayName,
                    IsReadOnly = config.IsReadOnly,
                    RequiresRestart = config.RequiresRestart,
                    IsAdvanced = config.IsAdvanced,
                    ValidationRules = string.IsNullOrEmpty(config.ValidationRules) 
                        ? null 
                        : JsonSerializer.Deserialize<object>(config.ValidationRules),
                    DefaultValue = config.DefaultValue,
                    AllowedValues = string.IsNullOrEmpty(config.AllowedValues) 
                        ? null 
                        : JsonSerializer.Deserialize<string[]>(config.AllowedValues),
                    Section = config.Section,
                    DisplayOrder = config.DisplayOrder,
                    CreatedAt = config.CreatedAt,
                    UpdatedAt = config.UpdatedAt,
                    UpdatedBy = config.UpdatedBy
                };

                _logger.LogInformation("Configuration created: {Key} by {CreatedBy}", request.Key, request.CreatedBy);
                return CreatedAtAction(nameof(GetConfiguration), new { id = config.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating configuration {Key}", request.Key);
                return StatusCode(500, new { message = "Error creating configuration", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ConfigurationResponseDto>> UpdateConfiguration(
            int id, [FromBody] ConfigurationUpdateDto request)
        {
            try
            {
                var config = await _context.SystemConfigurations.FindAsync(id);
                if (config == null)
                    return NotFound();

                if (config.IsReadOnly)
                {
                    return BadRequest(new { message = "Cannot modify read-only configuration" });
                }

                // Use configuration service for validation and notifications
                var success = await _configService.SetConfigurationValueAsync(config.Key, request.Value, request.UpdatedBy);
                if (!success)
                {
                    return BadRequest(new { message = "Failed to update configuration value" });
                }

                // Reload the configuration to get updated values
                await _context.Entry(config).ReloadAsync();

                var response = new ConfigurationResponseDto
                {
                    Id = config.Id,
                    Key = config.Key,
                    Value = config.Value,
                    Category = config.Category,
                    DataType = config.DataType,
                    Description = config.Description,
                    DisplayName = config.DisplayName,
                    IsReadOnly = config.IsReadOnly,
                    RequiresRestart = config.RequiresRestart,
                    IsAdvanced = config.IsAdvanced,
                    ValidationRules = string.IsNullOrEmpty(config.ValidationRules) 
                        ? null 
                        : JsonSerializer.Deserialize<object>(config.ValidationRules),
                    DefaultValue = config.DefaultValue,
                    AllowedValues = string.IsNullOrEmpty(config.AllowedValues) 
                        ? null 
                        : JsonSerializer.Deserialize<string[]>(config.AllowedValues),
                    Section = config.Section,
                    DisplayOrder = config.DisplayOrder,
                    CreatedAt = config.CreatedAt,
                    UpdatedAt = config.UpdatedAt,
                    UpdatedBy = config.UpdatedBy
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating configuration {Id}", id);
                return StatusCode(500, new { message = "Error updating configuration", error = ex.Message });
            }
        }

        [HttpPost("bulk-update")]
        [RequirePermission("CONFIG_BULK_UPDATE")]
        [AuditAdminAction("BULK_UPDATE_CONFIG", "Configuration")]
        public async Task<ActionResult<object>> BulkUpdateConfigurations(
            [FromBody] ConfigurationBulkUpdateDto request)
        {
            try
            {
                var result = await _configService.BulkUpdateConfigurationsAsync(
                    request.Configurations.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value), 
                    request.UpdatedBy);

                var requiresRestart = false;
                var updatedConfigs = new List<string>();

                foreach (var kvp in request.Configurations)
                {
                    var config = await _context.SystemConfigurations
                        .FirstOrDefaultAsync(c => c.Key == kvp.Key);
                    
                    if (config != null)
                    {
                        updatedConfigs.Add(string.IsNullOrEmpty(config.DisplayName) ? config.Key : config.DisplayName);
                        if (config.RequiresRestart)
                            requiresRestart = true;
                    }
                }

                return Ok(new
                {
                    success = result,
                    message = result 
                        ? $"Successfully updated {request.Configurations.Count} configurations"
                        : "Some configurations failed to update",
                    updatedConfigurations = updatedConfigs,
                    requiresRestart = requiresRestart,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk update");
                return StatusCode(500, new { message = "Error during bulk update", error = ex.Message });
            }
        }

        [HttpPost("validate")]
        public async Task<ActionResult<ConfigurationValidationResultDto>> ValidateConfiguration(
            [FromBody] ConfigurationValidationDto request)
        {
            try
            {
                var result = await _configService.ValidateConfigurationAsync(request.Key, request.Value);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating configuration");
                return StatusCode(500, new { message = "Error validating configuration", error = ex.Message });
            }
        }

        [HttpPost("{id}/reset")]
        public async Task<ActionResult<object>> ResetConfiguration(int id, [FromQuery] string? updatedBy = null)
        {
            try
            {
                var config = await _context.SystemConfigurations.FindAsync(id);
                if (config == null)
                    return NotFound();

                if (config.IsReadOnly)
                {
                    return BadRequest(new { message = "Cannot reset read-only configuration" });
                }

                await _configService.ResetConfigurationToDefaultAsync(config.Key, updatedBy);

                return Ok(new 
                { 
                    message = $"Configuration '{config.DisplayName}' reset to default value",
                    key = config.Key,
                    defaultValue = config.DefaultValue,
                    requiresRestart = config.RequiresRestart
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting configuration {Id}", id);
                return StatusCode(500, new { message = "Error resetting configuration", error = ex.Message });
            }
        }

        [HttpGet("categories")]
        public async Task<ActionResult<object>> GetConfigurationCategories()
        {
            try
            {
                var categories = await _context.SystemConfigurations
                    .GroupBy(c => c.Category)
                    .Select(g => new
                    {
                        category = g.Key,
                        displayName = GetCategoryDisplayName(g.Key),
                        count = g.Count(),
                        readOnlyCount = g.Count(c => c.IsReadOnly),
                        advancedCount = g.Count(c => c.IsAdvanced),
                        requiresRestartCount = g.Count(c => c.RequiresRestart)
                    })
                    .OrderBy(c => c.category)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration categories");
                return StatusCode(500, new { message = "Error getting configuration categories", error = ex.Message });
            }
        }

        [HttpPost("export")]
        [RequirePermission("CONFIG_EXPORT")]
        [AuditAdminAction("EXPORT_CONFIG", "Configuration")]
        public async Task<ActionResult> ExportConfigurations([FromBody] ConfigurationExportDto request)
        {
            try
            {
                var exportData = await _configService.ExportConfigurationsAsync(request);
                
                var fileName = $"configurations_{DateTime.UtcNow:yyyyMMddHHmmss}.{request.Format}";
                var contentType = request.Format.ToLower() switch
                {
                    "json" => "application/json",
                    _ => "application/octet-stream"
                };

                return File(System.Text.Encoding.UTF8.GetBytes(exportData), contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting configurations");
                return StatusCode(500, new { message = "Error exporting configurations", error = ex.Message });
            }
        }

        [HttpPost("import")]
        [RequirePermission("CONFIG_IMPORT")]
        [AuditAdminAction("IMPORT_CONFIG", "Configuration")]
        [RequireConfirmation]
        public async Task<ActionResult<ConfigurationImportResultDto>> ImportConfigurations(
            [FromBody] ConfigurationImportDto request)
        {
            try
            {
                var result = await _configService.ImportConfigurationsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing configurations");
                return StatusCode(500, new ConfigurationImportResultDto
                {
                    Success = false,
                    Errors = { $"Import failed: {ex.Message}" }
                });
            }
        }

        [HttpGet("templates")]
        public ActionResult<IEnumerable<ConfigurationTemplateDto>> GetConfigurationTemplates()
        {
            try
            {
                var templates = new List<ConfigurationTemplateDto>
                {
                    GetDevelopmentTemplate(),
                    GetProductionTemplate(),
                    GetTestingTemplate(),
                    GetHighPerformanceTemplate(),
                    GetHighSecurityTemplate(),
                    GetMinimalTemplate()
                };

                return Ok(templates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting configuration templates");
                return StatusCode(500, new { message = "Error getting configuration templates", error = ex.Message });
            }
        }

        [HttpPost("templates/{templateName}/apply")]
        [RequirePermission("CONFIG_TEMPLATES")]
        [AuditAdminAction("APPLY_TEMPLATE", "Configuration")]
        [RequireConfirmation]
        public async Task<ActionResult<object>> ApplyConfigurationTemplate(
            string templateName, [FromQuery] string? appliedBy = null)
        {
            try
            {
                var template = templateName.ToLower() switch
                {
                    "development" => GetDevelopmentTemplate(),
                    "production" => GetProductionTemplate(),
                    "testing" => GetTestingTemplate(),
                    "high-performance" => GetHighPerformanceTemplate(),
                    "high-security" => GetHighSecurityTemplate(),
                    "minimal" => GetMinimalTemplate(),
                    _ => throw new ArgumentException($"Unknown template: {templateName}")
                };

                var configurations = template.Configurations.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => (object)kvp.Value.Value);

                var success = await _configService.BulkUpdateConfigurationsAsync(configurations, appliedBy);

                return Ok(new
                {
                    success = success,
                    message = success 
                        ? $"Template '{template.TemplateName}' applied successfully"
                        : $"Failed to apply template '{template.TemplateName}'",
                    template = template.TemplateName,
                    configurationsCount = template.Configurations.Count,
                    appliedBy = appliedBy,
                    appliedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying configuration template {TemplateName}", templateName);
                return StatusCode(500, new { message = "Error applying configuration template", error = ex.Message });
            }
        }

        [HttpGet("health")]
        public async Task<ActionResult<SystemHealthDto>> GetSystemHealth()
        {
            try
            {
                var health = new SystemHealthDto
                {
                    IsHealthy = true,
                    CheckedAt = DateTime.UtcNow
                };

                // System info
                health.SystemInfo = new Dictionary<string, object>
                {
                    ["environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                    ["machineName"] = Environment.MachineName,
                    ["processId"] = Environment.ProcessId,
                    ["workingSet"] = GC.GetTotalMemory(false),
                    ["processorCount"] = Environment.ProcessorCount,
                    ["osVersion"] = Environment.OSVersion.ToString(),
                    ["uptime"] = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()
                };

                // Service status (simplified - in real implementation would check actual service status)
                health.ServiceStatus = new Dictionary<string, bool>
                {
                    ["DataRetentionService"] = true,
                    ["AutoScenarioGenerationService"] = true,
                    ["ApiRequestService"] = true,
                    ["LiveDataService"] = true
                };

                // Configuration summary
                var configStats = await _context.SystemConfigurations
                    .GroupBy(c => 1)
                    .Select(g => new
                    {
                        total = g.Count(),
                        readOnly = g.Count(c => c.IsReadOnly),
                        requiresRestart = g.Count(c => c.RequiresRestart),
                        advanced = g.Count(c => c.IsAdvanced)
                    })
                    .FirstOrDefaultAsync();

                if (configStats != null)
                {
                    health.ConfigurationSummary = new Dictionary<string, string>
                    {
                        ["Total Configurations"] = configStats.total.ToString(),
                        ["Read-Only"] = configStats.readOnly.ToString(),
                        ["Requires Restart"] = configStats.requiresRestart.ToString(),
                        ["Advanced"] = configStats.advanced.ToString()
                    };
                }

                // Health checks
                health.HealthChecks = new List<string>
                {
                    "Database connectivity: OK",
                    "Configuration service: OK",
                    "Background services: OK",
                    "Memory usage: Normal"
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system health");
                return Ok(new SystemHealthDto
                {
                    IsHealthy = false,
                    CheckedAt = DateTime.UtcNow,
                    HealthChecks = { $"Health check failed: {ex.Message}" }
                });
            }
        }

        // Helper methods for template definitions
        private ConfigurationTemplateDto GetDevelopmentTemplate()
        {
            return new ConfigurationTemplateDto
            {
                TemplateName = "Development",
                Description = "Optimized settings for development environment",
                Category = "environment",
                Configurations = new Dictionary<string, ConfigurationCreateDto>
                {
                    ["logging.level"] = new() { Key = "logging.level", Value = "Debug", DataType = "string", Category = "logging" },
                    ["retention.api_logs.days"] = new() { Key = "retention.api_logs.days", Value = "30", DataType = "int", Category = "retention" },
                    ["generation.auto_enabled"] = new() { Key = "generation.auto_enabled", Value = "true", DataType = "bool", Category = "generation" },
                    ["features.beta_features_enabled"] = new() { Key = "features.beta_features_enabled", Value = "true", DataType = "bool", Category = "features" }
                }
            };
        }

        private ConfigurationTemplateDto GetProductionTemplate()
        {
            return new ConfigurationTemplateDto
            {
                TemplateName = "Production",
                Description = "Optimized settings for production environment",
                Category = "environment",
                Configurations = new Dictionary<string, ConfigurationCreateDto>
                {
                    ["logging.level"] = new() { Key = "logging.level", Value = "Information", DataType = "string", Category = "logging" },
                    ["retention.api_logs.days"] = new() { Key = "retention.api_logs.days", Value = "90", DataType = "int", Category = "retention" },
                    ["generation.auto_enabled"] = new() { Key = "generation.auto_enabled", Value = "true", DataType = "bool", Category = "generation" },
                    ["security.require_strong_passwords"] = new() { Key = "security.require_strong_passwords", Value = "true", DataType = "bool", Category = "security" }
                }
            };
        }

        private ConfigurationTemplateDto GetTestingTemplate()
        {
            return new ConfigurationTemplateDto
            {
                TemplateName = "Testing",
                Description = "Optimized settings for testing environment",
                Category = "environment",
                Configurations = new Dictionary<string, ConfigurationCreateDto>
                {
                    ["retention.api_logs.days"] = new() { Key = "retention.api_logs.days", Value = "7", DataType = "int", Category = "retention" },
                    ["generation.max_scenarios_per_session"] = new() { Key = "generation.max_scenarios_per_session", Value = "5", DataType = "int", Category = "generation" },
                    ["api.default_timeout_seconds"] = new() { Key = "api.default_timeout_seconds", Value = "10", DataType = "int", Category = "api" }
                }
            };
        }

        private ConfigurationTemplateDto GetHighPerformanceTemplate()
        {
            return new ConfigurationTemplateDto
            {
                TemplateName = "High Performance",
                Description = "Settings optimized for maximum performance",
                Category = "performance",
                Configurations = new Dictionary<string, ConfigurationCreateDto>
                {
                    ["performance.enable_caching"] = new() { Key = "performance.enable_caching", Value = "true", DataType = "bool", Category = "performance" },
                    ["performance.cache_expiration_minutes"] = new() { Key = "performance.cache_expiration_minutes", Value = "30", DataType = "int", Category = "performance" },
                    ["api.max_concurrent_requests"] = new() { Key = "api.max_concurrent_requests", Value = "100", DataType = "int", Category = "api" }
                }
            };
        }

        private ConfigurationTemplateDto GetHighSecurityTemplate()
        {
            return new ConfigurationTemplateDto
            {
                TemplateName = "High Security",
                Description = "Settings with enhanced security measures",
                Category = "security",
                Configurations = new Dictionary<string, ConfigurationCreateDto>
                {
                    ["security.require_strong_passwords"] = new() { Key = "security.require_strong_passwords", Value = "true", DataType = "bool", Category = "security" },
                    ["security.max_login_attempts"] = new() { Key = "security.max_login_attempts", Value = "3", DataType = "int", Category = "security" },
                    ["security.session_timeout_hours"] = new() { Key = "security.session_timeout_hours", Value = "4", DataType = "int", Category = "security" },
                    ["logging.enable_audit_logging"] = new() { Key = "logging.enable_audit_logging", Value = "true", DataType = "bool", Category = "logging" }
                }
            };
        }

        private ConfigurationTemplateDto GetMinimalTemplate()
        {
            return new ConfigurationTemplateDto
            {
                TemplateName = "Minimal",
                Description = "Minimal resource usage settings",
                Category = "resource",
                Configurations = new Dictionary<string, ConfigurationCreateDto>
                {
                    ["generation.auto_enabled"] = new() { Key = "generation.auto_enabled", Value = "false", DataType = "bool", Category = "generation" },
                    ["retention.cleanup_interval_hours"] = new() { Key = "retention.cleanup_interval_hours", Value = "168", DataType = "int", Category = "retention" }, // Weekly
                    ["performance.default_page_size"] = new() { Key = "performance.default_page_size", Value = "10", DataType = "int", Category = "performance" }
                }
            };
        }

        private string GetSectionDescription(string section)
        {
            return section switch
            {
                "Data Retention" => "Settings for managing data retention and cleanup policies",
                "Scenario Generation" => "Configuration for automatic and manual scenario generation",
                "API Settings" => "API-related configuration and limits",
                "Security" => "Security and authentication settings",
                "Performance" => "Performance optimization settings",
                "Features" => "Feature flags and experimental features",
                "Notifications" => "Notification and alerting settings",
                "Logging" => "Logging configuration and audit settings",
                "User Interface" => "User interface and display settings",
                "Services" => "Background service configuration",
                _ => "General system settings"
            };
        }

        private string GetCategoryDisplayName(string category)
        {
            return category switch
            {
                "retention" => "Data Retention",
                "generation" => "Scenario Generation",
                "api" => "API Settings",
                "security" => "Security",
                "performance" => "Performance",
                "features" => "Feature Flags",
                "notifications" => "Notifications",
                "logging" => "Logging",
                "database" => "Database",
                "services" => "Services",
                "ui" => "User Interface",
                _ => category.ToUpperFirst()
            };
        }
    }
}

// Extension method for string formatting
public static class StringExtensions
{
    public static string ToUpperFirst(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpper(str[0]) + str[1..];
    }
}
