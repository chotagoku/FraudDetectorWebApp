namespace FraudDetectorWebApp.DTOs
{
    // User DTOs
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class UserRegistrationDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Company { get; set; }
    }

    public class UserLoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Scenario DTOs
    public class GeneratedScenarioDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = string.Empty;
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
        public DateTime GeneratedAt { get; set; }
        public bool IsTested { get; set; }
        public DateTime? TestedAt { get; set; }
        public string? TestResponse { get; set; }
        public int? ResponseTimeMs { get; set; }
        public bool? TestSuccessful { get; set; }
        public string? TestErrorMessage { get; set; }
        public int? LastStatusCode { get; set; }
        public string? ApiEndpoint { get; set; }
        public bool IsFavorite { get; set; }
        public string? Notes { get; set; }
    }

    public class ScenarioGenerationRequestDto
    {
        public int Count { get; set; } = 1;
        public string RiskFocus { get; set; } = "mixed";
        public bool SaveToDatabase { get; set; } = true;
        public string Format { get; set; } = "json";
        public bool UseDatabase { get; set; } = false;
    }

    public class TestScenarioRequestDto
    {
        public string ApiEndpoint { get; set; } = string.Empty;
        public string? BearerToken { get; set; }
    }

    public class BulkTestRequestDto
    {
        public List<int> ScenarioIds { get; set; } = new();
        public string ApiEndpoint { get; set; } = string.Empty;
        public string? BearerToken { get; set; }
    }

    public class UpdateNotesRequestDto
    {
        public string? Notes { get; set; }
    }

    // API Configuration DTOs
    public class ApiConfigurationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public string RequestTemplate { get; set; } = string.Empty;
        public string? BearerToken { get; set; }
        public int DelayBetweenRequests { get; set; }
        public int MaxIterations { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int RequestLogsCount { get; set; }
        public DateTime LastRequestTime { get; set; }
    }

    // Response DTOs
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string? Error { get; set; }
        public int? StatusCode { get; set; }
    }

    public class PaginatedResponseDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1;
    }

    public class StatisticsDto
    {
        public int TotalGenerated { get; set; }
        public int TotalTested { get; set; }
        public int SuccessfulTests { get; set; }
        public double AverageResponseTime { get; set; }
        public RiskDistributionDto RiskDistribution { get; set; } = new();
    }

    public class RiskDistributionDto
    {
        public int Low { get; set; }
        public int Medium { get; set; }
        public int High { get; set; }
    }
}
