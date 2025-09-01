using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using Microsoft.EntityFrameworkCore;

namespace FraudDetectorWebApp.Services
{
    public class ConfigurationSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConfigurationSeeder> _logger;

        public ConfigurationSeeder(ApplicationDbContext context, ILogger<ConfigurationSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDefaultConfigurationsAsync()
        {
            try
            {
                // Check if any configurations already exist
                if (await _context.SystemConfigurations.AnyAsync())
                {
                    _logger.LogInformation("System configurations already exist. Skipping seeding.");
                    return;
                }

                _logger.LogInformation("Seeding default system configurations...");

                var defaultConfigs = GetDefaultConfigurations();

                await _context.SystemConfigurations.AddRangeAsync(defaultConfigs);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully seeded {Count} default system configurations", defaultConfigs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding default system configurations");
                throw;
            }
        }

        private List<SystemConfiguration> GetDefaultConfigurations()
        {
            var now = DateTime.UtcNow;
            const string systemUser = "System";

            return new List<SystemConfiguration>
            {
                // Data Retention Settings
                new()
                {
                    Key = "retention.api_logs.days",
                    Value = "90",
                    Category = "retention",
                    DataType = "int",
                    DisplayName = "API Logs Retention (Days)",
                    Description = "Number of days to keep API request logs before cleanup",
                    Section = "Data Retention",
                    DefaultValue = "90",
                    ValidationRules = "{\"min\": 1, \"max\": 365}",
                    DisplayOrder = 1,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "retention.scenarios.days",
                    Value = "180",
                    Category = "retention",
                    DataType = "int",
                    DisplayName = "Generated Scenarios Retention (Days)",
                    Description = "Number of days to keep generated scenarios before cleanup",
                    Section = "Data Retention",
                    DefaultValue = "180",
                    ValidationRules = "{\"min\": 7, \"max\": 730}",
                    DisplayOrder = 2,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "retention.cleanup_interval_hours",
                    Value = "24",
                    Category = "retention",
                    DataType = "int",
                    DisplayName = "Cleanup Interval (Hours)",
                    Description = "How often to run the data retention cleanup process",
                    Section = "Data Retention",
                    DefaultValue = "24",
                    ValidationRules = "{\"min\": 1, \"max\": 168}",
                    DisplayOrder = 3,
                    RequiresRestart = false,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // Scenario Generation Settings
                new()
                {
                    Key = "generation.auto_enabled",
                    Value = "true",
                    Category = "generation",
                    DataType = "bool",
                    DisplayName = "Enable Auto Generation",
                    Description = "Enable automatic scenario generation service",
                    Section = "Scenario Generation",
                    DefaultValue = "true",
                    DisplayOrder = 1,
                    RequiresRestart = true,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "generation.interval_minutes",
                    Value = "60",
                    Category = "generation",
                    DataType = "int",
                    DisplayName = "Generation Interval (Minutes)",
                    Description = "How often to automatically generate new scenarios",
                    Section = "Scenario Generation",
                    DefaultValue = "60",
                    ValidationRules = "{\"min\": 5, \"max\": 1440}",
                    DisplayOrder = 2,
                    RequiresRestart = false,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "generation.max_scenarios_per_session",
                    Value = "10",
                    Category = "generation",
                    DataType = "int",
                    DisplayName = "Max Scenarios per Session",
                    Description = "Maximum number of scenarios to generate in one session",
                    Section = "Scenario Generation",
                    DefaultValue = "10",
                    ValidationRules = "{\"min\": 1, \"max\": 100}",
                    DisplayOrder = 3,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // API Settings
                new()
                {
                    Key = "api.default_timeout_seconds",
                    Value = "30",
                    Category = "api",
                    DataType = "int",
                    DisplayName = "Default API Timeout (Seconds)",
                    Description = "Default timeout for external API requests",
                    Section = "API Settings",
                    DefaultValue = "30",
                    ValidationRules = "{\"min\": 5, \"max\": 300}",
                    DisplayOrder = 1,
                    RequiresRestart = false,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "api.max_concurrent_requests",
                    Value = "50",
                    Category = "api",
                    DataType = "int",
                    DisplayName = "Max Concurrent Requests",
                    Description = "Maximum number of concurrent API requests",
                    Section = "API Settings",
                    DefaultValue = "50",
                    ValidationRules = "{\"min\": 1, \"max\": 200}",
                    DisplayOrder = 2,
                    RequiresRestart = true,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // Security Settings
                new()
                {
                    Key = "security.require_strong_passwords",
                    Value = "true",
                    Category = "security",
                    DataType = "bool",
                    DisplayName = "Require Strong Passwords",
                    Description = "Enforce strong password requirements",
                    Section = "Security",
                    DefaultValue = "true",
                    DisplayOrder = 1,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "security.max_login_attempts",
                    Value = "5",
                    Category = "security",
                    DataType = "int",
                    DisplayName = "Max Login Attempts",
                    Description = "Maximum failed login attempts before account lockout",
                    Section = "Security",
                    DefaultValue = "5",
                    ValidationRules = "{\"min\": 1, \"max\": 20}",
                    DisplayOrder = 2,
                    RequiresRestart = false,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "security.session_timeout_hours",
                    Value = "8",
                    Category = "security",
                    DataType = "int",
                    DisplayName = "Session Timeout (Hours)",
                    Description = "How long user sessions remain active",
                    Section = "Security",
                    DefaultValue = "8",
                    ValidationRules = "{\"min\": 1, \"max\": 72}",
                    DisplayOrder = 3,
                    RequiresRestart = true,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // Performance Settings
                new()
                {
                    Key = "performance.enable_caching",
                    Value = "true",
                    Category = "performance",
                    DataType = "bool",
                    DisplayName = "Enable Caching",
                    Description = "Enable memory caching for better performance",
                    Section = "Performance",
                    DefaultValue = "true",
                    DisplayOrder = 1,
                    RequiresRestart = true,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "performance.cache_expiration_minutes",
                    Value = "60",
                    Category = "performance",
                    DataType = "int",
                    DisplayName = "Cache Expiration (Minutes)",
                    Description = "How long cached data remains valid",
                    Section = "Performance",
                    DefaultValue = "60",
                    ValidationRules = "{\"min\": 1, \"max\": 1440}",
                    DisplayOrder = 2,
                    RequiresRestart = false,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "performance.default_page_size",
                    Value = "25",
                    Category = "performance",
                    DataType = "int",
                    DisplayName = "Default Page Size",
                    Description = "Default number of items per page in listings",
                    Section = "Performance",
                    DefaultValue = "25",
                    ValidationRules = "{\"min\": 5, \"max\": 1000}",
                    AllowedValues = "[\"10\", \"25\", \"50\", \"100\"]",
                    DisplayOrder = 3,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // Feature Flags
                new()
                {
                    Key = "features.beta_features_enabled",
                    Value = "false",
                    Category = "features",
                    DataType = "bool",
                    DisplayName = "Enable Beta Features",
                    Description = "Enable experimental beta features",
                    Section = "Features",
                    DefaultValue = "false",
                    DisplayOrder = 1,
                    RequiresRestart = true,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // Logging Settings
                new()
                {
                    Key = "logging.level",
                    Value = "Information",
                    Category = "logging",
                    DataType = "string",
                    DisplayName = "Logging Level",
                    Description = "Minimum logging level for application logs",
                    Section = "Logging",
                    DefaultValue = "Information",
                    AllowedValues = "[\"Trace\", \"Debug\", \"Information\", \"Warning\", \"Error\", \"Critical\"]",
                    DisplayOrder = 1,
                    RequiresRestart = true,
                    IsAdvanced = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "logging.enable_audit_logging",
                    Value = "true",
                    Category = "logging",
                    DataType = "bool",
                    DisplayName = "Enable Audit Logging",
                    Description = "Log all user actions for auditing purposes",
                    Section = "Logging",
                    DefaultValue = "true",
                    DisplayOrder = 2,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // Notification Settings
                new()
                {
                    Key = "notifications.email_enabled",
                    Value = "false",
                    Category = "notifications",
                    DataType = "bool",
                    DisplayName = "Enable Email Notifications",
                    Description = "Send email notifications for important events",
                    Section = "Notifications",
                    DefaultValue = "false",
                    DisplayOrder = 1,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // UI Settings
                new()
                {
                    Key = "ui.theme",
                    Value = "auto",
                    Category = "ui",
                    DataType = "string",
                    DisplayName = "UI Theme",
                    Description = "Default theme for the user interface",
                    Section = "User Interface",
                    DefaultValue = "auto",
                    AllowedValues = "[\"light\", \"dark\", \"auto\"]",
                    DisplayOrder = 1,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "ui.language",
                    Value = "en",
                    Category = "ui",
                    DataType = "string",
                    DisplayName = "Default Language",
                    Description = "Default language for the user interface",
                    Section = "User Interface",
                    DefaultValue = "en",
                    AllowedValues = "[\"en\", \"es\", \"fr\", \"de\"]",
                    DisplayOrder = 2,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },

                // System Maintenance
                new()
                {
                    Key = "system.maintenance_mode",
                    Value = "false",
                    Category = "services",
                    DataType = "bool",
                    DisplayName = "Maintenance Mode",
                    Description = "Put the system into maintenance mode",
                    Section = "Services",
                    DefaultValue = "false",
                    DisplayOrder = 1,
                    RequiresRestart = false,
                    IsAdvanced = true,
                    IsReadOnly = false,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Key = "system.version",
                    Value = "1.0.0",
                    Category = "services",
                    DataType = "string",
                    DisplayName = "System Version",
                    Description = "Current version of the application",
                    Section = "Services",
                    DefaultValue = "1.0.0",
                    DisplayOrder = 2,
                    RequiresRestart = false,
                    IsAdvanced = false,
                    IsReadOnly = true,
                    UpdatedBy = systemUser,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            };
        }
    }
}
