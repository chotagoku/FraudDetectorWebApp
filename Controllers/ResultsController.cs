using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(ApplicationDbContext context, ILogger<ResultsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllResults(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? configurationId = null)
        {
            var query = _context.ApiRequestLogs.AsQueryable();

            if(configurationId.HasValue)
            {
                query = query.Where(l => l.ApiConfigurationId == configurationId.Value);
            }

            var totalCount = await query.CountAsync();

            var results = await query
                .OrderByDescending(l => l.RequestTimestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(
                    l => new
                    {
                        l.Id,
                        l.ApiConfigurationId,
                        l.RequestPayload,
                        l.ResponseContent,
                        l.ResponseTimeMs,
                        l.IsSuccessful,
                        l.StatusCode,
                        l.ErrorMessage,
                        l.RequestTimestamp,
                        l.IterationNumber
                    })
                .ToListAsync();

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return results;
        }

        [HttpGet("configuration/{configurationId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetResultsByConfiguration(
            int configurationId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var configuration = await _context.ApiConfigurations.FindAsync(configurationId);
            if(configuration == null)
                return NotFound("Configuration not found");

            var totalCount = await _context.ApiRequestLogs
                .Where(l => l.ApiConfigurationId == configurationId)
                .CountAsync();

            var results = await _context.ApiRequestLogs
                .Where(l => l.ApiConfigurationId == configurationId)
                .OrderByDescending(l => l.RequestTimestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(
                    l => new
                    {
                        l.Id,
                        l.ApiConfigurationId,
                        l.RequestPayload,
                        l.ResponseContent,
                        l.ResponseTimeMs,
                        l.IsSuccessful,
                        l.StatusCode,
                        l.ErrorMessage,
                        l.RequestTimestamp,
                        l.IterationNumber
                    })
                .ToListAsync();

            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return results;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetResult(int id)
        {
            var result = await _context.ApiRequestLogs
                .Where(l => l.Id == id)
                .Select(
                    l => new
                    {
                        l.Id,
                        l.ApiConfigurationId,
                        l.RequestPayload,
                        l.ResponseContent,
                        l.ResponseTimeMs,
                        l.IsSuccessful,
                        l.StatusCode,
                        l.ErrorMessage,
                        l.RequestTimestamp,
                        l.IterationNumber,
                        ApiConfigurationName = l.ApiConfiguration.Name
                    })
                .FirstOrDefaultAsync();

            if(result == null)
                return NotFound();

            return result;
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            var totalRequests = await _context.ApiRequestLogs.CountAsync();
            var successfulRequests = await _context.ApiRequestLogs.CountAsync(l => l.IsSuccessful);
            var failedRequests = totalRequests - successfulRequests;

            var averageResponseTime = await _context.ApiRequestLogs
                    .Where(l => l.IsSuccessful)
                    .AverageAsync(l => (double?)l.ResponseTimeMs) ??
                0;

            var configurations = await _context.ApiConfigurations.CountAsync();
            var activeConfigurations = await _context.ApiConfigurations.CountAsync(c => c.IsActive);

            var recentResults = await _context.ApiRequestLogs
                .OrderByDescending(l => l.RequestTimestamp)
                .Take(10)
                .Select(
                    l => new
                    {
                        l.Id,
                        Name = l.ApiConfiguration.Name,
                        l.ResponseTimeMs,
                        l.IsSuccessful,
                        l.StatusCode,
                        l.RequestTimestamp,
                        l.IterationNumber
                    })
                .ToListAsync();

            return Ok(
                new
                {
                    totalRequests,
                    successfulRequests,
                    failedRequests,
                    averageResponseTime = Math.Round(averageResponseTime, 2),
                    configurations,
                    activeConfigurations,
                    recentResults
                });
        }

        [HttpGet("statistics/configuration/{configurationId}")]
        public async Task<ActionResult<object>> GetConfigurationStatistics(int configurationId)
        {
            var configuration = await _context.ApiConfigurations.FindAsync(configurationId);
            if(configuration == null)
                return NotFound("Configuration not found");

            var logs = await _context.ApiRequestLogs.Where(l => l.ApiConfigurationId == configurationId).ToListAsync();

            var totalRequests = logs.Count;
            var successfulRequests = logs.Count(l => l.IsSuccessful);
            var failedRequests = totalRequests - successfulRequests;

            var averageResponseTime = logs.Where(l => l.IsSuccessful).Average(l => (double?)l.ResponseTimeMs) ?? 0;

            var minResponseTime = logs.Where(l => l.IsSuccessful).Min(l => (long?)l.ResponseTimeMs) ?? 0;

            var maxResponseTime = logs.Where(l => l.IsSuccessful).Max(l => (long?)l.ResponseTimeMs) ?? 0;

            var lastRequest = logs.OrderByDescending(l => l.RequestTimestamp).FirstOrDefault();

            return Ok(
                new
                {
                    configurationName = configuration.Name,
                    totalRequests,
                    successfulRequests,
                    failedRequests,
                    successRate = totalRequests > 0
                        ? Math.Round((double)successfulRequests / totalRequests * 100, 2)
                        : 0,
                    averageResponseTime = Math.Round(averageResponseTime, 2),
                    minResponseTime,
                    maxResponseTime,
                    lastRequestTime = lastRequest?.RequestTimestamp,
                    currentIteration = lastRequest?.IterationNumber ?? 0
                });
        }

        [HttpDelete("configuration/{configurationId}")]
        public async Task<IActionResult> DeleteConfigurationResults(int configurationId)
        {
            var configuration = await _context.ApiConfigurations.FindAsync(configurationId);
            if(configuration == null)
                return NotFound("Configuration not found");

            var logs = await _context.ApiRequestLogs.Where(l => l.ApiConfigurationId == configurationId).ToListAsync();

            _context.ApiRequestLogs.RemoveRange(logs);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Deleted {logs.Count} request logs for configuration {configuration.Name}" });
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteAllResults()
        {
            var allLogs = await _context.ApiRequestLogs.ToListAsync();
            _context.ApiRequestLogs.RemoveRange(allLogs);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Deleted {allLogs.Count} request logs" });
        }
        
        [HttpGet("combined")]
        public async Task<ActionResult<IEnumerable<object>>> GetCombinedResults(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] int? configurationId = null)
        {
            // Get API request logs
            var logsQuery = _context.ApiRequestLogs.AsQueryable();
            if (configurationId.HasValue)
            {
                logsQuery = logsQuery.Where(l => l.ApiConfigurationId == configurationId.Value);
            }
            
            var apiLogs = await logsQuery
                .OrderByDescending(l => l.RequestTimestamp)
                .Take(pageSize * 10) // Get more to combine with scenarios
                .ToListAsync();
                
            // Get generated scenarios  
            var scenariosQuery = _context.GeneratedScenarios.AsQueryable();
            if (configurationId.HasValue)
            {
                scenariosQuery = scenariosQuery.Where(s => s.ConfigurationId == configurationId.Value);
            }
            
            var scenarios = await scenariosQuery
                .OrderByDescending(s => s.GeneratedAt)
                .Take(pageSize * 10)
                .ToListAsync();
                
            // Combine and create unified results
            var combinedResults = new List<object>();
            
            // Add API test results
            foreach (var log in apiLogs)
            {
                // Try to extract scenario data from the request payload
                string fromName = "Unknown", toName = "Unknown", riskLevel = "unknown";
                decimal amount = 0;
                
                try
                {
                    var requestData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(log.RequestPayload);
                    // Parse the content if it's in the expected format
                    var payloadStr = log.RequestPayload;
                    if (payloadStr.Contains("FromName:"))
                    {
                        var fromMatch = System.Text.RegularExpressions.Regex.Match(payloadStr, @"FromName: ([^\n]+)");
                        if (fromMatch.Success) fromName = fromMatch.Groups[1].Value;
                        
                        var toMatch = System.Text.RegularExpressions.Regex.Match(payloadStr, @"ToName: ([^\n]+)");
                        if (toMatch.Success) toName = toMatch.Groups[1].Value;
                        
                        var amountMatch = System.Text.RegularExpressions.Regex.Match(payloadStr, @"Amount: (\d+)");
                        if (amountMatch.Success) decimal.TryParse(amountMatch.Groups[1].Value, out amount);
                        
                        var riskMatch = System.Text.RegularExpressions.Regex.Match(payloadStr, @"Amount Risk Score: (\d+)");
                        if (riskMatch.Success)
                        {
                            var score = int.Parse(riskMatch.Groups[1].Value);
                            riskLevel = score <= 3 ? "low" : score <= 6 ? "medium" : "high";
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Could not parse request payload for log {LogId}: {Error}", log.Id, ex.Message);
                }
                
                combinedResults.Add(new
                {
                    id = log.Id,
                    type = "api_test",
                    iterationNumber = log.IterationNumber,
                    requestTimestamp = log.RequestTimestamp,
                    responseTimeMs = log.ResponseTimeMs,
                    isSuccessful = log.IsSuccessful,
                    isTested = true,
                    statusCode = log.StatusCode,
                    requestPayload = log.RequestPayload,
                    responseContent = log.ResponseContent,
                    errorMessage = log.ErrorMessage,
                    configurationId = log.ApiConfigurationId,
                    // Extracted data from payload
                    fromName = fromName,
                    toName = toName,
                    amount = amount,
                    riskLevel = riskLevel,
                    generatedScenarioId = log.GeneratedScenarioId
                });
            }
            
            // Add untested scenarios
            foreach (var scenario in scenarios)
            {
                // Only add scenarios that don't have a linked API test
                var hasApiTest = apiLogs.Any(l => l.GeneratedScenarioId == scenario.Id);
                if (!hasApiTest)
                {
                    combinedResults.Add(new
                    {
                        id = scenario.Id,
                        type = "scenario", 
                        iterationNumber = scenario.Id,
                        requestTimestamp = scenario.GeneratedAt,
                        responseTimeMs = scenario.ResponseTimeMs ?? 0,
                        isSuccessful = scenario.TestSuccessful ?? false,
                        isTested = scenario.IsTested,
                        statusCode = scenario.LastStatusCode ?? 0,
                        requestPayload = scenario.ScenarioJson,
                        responseContent = scenario.TestResponse,
                        errorMessage = scenario.TestErrorMessage,
                        configurationId = scenario.ConfigurationId,
                        // Scenario data
                        fromName = scenario.FromName,
                        toName = scenario.ToName,
                        amount = scenario.Amount,
                        riskLevel = scenario.RiskLevel,
                        generatedScenarioId = scenario.Id
                    });
                }
            }
            
            // Sort combined results by timestamp and take the requested page
            var sortedResults = combinedResults
                .OrderByDescending(r => (DateTime)r.GetType().GetProperty("requestTimestamp").GetValue(r))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
                
            var totalCount = combinedResults.Count;
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(sortedResults);
        }
    }
}
