using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FraudDetectorWebApp.Models
{
    public class ApiRequestLog : ISoftDelete
    {
        public int Id { get; set; }

        public int ApiConfigurationId { get; set; }

        [Required]
        public string RequestPayload { get; set; } = string.Empty;

        public string? ResponseContent { get; set; }

        public long ResponseTimeMs { get; set; }

        public int StatusCode { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;

        public bool IsSuccessful { get; set; }

        public int IterationNumber { get; set; }

        // Link to GeneratedScenario if this request was testing a specific scenario
        public int? GeneratedScenarioId { get; set; }

        // Link to BetaScenario if this request was testing a beta scenario
        public int? BetaScenarioId { get; set; }

        // Soft deletion
        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(ApiConfigurationId))]
        public virtual ApiConfiguration ApiConfiguration { get; set; } = null!;

        [ForeignKey(nameof(GeneratedScenarioId))]
        public virtual GeneratedScenario? GeneratedScenario { get; set; }

        [ForeignKey(nameof(BetaScenarioId))]
        public virtual BetaScenario? BetaScenario { get; set; }
    }
}
