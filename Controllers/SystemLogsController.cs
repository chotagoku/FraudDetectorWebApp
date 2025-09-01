using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.DTOs;
using FraudDetectorWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/system-logs")]
    [Authorize(Roles = "Admin")]
    public class SystemLogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemLogsController> _logger;

        public SystemLogsController(ApplicationDbContext context, ILogger<SystemLogsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("api-requests")]
        public async Task<ActionResult<IEnumerable<ApiRequestLogDto>>> GetApiRequestLogs([FromQuery] LogSearchDto search)
        {
            try
            {
                var query = _context.ApiRequestLogs
                    .Include(log => log.ApiConfiguration)
                    .Include(log => log.GeneratedScenario)
                    .Include(log => log.BetaScenario)
                    .Where(log => !log.IsDeleted);

                // Apply filters
                if (search.StartDate.HasValue)
                    query = query.Where(log => log.RequestTimestamp >= search.StartDate.Value);

                if (search.EndDate.HasValue)
                    query = query.Where(log => log.RequestTimestamp <= search.EndDate.Value);

                if (!string.IsNullOrEmpty(search.SearchTerm))
                {
                    query = query.Where(log => 
                        log.RequestPayload.Contains(search.SearchTerm) ||
                        log.ErrorMessage!.Contains(search.SearchTerm) ||
                        log.ApiConfiguration.Name.Contains(search.SearchTerm));
                }

                if (search.IsSuccessful.HasValue)
                    query = query.Where(log => log.IsSuccessful == search.IsSuccessful.Value);

                if (search.StatusCodeFilter.HasValue)
                    query = query.Where(log => log.StatusCode == search.StatusCodeFilter.Value);

                // Apply ordering
                query = search.OrderBy.ToLower() switch
                {
                    "timestamp_asc" => query.OrderBy(log => log.RequestTimestamp),
                    "timestamp_desc" => query.OrderByDescending(log => log.RequestTimestamp),
                    "response_time_asc" => query.OrderBy(log => log.ResponseTimeMs),
                    "response_time_desc" => query.OrderByDescending(log => log.ResponseTimeMs),
                    "status_asc" => query.OrderBy(log => log.StatusCode),
                    "status_desc" => query.OrderByDescending(log => log.StatusCode),
                    _ => query.OrderByDescending(log => log.RequestTimestamp)
                };

                var totalCount = await query.CountAsync();

                var logs = await query
                    .Skip((search.Page - 1) * search.PageSize)
                    .Take(search.PageSize)
                    .ToListAsync();

                var result = logs.Select(log => new ApiRequestLogDto
                {
                    Id = log.Id,
                    ApiName = log.ApiConfiguration.Name,
                    RequestPayload = log.RequestPayload,
                    ResponseContent = log.ResponseContent,
                    ResponseTimeMs = log.ResponseTimeMs,
                    StatusCode = log.StatusCode,
                    ErrorMessage = log.ErrorMessage,
                    RequestTimestamp = log.RequestTimestamp,
                    IsSuccessful = log.IsSuccessful,
                    IterationNumber = log.IterationNumber,
                    ScenarioName = log.GeneratedScenario?.Name ?? log.BetaScenario?.Name,
                    ScenarioType = log.GeneratedScenario != null ? "Generated" : 
                                  log.BetaScenario != null ? "Beta" : null,
                    ScenarioId = log.GeneratedScenarioId ?? log.BetaScenarioId
                }).ToList();

                Response.Headers["X-Total-Count"] = totalCount.ToString();
                Response.Headers["X-Page"] = search.Page.ToString();
                Response.Headers["X-Page-Size"] = search.PageSize.ToString();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API request logs");
                return StatusCode(500, new { message = "Error retrieving API request logs", error = ex.Message });
            }
        }

        [HttpGet("api-requests/stats")]
        public async Task<ActionResult<LogStatsDto>> GetApiRequestStats([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var query = _context.ApiRequestLogs.Where(log => !log.IsDeleted);

                if (startDate.HasValue)
                    query = query.Where(log => log.RequestTimestamp >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(log => log.RequestTimestamp <= endDate.Value);

                var logs = await query.Include(log => log.ApiConfiguration).ToListAsync();

                var stats = new LogStatsDto
                {
                    TotalLogs = logs.Count,
                    SuccessfulRequests = logs.Count(l => l.IsSuccessful),
                    FailedRequests = logs.Count(l => !l.IsSuccessful),
                    AverageResponseTime = logs.Any() ? logs.Average(l => l.ResponseTimeMs) : 0,
                    StatusCodeDistribution = logs.GroupBy(l => l.StatusCode)
                                               .ToDictionary(g => g.Key, g => g.Count()),
                    ApiUsageStats = logs.GroupBy(l => l.ApiConfiguration.Name)
                                       .ToDictionary(g => g.Key, g => g.Count())
                };

                // Generate hourly trends for the last 24 hours or the specified period
                var trendStart = startDate ?? DateTime.UtcNow.AddHours(-24);
                var trendEnd = endDate ?? DateTime.UtcNow;
                
                stats.HourlyTrends = GenerateHourlyTrends(logs, trendStart, trendEnd);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API request stats");
                return StatusCode(500, new { message = "Error retrieving API request stats", error = ex.Message });
            }
        }

        [HttpGet("api-requests/{id}")]
        public async Task<ActionResult<ApiRequestLogDto>> GetApiRequestLog(int id)
        {
            try
            {
                var log = await _context.ApiRequestLogs
                    .Include(log => log.ApiConfiguration)
                    .Include(log => log.GeneratedScenario)
                    .Include(log => log.BetaScenario)
                    .FirstOrDefaultAsync(log => log.Id == id && !log.IsDeleted);

                if (log == null)
                    return NotFound();

                var result = new ApiRequestLogDto
                {
                    Id = log.Id,
                    ApiName = log.ApiConfiguration.Name,
                    RequestPayload = log.RequestPayload,
                    ResponseContent = log.ResponseContent,
                    ResponseTimeMs = log.ResponseTimeMs,
                    StatusCode = log.StatusCode,
                    ErrorMessage = log.ErrorMessage,
                    RequestTimestamp = log.RequestTimestamp,
                    IsSuccessful = log.IsSuccessful,
                    IterationNumber = log.IterationNumber,
                    ScenarioName = log.GeneratedScenario?.Name ?? log.BetaScenario?.Name,
                    ScenarioType = log.GeneratedScenario != null ? "Generated" : 
                                  log.BetaScenario != null ? "Beta" : null,
                    ScenarioId = log.GeneratedScenarioId ?? log.BetaScenarioId
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API request log {Id}", id);
                return StatusCode(500, new { message = "Error retrieving API request log", error = ex.Message });
            }
        }

        [HttpPost("export")]
        public async Task<ActionResult> ExportLogs([FromBody] LogExportDto request)
        {
            try
            {
                string exportData = "";
                string fileName = "";
                string contentType = "";

                switch (request.LogType)
                {
                    case LogType.ApiRequest:
                        var apiLogs = await GetApiRequestLogsForExport(request);
                        (exportData, fileName, contentType) = FormatLogsForExport(apiLogs, request, "api_requests");
                        break;

                    case LogType.System:
                        // For now, return a placeholder since we don't have system logs table
                        var systemLogs = GetSystemLogsForExport(request);
                        (exportData, fileName, contentType) = FormatLogsForExport(systemLogs, request, "system_logs");
                        break;

                    default:
                        return BadRequest(new { message = "Unsupported log type for export" });
                }

                return File(Encoding.UTF8.GetBytes(exportData), contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting logs");
                return StatusCode(500, new { message = "Error exporting logs", error = ex.Message });
            }
        }

        [HttpPost("cleanup")]
        public async Task<ActionResult<LogCleanupResultDto>> CleanupLogs([FromBody] LogCleanupDto request)
        {
            try
            {
                var result = new LogCleanupResultDto
                {
                    WasDryRun = request.DryRun,
                    Details = new List<string>()
                };

                int recordsToDelete = 0;

                if (request.LogType == LogType.ApiRequest || request.LogType == LogType.All)
                {
                    var apiLogsQuery = _context.ApiRequestLogs
                        .Where(log => log.RequestTimestamp < request.OlderThan && !log.IsDeleted);
                    
                    recordsToDelete += await apiLogsQuery.CountAsync();
                    result.Details.Add($"API Request Logs: {await apiLogsQuery.CountAsync()} records");

                    if (!request.DryRun)
                    {
                        // Soft delete API request logs
                        var apiLogs = await apiLogsQuery.ToListAsync();
                        foreach (var log in apiLogs)
                        {
                            log.IsDeleted = true;
                            log.DeletedAt = DateTime.UtcNow;
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                result.RecordsDeleted = recordsToDelete;
                result.Success = true;
                result.Message = request.DryRun 
                    ? $"Dry run completed. {recordsToDelete} records would be deleted."
                    : $"Successfully deleted {recordsToDelete} log records.";

                _logger.LogInformation("Log cleanup {Action}: {RecordsDeleted} records {DryRun}", 
                    request.DryRun ? "simulated" : "completed", recordsToDelete,
                    request.DryRun ? "(dry run)" : "");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during log cleanup");
                return StatusCode(500, new LogCleanupResultDto
                {
                    Success = false,
                    Message = $"Log cleanup failed: {ex.Message}"
                });
            }
        }

        private async Task<List<object>> GetApiRequestLogsForExport(LogExportDto request)
        {
            var query = _context.ApiRequestLogs
                .Include(log => log.ApiConfiguration)
                .Include(log => log.GeneratedScenario)
                .Include(log => log.BetaScenario)
                .Where(log => !log.IsDeleted);

            if (request.StartDate.HasValue)
                query = query.Where(log => log.RequestTimestamp >= request.StartDate.Value);

            if (request.EndDate.HasValue)
                query = query.Where(log => log.RequestTimestamp <= request.EndDate.Value);

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(log => 
                    log.RequestPayload.Contains(request.SearchTerm) ||
                    log.ErrorMessage!.Contains(request.SearchTerm) ||
                    log.ApiConfiguration.Name.Contains(request.SearchTerm));
            }

            var logs = await query
                .OrderByDescending(log => log.RequestTimestamp)
                .Take(request.MaxRecords)
                .ToListAsync();

            return logs.Cast<object>().ToList();
        }

        private List<object> GetSystemLogsForExport(LogExportDto request)
        {
            // Placeholder for system logs - in a real implementation, you would fetch from your logging system
            var systemLogs = new List<object>
            {
                new
                {
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    Level = "Information",
                    Category = "Application",
                    Message = "Application started successfully",
                    Properties = new Dictionary<string, object>()
                },
                new
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-30),
                    Level = "Warning",
                    Category = "Database",
                    Message = "Database connection pool near capacity",
                    Properties = new Dictionary<string, object> { ["ConnectionCount"] = 85 }
                }
            };

            return systemLogs.Where(log => 
                (request.StartDate == null || ((dynamic)log).Timestamp >= request.StartDate) &&
                (request.EndDate == null || ((dynamic)log).Timestamp <= request.EndDate) &&
                (string.IsNullOrEmpty(request.SearchTerm) || 
                 ((dynamic)log).Message.Contains(request.SearchTerm)))
                .Take(request.MaxRecords)
                .ToList();
        }

        private (string data, string fileName, string contentType) FormatLogsForExport(
            List<object> logs, LogExportDto request, string filePrefix)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            
            switch (request.Format.ToLower())
            {
                case "json":
                    return (
                        JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }),
                        $"{filePrefix}_{timestamp}.json",
                        "application/json"
                    );

                case "csv":
                    var csv = ConvertToCsv(logs);
                    return (
                        csv,
                        $"{filePrefix}_{timestamp}.csv",
                        "text/csv"
                    );

                case "txt":
                    var txt = ConvertToText(logs);
                    return (
                        txt,
                        $"{filePrefix}_{timestamp}.txt",
                        "text/plain"
                    );

                default:
                    throw new ArgumentException($"Unsupported export format: {request.Format}");
            }
        }

        private string ConvertToCsv(List<object> logs)
        {
            if (!logs.Any()) return "";

            var sb = new StringBuilder();
            var first = logs.First();
            
            // Get properties from first object for headers
            var properties = first.GetType().GetProperties();
            sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            foreach (var log in logs)
            {
                var values = properties.Select(p => 
                {
                    var value = p.GetValue(log)?.ToString() ?? "";
                    return $"\"{value.Replace("\"", "\"\"")}\""; // Escape quotes
                });
                sb.AppendLine(string.Join(",", values));
            }

            return sb.ToString();
        }

        private string ConvertToText(List<object> logs)
        {
            var sb = new StringBuilder();
            
            foreach (var log in logs)
            {
                sb.AppendLine("=" + new string('=', 50));
                var properties = log.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(log);
                    sb.AppendLine($"{prop.Name}: {value}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private List<LogTrendPoint> GenerateHourlyTrends(List<ApiRequestLog> logs, DateTime start, DateTime end)
        {
            var trends = new List<LogTrendPoint>();
            var current = start.Date.AddHours(start.Hour); // Round down to hour

            while (current <= end)
            {
                var hourLogs = logs.Where(l => l.RequestTimestamp >= current && l.RequestTimestamp < current.AddHours(1));
                
                trends.Add(new LogTrendPoint
                {
                    Timestamp = current,
                    Count = hourLogs.Count(),
                    ErrorCount = hourLogs.Count(l => !l.IsSuccessful),
                    AverageResponseTime = hourLogs.Any() ? hourLogs.Average(l => l.ResponseTimeMs) : 0
                });

                current = current.AddHours(1);
            }

            return trends;
        }
    }
}
