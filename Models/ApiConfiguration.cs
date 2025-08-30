using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.Models
{
    public class ApiConfiguration
    {
        public int Id { get; set; }
        
        public int? UserId { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public string ApiEndpoint { get; set; } = string.Empty;
        
        [Required]
        public string RequestTemplate { get; set; } = string.Empty;
        
        public string? BearerToken { get; set; }
        
        public int DelayBetweenRequests { get; set; } = 5000; // milliseconds
        
        public int MaxIterations { get; set; } = 10;
        
        public bool IsActive { get; set; } = false;
        
        public bool TrustSslCertificate { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        // Soft deletion
        public bool IsDeleted { get; set; } = false;
        
        public DateTime? DeletedAt { get; set; }
        
        // Navigation properties
        public virtual User? User { get; set; }
        public virtual ICollection<ApiRequestLog> RequestLogs { get; set; } = new List<ApiRequestLog>();
        public virtual ICollection<GeneratedScenario> GeneratedScenarios { get; set; } = new List<GeneratedScenario>();
    }
}
