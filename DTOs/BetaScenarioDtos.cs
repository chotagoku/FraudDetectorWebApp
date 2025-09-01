using System.ComponentModel.DataAnnotations;
using FraudDetectorWebApp.Attributes;

namespace FraudDetectorWebApp.DTOs
{
    public class BetaScenarioRequestDto
    {
        [Required(ErrorMessage = "Scenario name is required.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Scenario name must be between 3 and 200 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "User story is required.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "User story must be between 10 and 2000 characters.")]
        public string UserStory { get; set; } = string.Empty;

        public string? Conditions { get; set; }

        public string RiskLevel { get; set; } = "medium"; // low, medium, high, critical

        public string? Category { get; set; }

        public string? BusinessType { get; set; }

        public string? CustomerSegment { get; set; }

        public bool UseDatabaseData { get; set; } = true; // Whether to incorporate existing DB data

        public bool AutoGenerateWatchlists { get; set; } = true; // Auto-create watchlist matches

        public int? ConfigurationId { get; set; }

        public string? PreferredCurrency { get; set; } = "PKR";

        [NumericRange(0.01, 999999999, ErrorMessage = "Amount must be between 0.01 and 999,999,999.")]
        public decimal? SuggestedAmount { get; set; }

        public string? Tags { get; set; } // Comma-separated tags

        [Range(0, 5, ErrorMessage = "Priority must be between 0 and 5.")]
        public int Priority { get; set; } = 0; // 0-5

        public string GeneratedBy { get; set; } = "System";
    }

    public class BetaScenarioUpdateDto
    {
        [StringLength(200)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? UserStory { get; set; }

        public string? Conditions { get; set; }

        public string? RiskLevel { get; set; }

        public string? Category { get; set; }

        public string? BusinessType { get; set; }

        public string? CustomerSegment { get; set; }

        public string? Tags { get; set; }

        public int? Priority { get; set; }

        public string? Status { get; set; }

        public string? Notes { get; set; }

        public bool? IsFavorite { get; set; }
    }

    public class BetaScenarioResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserStory { get; set; } = string.Empty;
        public string GeneratedStory { get; set; } = string.Empty;
        public string TransactionStory { get; set; } = string.Empty;
        public object? ScenarioJson { get; set; } // Parsed JSON object
        public string RiskLevel { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Conditions { get; set; } = string.Empty;
        
        // Profile Information
        public string UserProfile { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string CustomerSegment { get; set; } = string.Empty;
        
        // Transaction Details
        public string FromName { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string ActivityCode { get; set; } = string.Empty;
        
        // Risk Scoring
        public int AmountRiskScore { get; set; }
        public decimal AmountZScore { get; set; }
        public int FraudScore { get; set; }
        public int ComplianceScore { get; set; }
        
        // Flags
        public bool HighAmountFlag { get; set; }
        public bool SuspiciousActivityFlag { get; set; }
        public bool ComplianceFlag { get; set; }
        
        // Generation Metadata
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
        
        // Testing Info
        public bool IsTested { get; set; }
        public DateTime? TestedAt { get; set; }
        public bool? TestSuccessful { get; set; }
        
        // Management
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool IsFavorite { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; } = string.Empty;
        
        public bool UsedDatabaseData { get; set; }
        public string? SourceDataSummary { get; set; }
    }

    public class BetaScenarioBulkRequestDto
    {
        [Required]
        [Range(1, 50)]
        public int Count { get; set; } = 1;

        [Required]
        public string BaseStory { get; set; } = string.Empty;

        public string? Conditions { get; set; }

        public string RiskLevel { get; set; } = "mixed"; // low, medium, high, critical, mixed

        public string? Category { get; set; }

        public bool UseDatabaseData { get; set; } = true;

        public bool AutoGenerateWatchlists { get; set; } = true;

        public bool SaveToDatabase { get; set; } = true;

        public int? ConfigurationId { get; set; }

        public string GeneratedBy { get; set; } = "System";

        public bool VariateStories { get; set; } = true; // Create variations of the base story
    }

    public class BetaScenarioTestRequestDto
    {
        [Required(ErrorMessage = "API endpoint is required.")]
        [ValidUrl(ErrorMessage = "Please enter a valid API endpoint URL.")]
        public string ApiEndpoint { get; set; } = string.Empty;

        public string? BearerToken { get; set; }

        public bool UpdateScenarioWithResults { get; set; } = true;
    }

    public class BetaScenarioBulkTestRequestDto
    {
        [Required(ErrorMessage = "Scenario IDs are required.")]
        [CollectionCount(MinCount = 1, MaxCount = 50, ErrorMessage = "You must select between 1 and 50 scenarios.")]
        public int[] ScenarioIds { get; set; } = Array.Empty<int>();

        [Required(ErrorMessage = "API endpoint is required.")]
        [ValidUrl(ErrorMessage = "Please enter a valid API endpoint URL.")]
        public string ApiEndpoint { get; set; } = string.Empty;

        public string? BearerToken { get; set; }

        public bool UpdateScenariosWithResults { get; set; } = true;
    }

    public class DatabaseDataIntegrationDto
    {
        public bool IncludeExistingProfiles { get; set; } = true;
        public bool IncludeHistoricalTransactions { get; set; } = true;
        public bool IncludeWatchlistData { get; set; } = true;
        public bool IncludeRiskPatterns { get; set; } = true;
        
        public int? LimitToRecentDays { get; set; } = 30; // Only use data from last N days
        public string? FilterByRiskLevel { get; set; } // Only use scenarios of specific risk level
        public int? MaxSampleSize { get; set; } = 100; // Max number of records to sample
    }

    public class ScenarioGenerationOptionsDto
    {
        public string GenerationEngine { get; set; } = "GPT-4"; // AI engine to use
        public bool IncludeNarrative { get; set; } = true;
        public bool GenerateComplexScenario { get; set; } = true;
        public bool IncludeComplianceAspects { get; set; } = true;
        public bool IncludeAMLChecks { get; set; } = true;
        public bool GenerateMultipleVariations { get; set; } = false;
        public int VariationCount { get; set; } = 3;
    }
}
