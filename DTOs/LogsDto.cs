using System.ComponentModel.DataAnnotations;

namespace FraudDetectorWebApp.DTOs
{
    public class LogSearchDto
    {
        public string? SearchTerm { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public LogLevel? LogLevel { get; set; }
        public LogType? LogType { get; set; }
        public bool? IsSuccessful { get; set; }
        public int? StatusCodeFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string OrderBy { get; set; } = "timestamp_desc";
    }

    public class ApiRequestLogDto
    {
        public int Id { get; set; }
        public string ApiName { get; set; } = string.Empty;
        public string RequestPayload { get; set; } = string.Empty;
        public string? ResponseContent { get; set; }
        public long ResponseTimeMs { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime RequestTimestamp { get; set; }
        public bool IsSuccessful { get; set; }
        public int IterationNumber { get; set; }
        public string? ScenarioName { get; set; }
        public string? ScenarioType { get; set; } // "Generated" or "Beta"
        public int? ScenarioId { get; set; }
    }

    public class SystemLogDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public Dictionary<string, object>? Properties { get; set; }
    }

    public class LogExportDto
    {
        [Required]
        public LogType LogType { get; set; }
        
        [Required]
        public string Format { get; set; } = "json"; // json, csv, txt
        
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SearchTerm { get; set; }
        public LogLevel? MinLogLevel { get; set; }
        public int MaxRecords { get; set; } = 10000;
    }

    public class LogStatsDto
    {
        public int TotalLogs { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public int DebugCount { get; set; }
        public double AverageResponseTime { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public Dictionary<int, int> StatusCodeDistribution { get; set; } = new();
        public Dictionary<string, int> ApiUsageStats { get; set; } = new();
        public List<LogTrendPoint> HourlyTrends { get; set; } = new();
    }

    public class LogTrendPoint
    {
        public DateTime Timestamp { get; set; }
        public int Count { get; set; }
        public int ErrorCount { get; set; }
        public double AverageResponseTime { get; set; }
    }

    public class LogCleanupDto
    {
        [Required]
        public LogType LogType { get; set; }
        
        [Required]
        public DateTime OlderThan { get; set; }
        
        public bool DryRun { get; set; } = true;
    }

    public class LogCleanupResultDto
    {
        public bool Success { get; set; }
        public int RecordsDeleted { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Details { get; set; } = new();
        public bool WasDryRun { get; set; }
    }

    public enum LogType
    {
        ApiRequest,
        System,
        Audit,
        All
    }

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5
    }
}
