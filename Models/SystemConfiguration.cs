using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.Models
{
    public class SystemConfiguration : ISoftDelete
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // retention, generation, api, security, etc.

        [Required]
        [StringLength(20)]
        public string DataType { get; set; } = "string"; // string, int, bool, decimal, timespan, datetime

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        public bool IsReadOnly { get; set; } = false;

        public bool RequiresRestart { get; set; } = false;

        public bool IsAdvanced { get; set; } = false; // Hide from basic view

        public string? ValidationRules { get; set; } // JSON validation rules

        public string? DefaultValue { get; set; }

        public string? AllowedValues { get; set; } // JSON array for dropdown options

        [StringLength(50)]
        public string Section { get; set; } = "General"; // UI grouping

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public string? UpdatedBy { get; set; }

        // Soft deletion
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }

    // Predefined configuration categories
    public static class ConfigurationCategories
    {
        public const string Retention = "retention";
        public const string Generation = "generation";
        public const string Api = "api";
        public const string Security = "security";
        public const string Performance = "performance";
        public const string Notifications = "notifications";
        public const string Features = "features";
        public const string Logging = "logging";
        public const string Database = "database";
        public const string Services = "services";
    }

    // Predefined configuration keys
    public static class ConfigurationKeys
    {
        // Retention Settings
        public const string ApiLogRetentionDays = "retention.api_logs.days";
        public const string GeneratedScenarioRetentionDays = "retention.generated_scenarios.days";
        public const string BetaScenarioRetentionDays = "retention.beta_scenarios.days";
        public const string ApiConfigurationRetentionDays = "retention.api_configurations.days";
        public const string RetentionCleanupIntervalHours = "retention.cleanup_interval_hours";

        // Auto Generation Settings
        public const string AutoGenerationEnabled = "generation.auto_enabled";
        public const string AutoGenerationIntervalHours = "generation.interval_hours";
        public const string MaxScenariosPerSession = "generation.max_scenarios_per_session";
        public const string MaxTotalActiveScenarios = "generation.max_total_active_scenarios";
        public const string UseDatabaseDataForGeneration = "generation.use_database_data";
        public const string AutoGenerateWatchlists = "generation.auto_generate_watchlists";

        // API Settings
        public const string DefaultApiTimeout = "api.default_timeout_seconds";
        public const string MaxConcurrentRequests = "api.max_concurrent_requests";
        public const string RateLimitEnabled = "api.rate_limit_enabled";
        public const string RateLimitPerMinute = "api.rate_limit_per_minute";
        public const string TrustSslCertificatesByDefault = "api.trust_ssl_certificates_default";

        // Security Settings
        public const string SessionTimeoutHours = "security.session_timeout_hours";
        public const string RequireStrongPasswords = "security.require_strong_passwords";
        public const string MaxLoginAttempts = "security.max_login_attempts";
        public const string LockoutDurationMinutes = "security.lockout_duration_minutes";
        public const string EnableTwoFactorAuth = "security.enable_two_factor_auth";

        // Performance Settings
        public const string DefaultPageSize = "performance.default_page_size";
        public const string MaxPageSize = "performance.max_page_size";
        public const string EnableCaching = "performance.enable_caching";
        public const string CacheExpirationMinutes = "performance.cache_expiration_minutes";
        public const string DatabaseConnectionPoolSize = "performance.db_connection_pool_size";

        // Feature Flags
        public const string BetaFeaturesEnabled = "features.beta_features_enabled";
        public const string ScenarioGenerationEnabled = "features.scenario_generation_enabled";
        public const string RealTimeUpdatesEnabled = "features.realtime_updates_enabled";
        public const string AdvancedAnalyticsEnabled = "features.advanced_analytics_enabled";
        public const string BulkOperationsEnabled = "features.bulk_operations_enabled";

        // Notification Settings
        public const string EmailNotificationsEnabled = "notifications.email_enabled";
        public const string SlackNotificationsEnabled = "notifications.slack_enabled";
        public const string NotifyOnHighRiskScenarios = "notifications.notify_high_risk_scenarios";
        public const string NotifyOnServiceErrors = "notifications.notify_service_errors";
        public const string NotifyOnRetentionCleanup = "notifications.notify_retention_cleanup";

        // Logging Settings
        public const string LogLevel = "logging.level";
        public const string EnableAuditLogging = "logging.enable_audit_logging";
        public const string EnablePerformanceLogging = "logging.enable_performance_logging";
        public const string LogRetentionDays = "logging.retention_days";
        public const string EnableSensitiveDataLogging = "logging.enable_sensitive_data_logging";

        // UI Settings
        public const string DefaultTheme = "ui.default_theme";
        public const string ShowAdvancedOptions = "ui.show_advanced_options";
        public const string EnableTooltips = "ui.enable_tooltips";
        public const string AutoRefreshInterval = "ui.auto_refresh_interval_seconds";
        public const string ShowBetaFeatures = "ui.show_beta_features";

        // Service Settings
        public const string EnableBackgroundServices = "services.enable_background_services";
        public const string ServiceHealthCheckInterval = "services.health_check_interval_minutes";
        public const string EnableServiceAutoRestart = "services.enable_auto_restart";
        public const string MaxServiceRestartAttempts = "services.max_restart_attempts";
    }

    // Configuration sections for UI grouping
    public static class ConfigurationSections
    {
        public const string DataRetention = "Data Retention";
        public const string ScenarioGeneration = "Scenario Generation";
        public const string ApiSettings = "API Settings";
        public const string Security = "Security";
        public const string Performance = "Performance";
        public const string Features = "Feature Flags";
        public const string Notifications = "Notifications";
        public const string Logging = "Logging";
        public const string UserInterface = "User Interface";
        public const string Services = "Background Services";
    }
}
