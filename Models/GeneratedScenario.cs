using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.Models
{
    public class GeneratedScenario
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public string ScenarioJson { get; set; } = string.Empty;
        
        public string RiskLevel { get; set; } = "mixed"; // low, medium, high, mixed
        
        public string UserProfile { get; set; } = string.Empty;
        
        public string UserActivity { get; set; } = string.Empty;
        
        public int AmountRiskScore { get; set; }
        
        public decimal AmountZScore { get; set; }
        
        public bool HighAmountFlag { get; set; }
        
        public bool HasWatchlistMatch { get; set; }
        
        public string FromName { get; set; } = string.Empty;
        
        public string ToName { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        
        public string ActivityCode { get; set; } = string.Empty;
        
        public string UserType { get; set; } = string.Empty;
        
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        
        // Enhanced fields for better tracking
        public string? ApiEndpoint { get; set; }
        
        public bool IsTested { get; set; } = false;
        
        public DateTime? TestedAt { get; set; }
        
        public string? TestResponse { get; set; }
        
        public int? ResponseTimeMs { get; set; }
        
        public bool? TestSuccessful { get; set; }
        
        public string? TestErrorMessage { get; set; }
        
        // Additional enhanced fields
        public string? GeneratedPrompt { get; set; } // Store the original prompt
        
        public string? FromAccount { get; set; }
        
        public string? ToAccount { get; set; }
        
        public string? TransactionId { get; set; }
        
        public string? CNIC { get; set; }
        
        public string? UserId { get; set; }
        
        public DateTime? TransactionDateTime { get; set; }
        
        public string? TransactionComments { get; set; }
        
        public string? ToBank { get; set; }
        
        public bool NewActivityCode { get; set; }
        
        public bool NewFromAccount { get; set; }
        
        public bool NewToAccount { get; set; }
        
        public bool NewToCity { get; set; }
        
        public bool OutsideUsualDay { get; set; }
        
        // Watchlist indicators
        public bool WatchlistFromAccount { get; set; }
        
        public bool WatchlistFromName { get; set; }
        
        public bool WatchlistToAccount { get; set; }
        
        public bool WatchlistToName { get; set; }
        
        public bool WatchlistToBank { get; set; }
        
        public bool WatchlistIPAddress { get; set; }
        
        // Testing metadata
        public int TestCount { get; set; } = 0;
        
        public DateTime? LastTestedAt { get; set; }
        
        public string? LastTestConfiguration { get; set; }
        
        public int? LastStatusCode { get; set; }
        
        // Tags for categorization
        public string Tags { get; set; } = string.Empty; // JSON array of tags
        
        public bool IsFavorite { get; set; } = false;
        
        public string? Notes { get; set; } // User notes about the scenario
        
        // Foreign key for ApiConfiguration
        public int? ConfigurationId { get; set; }
        
        // Soft deletion
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        public virtual ApiConfiguration? Configuration { get; set; }
        public virtual ICollection<ApiRequestLog> RequestLogs { get; set; } = new List<ApiRequestLog>();
    }
}
