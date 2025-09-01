using FraudDetectorWebApp.Controllers;
using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.DTOs;
using FraudDetectorWebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FraudDetectorWebApp.Services
{
    public class AutoScenarioGenerationService : BackgroundService
    {
        private readonly ILogger<AutoScenarioGenerationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _generationInterval = TimeSpan.FromHours(6); // Generate every 6 hours

        // Generation configuration
        private readonly int _maxScenariosPerSession = 10;
        private readonly int _maxTotalActiveScenarios = 1000; // Limit total active scenarios

        public AutoScenarioGenerationService(
            ILogger<AutoScenarioGenerationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Auto Scenario Generation Service started");

            // Wait a bit before starting the first generation
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await GenerateNewScenarios();
                    await Task.Delay(_generationInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during automatic scenario generation");
                    // Wait a shorter time before retrying on error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("Auto Scenario Generation Service stopped");
        }

        private async Task GenerateNewScenarios()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            _logger.LogInformation("Starting automatic scenario generation");

            // Check if we should generate scenarios based on current counts
            var shouldGenerate = await ShouldGenerateScenarios(context);
            if (!shouldGenerate)
            {
                _logger.LogInformation("Skipping generation - sufficient scenarios exist or limits reached");
                return;
            }

            // Analyze existing data to determine what types of scenarios to generate
            var generationPlan = await CreateGenerationPlan(context);

            // Generate scenarios based on the plan
            await ExecuteGenerationPlan(generationPlan, context);
        }

        private async Task<bool> ShouldGenerateScenarios(ApplicationDbContext context)
        {
            // Check total scenario counts
            var totalBetaScenarios = await context.BetaScenarios.CountAsync();
            var totalGeneratedScenarios = await context.GeneratedScenarios.CountAsync();

            if (totalBetaScenarios + totalGeneratedScenarios >= _maxTotalActiveScenarios)
            {
                _logger.LogInformation("Maximum total scenarios reached: {Total}", totalBetaScenarios + totalGeneratedScenarios);
                return false;
            }

            // Check recent generation activity
            var recentBetaScenarios = await context.BetaScenarios
                .Where(s => s.GeneratedAt > DateTime.UtcNow.AddHours(-6))
                .CountAsync();

            var recentGeneratedScenarios = await context.GeneratedScenarios
                .Where(s => s.GeneratedAt > DateTime.UtcNow.AddHours(-6))
                .CountAsync();

            if (recentBetaScenarios + recentGeneratedScenarios >= _maxScenariosPerSession)
            {
                _logger.LogInformation("Recent scenario generation limit reached");
                return false;
            }

            // Check if there are active API configurations
            var hasActiveConfigurations = await context.ApiConfigurations
                .AnyAsync(c => c.IsActive);

            if (!hasActiveConfigurations)
            {
                _logger.LogInformation("No active API configurations found");
                return false;
            }

            return true;
        }

        private async Task<GenerationPlan> CreateGenerationPlan(ApplicationDbContext context)
        {
            var plan = new GenerationPlan();

            // Analyze existing scenario patterns
            var scenarioStats = await AnalyzeExistingScenarios(context);
            
            // Determine what types of scenarios to generate based on usage patterns
            plan.BetaScenariosToGenerate = DetermineBetaScenarios(scenarioStats);
            plan.RegularScenariosToGenerate = DetermineRegularScenarios(scenarioStats);

            _logger.LogInformation("Generation plan created: {@Plan}", plan);

            return plan;
        }

        private async Task<ScenarioStats> AnalyzeExistingScenarios(ApplicationDbContext context)
        {
            var stats = new ScenarioStats();

            // Analyze beta scenarios
            var betaScenarios = await context.BetaScenarios
                .Where(s => s.GeneratedAt > DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            stats.RiskLevelDistribution = betaScenarios
                .GroupBy(s => s.RiskLevel)
                .ToDictionary(g => g.Key, g => g.Count());

            stats.CategoryDistribution = betaScenarios
                .Where(s => !string.IsNullOrEmpty(s.Category))
                .GroupBy(s => s.Category)
                .ToDictionary(g => g.Key, g => g.Count());

            stats.TestSuccessRate = betaScenarios.Count > 0
                ? (double)betaScenarios.Count(s => s.TestSuccessful == true) / betaScenarios.Count
                : 0;

            // Analyze regular scenarios
            var regularScenarios = await context.GeneratedScenarios
                .Where(s => s.GeneratedAt > DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            stats.RegularScenarioCount = regularScenarios.Count;
            stats.RegularTestSuccessRate = regularScenarios.Count > 0
                ? (double)regularScenarios.Count(s => s.TestSuccessful == true) / regularScenarios.Count
                : 0;

            // Analyze recent API activity
            var recentApiLogs = await context.ApiRequestLogs
                .Where(l => l.RequestTimestamp > DateTime.UtcNow.AddDays(-7))
                .CountAsync();

            stats.RecentApiActivity = recentApiLogs;

            return stats;
        }

        private List<BetaScenarioTemplate> DetermineBetaScenarios(ScenarioStats stats)
        {
            var templates = new List<BetaScenarioTemplate>();

            // Generate scenarios to balance risk level distribution
            var targetRiskDistribution = new Dictionary<string, int>
            {
                ["low"] = 2,
                ["medium"] = 3,
                ["high"] = 2,
                ["critical"] = 1
            };

            foreach (var riskLevel in targetRiskDistribution)
            {
                var existingCount = stats.RiskLevelDistribution.GetValueOrDefault(riskLevel.Key, 0);
                var neededCount = Math.Max(0, riskLevel.Value - existingCount / 10); // Adjust based on existing

                for (int i = 0; i < neededCount; i++)
                {
                    templates.Add(new BetaScenarioTemplate
                    {
                        RiskLevel = riskLevel.Key,
                        UserStory = GenerateRandomUserStory(riskLevel.Key),
                        Category = DetermineCategory(riskLevel.Key),
                        Priority = GetPriorityForRiskLevel(riskLevel.Key)
                    });
                }
            }

            // Add scenarios based on recent API activity
            if (stats.RecentApiActivity > 100)
            {
                templates.Add(new BetaScenarioTemplate
                {
                    RiskLevel = "high",
                    UserStory = "High API traffic scenario - suspicious bulk transaction processing detected",
                    Category = "bulk_processing",
                    Priority = 4
                });
            }

            return templates;
        }

        private List<RegularScenarioTemplate> DetermineRegularScenarios(ScenarioStats stats)
        {
            var templates = new List<RegularScenarioTemplate>();

            // Generate regular scenarios if the count is low
            if (stats.RegularScenarioCount < 20)
            {
                var riskLevels = new[] { "low", "medium", "high" };
                foreach (var risk in riskLevels)
                {
                    templates.Add(new RegularScenarioTemplate
                    {
                        RiskFocus = risk,
                        Count = 2
                    });
                }
            }

            return templates;
        }

        private async Task ExecuteGenerationPlan(GenerationPlan plan, ApplicationDbContext context)
        {
            var generationResults = new GenerationResults();

            // Generate beta scenarios
            if (plan.BetaScenariosToGenerate.Any())
            {
                generationResults.BetaScenariosGenerated = await GenerateBetaScenarios(plan.BetaScenariosToGenerate, context);
            }

            // Generate regular scenarios
            if (plan.RegularScenariosToGenerate.Any())
            {
                generationResults.RegularScenariosGenerated = await GenerateRegularScenarios(plan.RegularScenariosToGenerate, context);
            }

            _logger.LogInformation("Automatic generation completed: {@Results}", generationResults);
        }

        private async Task<int> GenerateBetaScenarios(List<BetaScenarioTemplate> templates, ApplicationDbContext context)
        {
            var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            var controller = new BetaScenarioController(context, 
                _serviceProvider.GetRequiredService<ILogger<BetaScenarioController>>(), 
                httpClientFactory);

            var generatedCount = 0;

            foreach (var template in templates)
            {
                try
                {
                    var request = new BetaScenarioRequestDto
                    {
                        Name = $"Auto-{template.RiskLevel}-{DateTime.UtcNow:yyyyMMddHHmm}-{generatedCount + 1}",
                        Description = $"Automatically generated {template.RiskLevel} risk scenario",
                        UserStory = template.UserStory,
                        RiskLevel = template.RiskLevel,
                        Category = template.Category,
                        Priority = template.Priority,
                        UseDatabaseData = true,
                        AutoGenerateWatchlists = true,
                        GeneratedBy = "AutoService"
                    };

                    var result = await controller.GenerateBetaScenario(request);
                    
                    if (result.Value?.Success == true)
                    {
                        generatedCount++;
                        _logger.LogDebug("Generated beta scenario: {Name}", request.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate beta scenario from template: {@Template}", template);
                }
            }

            return generatedCount;
        }

        private async Task<int> GenerateRegularScenarios(List<RegularScenarioTemplate> templates, ApplicationDbContext context)
        {
            var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();
            var controller = new GenerationsController(context, 
                _serviceProvider.GetRequiredService<ILogger<GenerationsController>>(), 
                httpClientFactory);

            var generatedCount = 0;

            foreach (var template in templates)
            {
                try
                {
                    var request = new ScenarioGenerationRequestDto
                    {
                        Count = template.Count,
                        RiskFocus = template.RiskFocus,
                        UseDatabase = true,
                        SaveToDatabase = true,
                        Format = "json"
                    };

                    var result = await controller.GenerateScenarios(request);
                    
                    if (result.Value?.Success == true)
                    {
                        generatedCount += template.Count;
                        _logger.LogDebug("Generated {Count} regular scenarios with risk focus: {RiskFocus}", 
                            template.Count, template.RiskFocus);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate regular scenarios from template: {@Template}", template);
                }
            }

            return generatedCount;
        }

        // Helper methods
        private string GenerateRandomUserStory(string riskLevel)
        {
            var stories = riskLevel switch
            {
                "low" => new[]
                {
                    "Regular customer making routine utility payment",
                    "Small business transferring employee salaries",
                    "Individual paying monthly subscription services",
                    "Customer transferring funds to family member"
                },
                "medium" => new[]
                {
                    "New customer attempting large international transfer",
                    "Business making unusual after-hours transaction",
                    "Customer with sudden increase in transaction frequency",
                    "Transfer to previously unseen beneficiary account"
                },
                "high" => new[]
                {
                    "Multiple rapid high-value transfers to different accounts",
                    "Customer attempting transfer to known high-risk jurisdiction",
                    "Unusual transaction pattern detected by ML algorithm",
                    "Transfer amount significantly higher than customer's typical behavior"
                },
                "critical" => new[]
                {
                    "Transfer to account on sanctions list",
                    "Pattern matching known money laundering scheme",
                    "Multiple accounts controlled by same entity making coordinated transfers",
                    "Customer attempting to circumvent transaction monitoring limits"
                },
                _ => new[] { "Generic transaction scenario for testing purposes" }
            };

            return stories[new Random().Next(stories.Length)];
        }

        private string DetermineCategory(string riskLevel)
        {
            return riskLevel switch
            {
                "low" => new[] { "routine_payment", "salary_transfer", "utility_payment" }[new Random().Next(3)],
                "medium" => new[] { "international_transfer", "new_beneficiary", "unusual_timing" }[new Random().Next(3)],
                "high" => new[] { "suspicious_pattern", "high_value", "rapid_succession" }[new Random().Next(3)],
                "critical" => new[] { "sanctions_risk", "money_laundering", "terrorism_financing" }[new Random().Next(3)],
                _ => "general"
            };
        }

        private int GetPriorityForRiskLevel(string riskLevel)
        {
            return riskLevel switch
            {
                "low" => 1,
                "medium" => 2,
                "high" => 4,
                "critical" => 5,
                _ => 1
            };
        }

        public async Task<AutoGenerationStatusDto> GetStatus()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var status = new AutoGenerationStatusDto
            {
                NextGenerationAt = DateTime.UtcNow.Add(_generationInterval),
                GenerationInterval = _generationInterval,
                MaxScenariosPerSession = _maxScenariosPerSession,
                MaxTotalActiveScenarios = _maxTotalActiveScenarios
            };

            // Get current counts
            status.CurrentBetaScenarios = await context.BetaScenarios.CountAsync();
            status.CurrentRegularScenarios = await context.GeneratedScenarios.CountAsync();

            // Get recent generation activity
            var recentGeneration = await context.BetaScenarios
                .Where(s => s.GeneratedBy == "AutoService" && s.GeneratedAt > DateTime.UtcNow.AddDays(-1))
                .CountAsync();

            status.RecentAutoGenerated = recentGeneration;

            return status;
        }

        public async Task ForceGeneration()
        {
            _logger.LogInformation("Force generation requested");
            await GenerateNewScenarios();
        }
    }

    // Supporting classes
    public class GenerationPlan
    {
        public List<BetaScenarioTemplate> BetaScenariosToGenerate { get; set; } = new();
        public List<RegularScenarioTemplate> RegularScenariosToGenerate { get; set; } = new();
    }

    public class BetaScenarioTemplate
    {
        public string RiskLevel { get; set; } = string.Empty;
        public string UserStory { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    public class RegularScenarioTemplate
    {
        public string RiskFocus { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class ScenarioStats
    {
        public Dictionary<string, int> RiskLevelDistribution { get; set; } = new();
        public Dictionary<string, int> CategoryDistribution { get; set; } = new();
        public double TestSuccessRate { get; set; }
        public int RegularScenarioCount { get; set; }
        public double RegularTestSuccessRate { get; set; }
        public int RecentApiActivity { get; set; }
    }

    public class GenerationResults
    {
        public int BetaScenariosGenerated { get; set; }
        public int RegularScenariosGenerated { get; set; }
    }

    public class AutoGenerationStatusDto
    {
        public DateTime NextGenerationAt { get; set; }
        public TimeSpan GenerationInterval { get; set; }
        public int MaxScenariosPerSession { get; set; }
        public int MaxTotalActiveScenarios { get; set; }
        public int CurrentBetaScenarios { get; set; }
        public int CurrentRegularScenarios { get; set; }
        public int RecentAutoGenerated { get; set; }
    }
}
