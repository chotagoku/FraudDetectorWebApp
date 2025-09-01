using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FraudDetectorWebApp.Services
{
    public class DataRetentionService : BackgroundService
    {
        private readonly ILogger<DataRetentionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check daily

        // Retention periods (configurable)
        private readonly TimeSpan _apiLogRetentionPeriod = TimeSpan.FromDays(90); // 90 days
        private readonly TimeSpan _scenarioRetentionPeriod = TimeSpan.FromDays(180); // 180 days
        private readonly TimeSpan _betaScenarioRetentionPeriod = TimeSpan.FromDays(365); // 1 year
        private readonly TimeSpan _configurationRetentionPeriod = TimeSpan.FromDays(365); // 1 year

        public DataRetentionService(
            ILogger<DataRetentionService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Data Retention Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformRetentionCleanup();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during retention cleanup");
                    // Wait a shorter time before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Data Retention Service stopped");
        }

        private async Task PerformRetentionCleanup()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Starting retention cleanup");

            var cleanupResults = new Dictionary<string, int>();

            // Clean up API Request Logs
            cleanupResults["ApiRequestLogs"] = await CleanupApiRequestLogs(context);

            // Clean up Generated Scenarios
            cleanupResults["GeneratedScenarios"] = await CleanupGeneratedScenarios(context);

            // Clean up Beta Scenarios
            cleanupResults["BetaScenarios"] = await CleanupBetaScenarios(context);

            // Clean up API Configurations
            cleanupResults["ApiConfigurations"] = await CleanupApiConfigurations(context);

            _logger.LogInformation("Retention cleanup completed. Results: {@CleanupResults}", cleanupResults);
        }

        private async Task<int> CleanupApiRequestLogs(ApplicationDbContext context)
        {
            var cutoffDate = DateTime.UtcNow - _apiLogRetentionPeriod;
            
            var expiredLogs = await context.ApiRequestLogs
                .IgnoreQueryFilters()
                .Where(log => log.IsDeleted && log.DeletedAt.HasValue && log.DeletedAt.Value < cutoffDate)
                .ToListAsync();

            if (expiredLogs.Any())
            {
                _logger.LogInformation("Permanently deleting {Count} expired API request logs", expiredLogs.Count);
                context.ApiRequestLogs.RemoveRange(expiredLogs);
                await context.SaveChangesAsync();
            }

            return expiredLogs.Count;
        }

        private async Task<int> CleanupGeneratedScenarios(ApplicationDbContext context)
        {
            var cutoffDate = DateTime.UtcNow - _scenarioRetentionPeriod;
            
            var expiredScenarios = await context.GeneratedScenarios
                .IgnoreQueryFilters()
                .Where(scenario => scenario.IsDeleted && scenario.DeletedAt.HasValue && scenario.DeletedAt.Value < cutoffDate)
                .ToListAsync();

            if (expiredScenarios.Any())
            {
                _logger.LogInformation("Permanently deleting {Count} expired generated scenarios", expiredScenarios.Count);
                context.GeneratedScenarios.RemoveRange(expiredScenarios);
                await context.SaveChangesAsync();
            }

            return expiredScenarios.Count;
        }

        private async Task<int> CleanupBetaScenarios(ApplicationDbContext context)
        {
            var cutoffDate = DateTime.UtcNow - _betaScenarioRetentionPeriod;
            
            var expiredBetaScenarios = await context.BetaScenarios
                .IgnoreQueryFilters()
                .Where(scenario => scenario.IsDeleted && scenario.DeletedAt.HasValue && scenario.DeletedAt.Value < cutoffDate)
                .ToListAsync();

            if (expiredBetaScenarios.Any())
            {
                _logger.LogInformation("Permanently deleting {Count} expired beta scenarios", expiredBetaScenarios.Count);
                context.BetaScenarios.RemoveRange(expiredBetaScenarios);
                await context.SaveChangesAsync();
            }

            return expiredBetaScenarios.Count;
        }

        private async Task<int> CleanupApiConfigurations(ApplicationDbContext context)
        {
            var cutoffDate = DateTime.UtcNow - _configurationRetentionPeriod;
            
            var expiredConfigurations = await context.ApiConfigurations
                .IgnoreQueryFilters()
                .Where(config => config.IsDeleted && config.DeletedAt.HasValue && config.DeletedAt.Value < cutoffDate)
                .ToListAsync();

            if (expiredConfigurations.Any())
            {
                _logger.LogInformation("Permanently deleting {Count} expired API configurations", expiredConfigurations.Count);
                context.ApiConfigurations.RemoveRange(expiredConfigurations);
                await context.SaveChangesAsync();
            }

            return expiredConfigurations.Count;
        }

        public async Task<RetentionStatusDto> GetRetentionStatus()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var status = new RetentionStatusDto
            {
                LastCleanupAt = DateTime.UtcNow, // This would be stored in configuration in real implementation
                NextCleanupAt = DateTime.UtcNow.Add(_checkInterval),
                RetentionPolicies = new Dictionary<string, TimeSpan>
                {
                    ["ApiRequestLogs"] = _apiLogRetentionPeriod,
                    ["GeneratedScenarios"] = _scenarioRetentionPeriod,
                    ["BetaScenarios"] = _betaScenarioRetentionPeriod,
                    ["ApiConfigurations"] = _configurationRetentionPeriod
                }
            };

            // Get counts of soft-deleted records
            var apiLogCount = await context.ApiRequestLogs.IgnoreQueryFilters().CountAsync(x => x.IsDeleted);
            var scenarioCount = await context.GeneratedScenarios.IgnoreQueryFilters().CountAsync(x => x.IsDeleted);
            var betaScenarioCount = await context.BetaScenarios.IgnoreQueryFilters().CountAsync(x => x.IsDeleted);
            var configCount = await context.ApiConfigurations.IgnoreQueryFilters().CountAsync(x => x.IsDeleted);

            status.PendingDeletionCounts = new Dictionary<string, int>
            {
                ["ApiRequestLogs"] = apiLogCount,
                ["GeneratedScenarios"] = scenarioCount,
                ["BetaScenarios"] = betaScenarioCount,
                ["ApiConfigurations"] = configCount
            };

            // Get counts of records eligible for permanent deletion
            var apiLogCutoff = DateTime.UtcNow - _apiLogRetentionPeriod;
            var scenarioCutoff = DateTime.UtcNow - _scenarioRetentionPeriod;
            var betaScenarioCutoff = DateTime.UtcNow - _betaScenarioRetentionPeriod;
            var configCutoff = DateTime.UtcNow - _configurationRetentionPeriod;

            var eligibleApiLogs = await context.ApiRequestLogs.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value < apiLogCutoff);
            var eligibleScenarios = await context.GeneratedScenarios.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value < scenarioCutoff);
            var eligibleBetaScenarios = await context.BetaScenarios.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value < betaScenarioCutoff);
            var eligibleConfigs = await context.ApiConfigurations.IgnoreQueryFilters()
                .CountAsync(x => x.IsDeleted && x.DeletedAt.HasValue && x.DeletedAt.Value < configCutoff);

            status.EligibleForPermanentDeletion = new Dictionary<string, int>
            {
                ["ApiRequestLogs"] = eligibleApiLogs,
                ["GeneratedScenarios"] = eligibleScenarios,
                ["BetaScenarios"] = eligibleBetaScenarios,
                ["ApiConfigurations"] = eligibleConfigs
            };

            return status;
        }

        public async Task ForceCleanup()
        {
            _logger.LogInformation("Force cleanup requested");
            await PerformRetentionCleanup();
        }
    }

    public class RetentionStatusDto
    {
        public DateTime LastCleanupAt { get; set; }
        public DateTime NextCleanupAt { get; set; }
        public Dictionary<string, TimeSpan> RetentionPolicies { get; set; } = new();
        public Dictionary<string, int> PendingDeletionCounts { get; set; } = new();
        public Dictionary<string, int> EligibleForPermanentDeletion { get; set; } = new();
    }
}
