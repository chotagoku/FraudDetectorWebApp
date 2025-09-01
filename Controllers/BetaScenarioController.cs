using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.DTOs;
using FraudDetectorWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BetaScenarioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BetaScenarioController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public BetaScenarioController(
            ApplicationDbContext context,
            ILogger<BetaScenarioController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ApiResponseDto<BetaScenarioResponseDto>>> GenerateBetaScenario(
            [FromBody] BetaScenarioRequestDto request)
        {
            try
            {
                _logger.LogInformation("Generating beta scenario: {Name}", request.Name);

                // Get database data if requested
                var dbData = await GetDatabaseDataForGeneration(request.UseDatabaseData);

                // Generate the comprehensive scenario
                var betaScenario = await GenerateScenarioFromUserInput(request, dbData);

                // Save to database
                _context.BetaScenarios.Add(betaScenario);
                await _context.SaveChangesAsync();

                var response = MapToResponseDto(betaScenario);

                return Ok(new ApiResponseDto<BetaScenarioResponseDto>
                {
                    Success = true,
                    Message = "Beta scenario generated successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating beta scenario: {Name}", request.Name);
                return StatusCode(500, new ApiResponseDto<BetaScenarioResponseDto>
                {
                    Success = false,
                    Message = "Error generating beta scenario",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("bulk-generate")]
        public async Task<ActionResult<ApiResponseDto<object>>> BulkGenerateBetaScenarios(
            [FromBody] BetaScenarioBulkRequestDto request)
        {
            try
            {
                _logger.LogInformation("Bulk generating {Count} beta scenarios", request.Count);

                var scenarios = new List<BetaScenario>();
                var dbData = await GetDatabaseDataForGeneration(request.UseDatabaseData);

                for (int i = 0; i < request.Count; i++)
                {
                    var scenarioRequest = new BetaScenarioRequestDto
                    {
                        Name = $"{request.BaseStory.Split(' ').FirstOrDefault()} Scenario {i + 1}",
                        Description = $"Generated scenario {i + 1} based on: {request.BaseStory}",
                        UserStory = request.VariateStories ? VariateStory(request.BaseStory, i) : request.BaseStory,
                        Conditions = request.Conditions,
                        RiskLevel = request.RiskLevel == "mixed" ? GetRandomRiskLevel() : request.RiskLevel,
                        Category = request.Category,
                        UseDatabaseData = request.UseDatabaseData,
                        AutoGenerateWatchlists = request.AutoGenerateWatchlists,
                        ConfigurationId = request.ConfigurationId,
                        GeneratedBy = request.GeneratedBy
                    };

                    var scenario = await GenerateScenarioFromUserInput(scenarioRequest, dbData);
                    scenarios.Add(scenario);
                }

                if (request.SaveToDatabase)
                {
                    _context.BetaScenarios.AddRange(scenarios);
                    await _context.SaveChangesAsync();
                }

                var responses = scenarios.Select(MapToResponseDto).ToList();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"Successfully generated {scenarios.Count} beta scenarios",
                    Data = new { scenarios = responses, totalGenerated = scenarios.Count }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk generating beta scenarios");
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Error bulk generating beta scenarios",
                    Error = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BetaScenarioResponseDto>>> GetBetaScenarios(
            [FromQuery] string? riskLevel = null,
            [FromQuery] string? category = null,
            [FromQuery] string? status = null,
            [FromQuery] bool? isFavorite = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var query = _context.BetaScenarios.AsQueryable();

            if (!string.IsNullOrEmpty(riskLevel))
                query = query.Where(s => s.RiskLevel == riskLevel);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category == category);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            if (isFavorite.HasValue)
                query = query.Where(s => s.IsFavorite == isFavorite.Value);

            var total = await query.CountAsync();
            var scenarios = await query
                .OrderByDescending(s => s.GeneratedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responses = scenarios.Select(MapToResponseDto).ToList();

            Response.Headers["X-Total-Count"] = total.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(responses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BetaScenarioResponseDto>> GetBetaScenario(int id)
        {
            var scenario = await _context.BetaScenarios
                .Include(s => s.Configuration)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (scenario == null)
                return NotFound();

            return Ok(MapToResponseDto(scenario));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BetaScenarioResponseDto>> UpdateBetaScenario(
            int id, [FromBody] BetaScenarioUpdateDto request)
        {
            var scenario = await _context.BetaScenarios.FindAsync(id);
            if (scenario == null)
                return NotFound();

            // Update properties if provided
            if (request.Name != null) scenario.Name = request.Name;
            if (request.Description != null) scenario.Description = request.Description;
            if (request.UserStory != null) scenario.UserStory = request.UserStory;
            if (request.Conditions != null) scenario.Conditions = request.Conditions;
            if (request.RiskLevel != null) scenario.RiskLevel = request.RiskLevel;
            if (request.Category != null) scenario.Category = request.Category;
            if (request.BusinessType != null) scenario.BusinessType = request.BusinessType;
            if (request.CustomerSegment != null) scenario.CustomerSegment = request.CustomerSegment;
            if (request.Tags != null) scenario.Tags = request.Tags;
            if (request.Priority.HasValue) scenario.Priority = request.Priority.Value;
            if (request.Status != null) scenario.Status = request.Status;
            if (request.Notes != null) scenario.Notes = request.Notes;
            if (request.IsFavorite.HasValue) scenario.IsFavorite = request.IsFavorite.Value;

            await _context.SaveChangesAsync();

            return Ok(MapToResponseDto(scenario));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteBetaScenario(int id)
        {
            var scenario = await _context.BetaScenarios.FindAsync(id);
            if (scenario == null)
                return NotFound();

            // Soft delete
            scenario.IsDeleted = true;
            scenario.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Beta scenario deleted successfully" });
        }

        [HttpPost("{id}/test")]
        public async Task<ActionResult<ApiResponseDto<object>>> TestBetaScenario(
            int id, [FromBody] BetaScenarioTestRequestDto request)
        {
            var scenario = await _context.BetaScenarios.FindAsync(id);
            if (scenario == null)
                return NotFound();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var httpClient = _httpClientFactory.CreateClient();

                var content = new StringContent(scenario.ScenarioJson, Encoding.UTF8, "application/json");

                if (!string.IsNullOrEmpty(request.BearerToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.BearerToken);
                }

                var response = await httpClient.PostAsync(request.ApiEndpoint, content);
                stopwatch.Stop();

                if (request.UpdateScenarioWithResults)
                {
                    scenario.IsTested = true;
                    scenario.TestedAt = DateTime.UtcNow;
                    scenario.LastTestedAt = DateTime.UtcNow;
                    scenario.ApiEndpoint = request.ApiEndpoint;
                    scenario.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                    scenario.TestSuccessful = response.IsSuccessStatusCode;
                    scenario.LastStatusCode = (int)response.StatusCode;
                    scenario.TestCount++;

                    if (response.IsSuccessStatusCode)
                    {
                        scenario.TestResponse = await response.Content.ReadAsStringAsync();
                        if (scenario.Status == "draft") scenario.Status = "tested";
                    }
                    else
                    {
                        scenario.TestErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                    }

                    await _context.SaveChangesAsync();
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Beta scenario tested successfully",
                    Data = new
                    {
                        id = scenario.Id,
                        success = response.IsSuccessStatusCode,
                        responseTime = stopwatch.ElapsedMilliseconds,
                        statusCode = (int)response.StatusCode,
                        response = response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null,
                        error = !response.IsSuccessStatusCode ? $"HTTP {response.StatusCode}: {response.ReasonPhrase}" : null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing beta scenario {ScenarioId}", id);

                if (request.UpdateScenarioWithResults)
                {
                    scenario.IsTested = true;
                    scenario.TestedAt = DateTime.UtcNow;
                    scenario.LastTestedAt = DateTime.UtcNow;
                    scenario.TestSuccessful = false;
                    scenario.TestErrorMessage = ex.Message;
                    scenario.TestCount++;
                    await _context.SaveChangesAsync();
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Error testing beta scenario",
                    Error = ex.Message
                });
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetBetaScenarioStatistics()
        {
            var stats = await _context.BetaScenarios
                .GroupBy(s => 1)
                .Select(g => new
                {
                    totalGenerated = g.Count(),
                    totalTested = g.Count(s => s.IsTested),
                    successfulTests = g.Count(s => s.TestSuccessful == true),
                    averageResponseTime = g.Where(s => s.ResponseTimeMs.HasValue).Average(s => s.ResponseTimeMs) ?? 0,
                    riskDistribution = new
                    {
                        low = g.Count(s => s.RiskLevel == "low"),
                        medium = g.Count(s => s.RiskLevel == "medium"),
                        high = g.Count(s => s.RiskLevel == "high"),
                        critical = g.Count(s => s.RiskLevel == "critical")
                    },
                    statusDistribution = new
                    {
                        draft = g.Count(s => s.Status == "draft"),
                        ready = g.Count(s => s.Status == "ready"),
                        tested = g.Count(s => s.Status == "tested"),
                        validated = g.Count(s => s.Status == "validated"),
                        archived = g.Count(s => s.Status == "archived")
                    },
                    favorites = g.Count(s => s.IsFavorite),
                    usedDatabaseData = g.Count(s => s.UsedDatabaseData)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                return Ok(new
                {
                    totalGenerated = 0,
                    totalTested = 0,
                    successfulTests = 0,
                    averageResponseTime = 0.0,
                    riskDistribution = new { low = 0, medium = 0, high = 0, critical = 0 },
                    statusDistribution = new { draft = 0, ready = 0, tested = 0, validated = 0, archived = 0 },
                    favorites = 0,
                    usedDatabaseData = 0
                });
            }

            return Ok(stats);
        }

        // Private helper methods
        private async Task<BetaScenario> GenerateScenarioFromUserInput(BetaScenarioRequestDto request, object? dbData)
        {
            // Generate comprehensive story based on user input
            var generatedStory = await GenerateComprehensiveStory(request, dbData);

            // Generate transaction-specific narrative
            var transactionStory = await GenerateTransactionStory(request, generatedStory, dbData);

            // Extract and generate transaction details
            var transactionDetails = GenerateTransactionDetails(request, dbData);

            // Generate risk scores and flags
            var riskScoring = GenerateRiskScoring(request, transactionDetails);

            // Generate watchlist indicators
            var watchlistFlags = GenerateWatchlistFlags(request, transactionDetails);

            // Create the API request JSON
            var scenarioJson = GenerateScenarioJson(transactionDetails, riskScoring, watchlistFlags);

            var betaScenario = new BetaScenario
            {
                Name = request.Name,
                Description = request.Description ?? $"Beta scenario: {request.UserStory}",
                UserStory = request.UserStory,
                GeneratedStory = generatedStory,
                TransactionStory = transactionStory,
                ScenarioJson = scenarioJson,
                RiskLevel = request.RiskLevel,
                Category = request.Category ?? "general",
                Conditions = request.Conditions ?? "",
                
                // Profile info
                UserProfile = transactionDetails.UserProfile,
                BusinessType = request.BusinessType ?? "",
                CustomerSegment = request.CustomerSegment ?? "",
                
                // Transaction details
                FromName = transactionDetails.FromName,
                ToName = transactionDetails.ToName,
                FromAccount = transactionDetails.FromAccount,
                ToAccount = transactionDetails.ToAccount,
                Amount = transactionDetails.Amount,
                Currency = request.PreferredCurrency ?? "PKR",
                ActivityCode = transactionDetails.ActivityCode,
                UserType = transactionDetails.UserType,
                
                // Risk scores
                AmountRiskScore = riskScoring.AmountRiskScore,
                AmountZScore = riskScoring.AmountZScore,
                FraudScore = riskScoring.FraudScore,
                ComplianceScore = riskScoring.ComplianceScore,
                
                // Flags
                HighAmountFlag = riskScoring.HighAmountFlag,
                SuspiciousActivityFlag = riskScoring.SuspiciousActivityFlag,
                ComplianceFlag = riskScoring.ComplianceFlag,
                AMLFlag = riskScoring.AMLFlag,
                CTFFlag = riskScoring.CTFFlag,
                
                // Context flags
                NewActivityCode = riskScoring.NewActivityCode,
                NewFromAccount = riskScoring.NewFromAccount,
                NewToAccount = riskScoring.NewToAccount,
                NewToCity = riskScoring.NewToCity,
                OutsideUsualDay = riskScoring.OutsideUsualDay,
                OfficeHours = DateTime.UtcNow.Hour >= 9 && DateTime.UtcNow.Hour <= 17,
                
                // Watchlist flags
                WatchlistFromAccount = watchlistFlags.FromAccount,
                WatchlistFromName = watchlistFlags.FromName,
                WatchlistToAccount = watchlistFlags.ToAccount,
                WatchlistToName = watchlistFlags.ToName,
                WatchlistToBank = watchlistFlags.ToBank,
                WatchlistIPAddress = watchlistFlags.IPAddress,
                WatchlistCNIC = watchlistFlags.CNIC,
                WatchlistPhoneNumber = watchlistFlags.PhoneNumber,
                
                // Additional context
                CNIC = transactionDetails.CNIC,
                PhoneNumber = transactionDetails.PhoneNumber,
                IPAddress = transactionDetails.IPAddress,
                DeviceId = transactionDetails.DeviceId,
                Location = transactionDetails.Location,
                TransactionId = transactionDetails.TransactionId,
                UserId = transactionDetails.UserId,
                TransactionDateTime = transactionDetails.TransactionDateTime,
                TransactionComments = transactionDetails.TransactionComments,
                ToBank = transactionDetails.ToBank,
                
                // Generation metadata
                GenerationPrompt = JsonSerializer.Serialize(request),
                GenerationEngine = "Beta-GPT-Enhanced",
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = request.GeneratedBy,
                
                // Management
                Tags = request.Tags ?? "",
                Priority = request.Priority,
                Status = "draft",
                
                // Database integration
                UsedDatabaseData = request.UseDatabaseData,
                SourceDataSummary = dbData != null ? "Incorporated existing transaction patterns and user profiles" : "No database data used",
                
                ConfigurationId = request.ConfigurationId
            };

            return betaScenario;
        }

        private async Task<object?> GetDatabaseDataForGeneration(bool useDatabaseData)
        {
            if (!useDatabaseData) return null;

            try
            {
                var recentScenarios = await _context.GeneratedScenarios
                    .Where(s => s.GeneratedAt > DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(s => s.GeneratedAt)
                    .Take(50)
                    .Select(s => new { s.FromName, s.ToName, s.Amount, s.RiskLevel, s.ActivityCode, s.UserProfile })
                    .ToListAsync();

                var recentLogs = await _context.ApiRequestLogs
                    .Where(l => l.RequestTimestamp > DateTime.UtcNow.AddDays(-7))
                    .Take(100)
                    .Select(l => new { l.RequestPayload, l.IsSuccessful, l.StatusCode })
                    .ToListAsync();

                return new { scenarios = recentScenarios, logs = recentLogs };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch database data for generation");
                return null;
            }
        }

        private async Task<string> GenerateComprehensiveStory(BetaScenarioRequestDto request, object? dbData)
        {
            // In a real implementation, this would call an AI service
            // For now, we'll create a comprehensive story based on the user input and patterns
            
            var storyElements = new List<string>();
            
            storyElements.Add($"Scenario: {request.UserStory}");
            
            if (!string.IsNullOrEmpty(request.Conditions))
            {
                storyElements.Add($"Conditions: {request.Conditions}");
            }
            
            if (!string.IsNullOrEmpty(request.BusinessType))
            {
                storyElements.Add($"Business Context: This involves a {request.BusinessType} entity");
            }
            
            if (!string.IsNullOrEmpty(request.CustomerSegment))
            {
                storyElements.Add($"Customer Profile: {request.CustomerSegment} customer segment");
            }

            // Add risk context based on level
            var riskContext = request.RiskLevel switch
            {
                "low" => "This is a routine transaction with minimal risk indicators",
                "medium" => "This transaction shows some suspicious patterns that warrant attention",
                "high" => "This transaction exhibits multiple red flags requiring investigation",
                "critical" => "This transaction shows severe fraud indicators requiring immediate action",
                _ => "This transaction requires standard risk assessment"
            };
            storyElements.Add($"Risk Assessment: {riskContext}");
            
            return string.Join(". ", storyElements) + ".";
        }

        private async Task<string> GenerateTransactionStory(BetaScenarioRequestDto request, string generatedStory, object? dbData)
        {
            // Generate a transaction-focused narrative
            var transactionElements = new List<string>();
            
            transactionElements.Add("Transaction Narrative:");
            transactionElements.Add($"Based on the scenario '{request.UserStory}', this transaction represents a typical {request.RiskLevel}-risk financial movement.");
            
            if (request.SuggestedAmount.HasValue)
            {
                transactionElements.Add($"The transaction amount of {request.SuggestedAmount:C} PKR is {GetAmountContext(request.SuggestedAmount.Value)}");
            }
            
            transactionElements.Add($"This type of transaction is commonly associated with {request.Category ?? "general financial activity"}.");
            
            return string.Join(" ", transactionElements);
        }

        private string GetAmountContext(decimal amount)
        {
            return amount switch
            {
                < 10000 => "considered a small retail transaction",
                < 100000 => "typical for individual transactions",
                < 1000000 => "substantial, requiring enhanced due diligence",
                _ => "high-value, subject to strict monitoring requirements"
            };
        }

        // Helper classes for transaction generation
        private class TransactionDetails
        {
            public string UserProfile { get; set; } = "";
            public string FromName { get; set; } = "";
            public string ToName { get; set; } = "";
            public string FromAccount { get; set; } = "";
            public string ToAccount { get; set; } = "";
            public decimal Amount { get; set; }
            public string ActivityCode { get; set; } = "";
            public string UserType { get; set; } = "";
            public string CNIC { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string IPAddress { get; set; } = "";
            public string DeviceId { get; set; } = "";
            public string Location { get; set; } = "";
            public string TransactionId { get; set; } = "";
            public string UserId { get; set; } = "";
            public DateTime TransactionDateTime { get; set; }
            public string TransactionComments { get; set; } = "";
            public string ToBank { get; set; } = "";
        }

        private class RiskScoring
        {
            public int AmountRiskScore { get; set; }
            public decimal AmountZScore { get; set; }
            public int FraudScore { get; set; }
            public int ComplianceScore { get; set; }
            public bool HighAmountFlag { get; set; }
            public bool SuspiciousActivityFlag { get; set; }
            public bool ComplianceFlag { get; set; }
            public bool AMLFlag { get; set; }
            public bool CTFFlag { get; set; }
            public bool NewActivityCode { get; set; }
            public bool NewFromAccount { get; set; }
            public bool NewToAccount { get; set; }
            public bool NewToCity { get; set; }
            public bool OutsideUsualDay { get; set; }
        }

        private class WatchlistFlags
        {
            public bool FromAccount { get; set; }
            public bool FromName { get; set; }
            public bool ToAccount { get; set; }
            public bool ToName { get; set; }
            public bool ToBank { get; set; }
            public bool IPAddress { get; set; }
            public bool CNIC { get; set; }
            public bool PhoneNumber { get; set; }
        }

        private TransactionDetails GenerateTransactionDetails(BetaScenarioRequestDto request, object? dbData)
        {
            var random = new Random();
            
            return new TransactionDetails
            {
                UserProfile = GenerateUserProfile(request),
                FromName = GenerateFromName(),
                ToName = GenerateToName(),
                FromAccount = GenerateAccount().ToString(),
                ToAccount = GenerateIBAN(),
                Amount = request.SuggestedAmount ?? GenerateAmount(request.RiskLevel),
                ActivityCode = GenerateActivityCode(),
                UserType = GenerateUserType(),
                CNIC = GenerateCNIC(),
                PhoneNumber = GeneratePhoneNumber(),
                IPAddress = GenerateIPAddress(),
                DeviceId = GenerateDeviceId(),
                Location = GenerateLocation(),
                TransactionId = GenerateTransactionId(),
                UserId = GenerateUserId(),
                TransactionDateTime = GenerateTransactionDateTime(),
                TransactionComments = GenerateTransactionComments(request),
                ToBank = GenerateBank()
            };
        }

        private RiskScoring GenerateRiskScoring(BetaScenarioRequestDto request, TransactionDetails details)
        {
            var random = new Random();
            
            var baseRisk = request.RiskLevel switch
            {
                "low" => random.Next(1, 4),
                "medium" => random.Next(3, 7),
                "high" => random.Next(6, 9),
                "critical" => random.Next(8, 11),
                _ => random.Next(1, 11)
            };

            return new RiskScoring
            {
                AmountRiskScore = baseRisk,
                AmountZScore = (decimal)(random.NextDouble() * 3.0),
                FraudScore = Math.Min(baseRisk * 10 + random.Next(-10, 20), 100),
                ComplianceScore = Math.Min(baseRisk * 8 + random.Next(-15, 25), 100),
                HighAmountFlag = details.Amount > 500000,
                SuspiciousActivityFlag = baseRisk >= 7,
                ComplianceFlag = baseRisk >= 6,
                AMLFlag = baseRisk >= 8,
                CTFFlag = baseRisk >= 9,
                NewActivityCode = random.NextDouble() > 0.7,
                NewFromAccount = random.NextDouble() > 0.8,
                NewToAccount = random.NextDouble() > 0.6,
                NewToCity = random.NextDouble() > 0.7,
                OutsideUsualDay = DateTime.UtcNow.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            };
        }

        private WatchlistFlags GenerateWatchlistFlags(BetaScenarioRequestDto request, TransactionDetails details)
        {
            var random = new Random();
            var baseProb = request.RiskLevel switch
            {
                "critical" => 0.4,
                "high" => 0.25,
                "medium" => 0.1,
                "low" => 0.05,
                _ => 0.1
            };

            return new WatchlistFlags
            {
                FromAccount = random.NextDouble() < baseProb,
                FromName = random.NextDouble() < baseProb,
                ToAccount = random.NextDouble() < baseProb,
                ToName = random.NextDouble() < baseProb,
                ToBank = random.NextDouble() < baseProb * 0.7,
                IPAddress = random.NextDouble() < baseProb * 0.8,
                CNIC = random.NextDouble() < baseProb,
                PhoneNumber = random.NextDouble() < baseProb * 0.6
            };
        }

        private string GenerateScenarioJson(TransactionDetails details, RiskScoring riskScoring, WatchlistFlags watchlistFlags)
        {
            var scenarioObject = new
            {
                model = "fraud-detector:beta",
                messages = new[] { 
                    new { 
                        role = "user", 
                        content = $@"User Profile: {details.UserProfile}
Transaction Details:
- CNIC: {details.CNIC}
- FromAccount: {details.FromAccount}
- FromName: {details.FromName}
- ToAccount: {details.ToAccount}
- ToName: {details.ToName}
- ToBank: {details.ToBank}
- Amount: {details.Amount}
- ActivityCode: {details.ActivityCode}
- UserType: {details.UserType}
- DateTime: {details.TransactionDateTime:dd/MM/yyyy, HH:mm:ss}
- TransactionComments: {details.TransactionComments}

Risk Assessment:
- Amount Risk Score: {riskScoring.AmountRiskScore}
- Amount Z-Score: {riskScoring.AmountZScore:F2}
- Fraud Score: {riskScoring.FraudScore}
- Compliance Score: {riskScoring.ComplianceScore}
- High Amount Flag: {(riskScoring.HighAmountFlag ? "Yes" : "No")}
- Suspicious Activity: {(riskScoring.SuspiciousActivityFlag ? "Yes" : "No")}

Watchlist Indicators:
- FromAccount: {(watchlistFlags.FromAccount ? "Yes" : "No")}
- FromName: {(watchlistFlags.FromName ? "Yes" : "No")}
- ToAccount: {(watchlistFlags.ToAccount ? "Yes" : "No")}
- ToName: {(watchlistFlags.ToName ? "Yes" : "No")}
- CNIC: {(watchlistFlags.CNIC ? "Yes" : "No")}"
                    } 
                },
                stream = false,
                beta = true
            };

            return JsonSerializer.Serialize(scenarioObject, new JsonSerializerOptions { WriteIndented = true });
        }

        private BetaScenarioResponseDto MapToResponseDto(BetaScenario scenario)
        {
            return new BetaScenarioResponseDto
            {
                Id = scenario.Id,
                Name = scenario.Name,
                Description = scenario.Description,
                UserStory = scenario.UserStory,
                GeneratedStory = scenario.GeneratedStory,
                TransactionStory = scenario.TransactionStory,
                ScenarioJson = JsonSerializer.Deserialize<object>(scenario.ScenarioJson),
                RiskLevel = scenario.RiskLevel,
                Category = scenario.Category,
                Conditions = scenario.Conditions,
                UserProfile = scenario.UserProfile,
                BusinessType = scenario.BusinessType,
                CustomerSegment = scenario.CustomerSegment,
                FromName = scenario.FromName,
                ToName = scenario.ToName,
                Amount = scenario.Amount,
                Currency = scenario.Currency,
                ActivityCode = scenario.ActivityCode,
                AmountRiskScore = scenario.AmountRiskScore,
                AmountZScore = scenario.AmountZScore,
                FraudScore = scenario.FraudScore,
                ComplianceScore = scenario.ComplianceScore,
                HighAmountFlag = scenario.HighAmountFlag,
                SuspiciousActivityFlag = scenario.SuspiciousActivityFlag,
                ComplianceFlag = scenario.ComplianceFlag,
                GeneratedAt = scenario.GeneratedAt,
                GeneratedBy = scenario.GeneratedBy,
                IsTested = scenario.IsTested,
                TestedAt = scenario.TestedAt,
                TestSuccessful = scenario.TestSuccessful,
                Tags = string.IsNullOrEmpty(scenario.Tags) ? Array.Empty<string>() : scenario.Tags.Split(','),
                IsFavorite = scenario.IsFavorite,
                Priority = scenario.Priority,
                Status = scenario.Status,
                UsedDatabaseData = scenario.UsedDatabaseData,
                SourceDataSummary = scenario.SourceDataSummary
            };
        }

        // Helper generation methods (simplified versions)
        private string GenerateUserProfile(BetaScenarioRequestDto request)
        {
            var profiles = new[]
            {
                "Small business owner", "Individual customer", "Corporate account holder",
                "Freelancer", "Export business", "Retail merchant", "Service provider"
            };
            return profiles[new Random().Next(profiles.Length)];
        }

        private string GenerateFromName() => $"BETA-{new Random().Next(1000, 9999)} ENTERPRISES";
        private string GenerateToName() => $"BETA-RECIPIENT-{new Random().Next(100, 999)}";
        private long GenerateAccount() => new Random().NextInt64(1000000000000000, 9999999999999999);
        private string GenerateIBAN() => $"PK{new Random().Next(10, 99)}BETA{new Random().NextInt64(100000000000000, 999999999999999)}";
        private string GenerateActivityCode() => new[] { "Fund Transfer", "Bill Payment", "Salary Transfer", "Investment" }[new Random().Next(4)];
        private string GenerateUserType() => new[] { "MOBILE", "WEB", "API", "BRANCH" }[new Random().Next(4)];
        private string GenerateCNIC() => $"42101{new Random().Next(10000000, 99999999)}";
        private string GeneratePhoneNumber() => $"+92{new Random().Next(300, 399)}{new Random().Next(1000000, 9999999)}";
        private string GenerateIPAddress() => $"192.168.{new Random().Next(1, 255)}.{new Random().Next(1, 255)}";
        private string GenerateDeviceId() => Guid.NewGuid().ToString()[..8];
        private string GenerateLocation() => new[] { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Faisalabad" }[new Random().Next(5)];
        private string GenerateTransactionId() => $"BETA{DateTime.Now:yyyyMMdd}{new Random().Next(100000, 999999)}";
        private string GenerateUserId() => $"beta_user_{new Random().Next(1000, 9999)}";
        private DateTime GenerateTransactionDateTime() => DateTime.UtcNow.AddDays(-new Random().Next(0, 30)).AddHours(-new Random().Next(0, 24));
        private string GenerateTransactionComments(BetaScenarioRequestDto request) => $"Beta scenario test: {request.UserStory}";
        private string GenerateBank() => new[] { "HABBPKKA001", "MCBLPKKA001", "HBLPKKA001", "UBLPKKA007" }[new Random().Next(4)];
        
        private decimal GenerateAmount(string riskLevel)
        {
            return riskLevel switch
            {
                "low" => new Random().Next(1000, 50000),
                "medium" => new Random().Next(50000, 500000),
                "high" => new Random().Next(500000, 2000000),
                "critical" => new Random().Next(2000000, 10000000),
                _ => new Random().Next(10000, 100000)
            };
        }

        private string GetRandomRiskLevel()
        {
            var levels = new[] { "low", "medium", "high", "critical" };
            return levels[new Random().Next(levels.Length)];
        }

        private string VariateStory(string baseStory, int variation)
        {
            var variations = new[]
            {
                baseStory,
                $"{baseStory} with additional complexity",
                $"{baseStory} involving multiple parties",
                $"{baseStory} with time-sensitive elements",
                $"{baseStory} requiring enhanced verification"
            };

            return variations[variation % variations.Length];
        }
    }
}
