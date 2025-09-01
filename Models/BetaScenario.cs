using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.Models
{
    public class BetaScenario : ISoftDelete
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public string UserStory { get; set; } = string.Empty; // User-provided story/conditions

        [Required]
        public string GeneratedStory { get; set; } = string.Empty; // AI-generated comprehensive story

        [Required]
        public string TransactionStory { get; set; } = string.Empty; // Generated transaction narrative

        [Required]
        public string ScenarioJson { get; set; } = string.Empty; // Final API request JSON

        public string RiskLevel { get; set; } = "medium"; // low, medium, high, critical

        public string Category { get; set; } = string.Empty; // fraud_type, business_category, etc.

        public string Conditions { get; set; } = string.Empty; // User-specified conditions

        // Profile Information
        public string UserProfile { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string CustomerSegment { get; set; } = string.Empty;

        // Transaction Details
        public string FromName { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string FromAccount { get; set; } = string.Empty;
        public string ToAccount { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";
        public string ActivityCode { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;

        // Risk Scoring
        public int AmountRiskScore { get; set; }
        public decimal AmountZScore { get; set; }
        public int FraudScore { get; set; } // 0-100 fraud likelihood
        public int ComplianceScore { get; set; } // 0-100 compliance risk

        // Flags and Indicators
        public bool HighAmountFlag { get; set; }
        public bool SuspiciousActivityFlag { get; set; }
        public bool ComplianceFlag { get; set; }
        public bool AMLFlag { get; set; }
        public bool CTFFlag { get; set; } // Counter Terrorism Financing

        // Context Flags
        public bool NewActivityCode { get; set; }
        public bool NewFromAccount { get; set; }
        public bool NewToAccount { get; set; }
        public bool NewToCity { get; set; }
        public bool OutsideUsualDay { get; set; }
        public bool OfficeHours { get; set; }

        // Enhanced Watchlist Indicators
        public bool WatchlistFromAccount { get; set; }
        public bool WatchlistFromName { get; set; }
        public bool WatchlistToAccount { get; set; }
        public bool WatchlistToName { get; set; }
        public bool WatchlistToBank { get; set; }
        public bool WatchlistIPAddress { get; set; }
        public bool WatchlistCNIC { get; set; }
        public bool WatchlistPhoneNumber { get; set; }

        // Transaction Context
        public string? CNIC { get; set; }
        public string? PhoneNumber { get; set; }
        public string? IPAddress { get; set; }
        public string? DeviceId { get; set; }
        public string? Location { get; set; }
        public string? TransactionId { get; set; }
        public string? UserId { get; set; }
        public DateTime? TransactionDateTime { get; set; }
        public string? TransactionComments { get; set; }
        public string? ToBank { get; set; }

        // Generation Metadata
        public string? GenerationPrompt { get; set; } // Original user prompt
        public string? GenerationEngine { get; set; } // AI engine used
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public string GeneratedBy { get; set; } = string.Empty; // User who generated it
        
        // Testing Information
        public bool IsTested { get; set; } = false;
        public DateTime? TestedAt { get; set; }
        public string? TestResponse { get; set; }
        public int? ResponseTimeMs { get; set; }
        public bool? TestSuccessful { get; set; }
        public string? TestErrorMessage { get; set; }
        public int TestCount { get; set; } = 0;
        public DateTime? LastTestedAt { get; set; }
        public int? LastStatusCode { get; set; }
        public string? ApiEndpoint { get; set; }

        // Categorization and Management
        public string Tags { get; set; } = string.Empty; // JSON array of tags
        public bool IsFavorite { get; set; } = false;
        public string? Notes { get; set; }
        public int Priority { get; set; } = 0; // 0-5 priority level
        public string Status { get; set; } = "draft"; // draft, ready, tested, validated, archived

        // Database Integration
        public bool UsedDatabaseData { get; set; } = false; // Whether existing DB data was used
        public string? SourceDataSummary { get; set; } // Summary of what DB data was used

        // Foreign Keys
        public int? ConfigurationId { get; set; }
        public int? BasedOnScenarioId { get; set; } // If created based on another scenario

        // Soft deletion
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        public virtual ApiConfiguration? Configuration { get; set; }
        public virtual BetaScenario? BaseScenario { get; set; }
        public virtual ICollection<BetaScenario> DerivedScenarios { get; set; } = new List<BetaScenario>();
        public virtual ICollection<ApiRequestLog> RequestLogs { get; set; } = new List<ApiRequestLog>();
    }
}
