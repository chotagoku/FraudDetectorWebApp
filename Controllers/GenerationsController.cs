using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using FraudDetectorWebApp.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FraudDetectorWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Temporarily disabled for quick generation - TODO: implement proper auth
    public class GenerationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GenerationsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GenerationsController(
            ApplicationDbContext context,
            ILogger<GenerationsController> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetGenerations(
            [FromQuery] string? riskLevel = null,
            [FromQuery] int? configurationId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var query = _context.GeneratedScenarios.AsQueryable();

            if(!string.IsNullOrEmpty(riskLevel) && riskLevel != "mixed")
            {
                query = query.Where(g => g.RiskLevel == riskLevel);
            }
            
            if(configurationId.HasValue && configurationId.Value > 0)
            {
                query = query.Where(g => g.ConfigurationId == configurationId.Value);
            }

            var total = await query.CountAsync();
            var generations = await query
                .OrderByDescending(g => g.GeneratedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(
                    g => new
                    {
                        g.Id,
                        g.Name,
                        g.Description,
                        g.ScenarioJson,
                        g.RiskLevel,
                        g.UserProfile,
                        g.UserActivity,
                        g.AmountRiskScore,
                        g.AmountZScore,
                        g.HighAmountFlag,
                        g.HasWatchlistMatch,
                        g.FromName,
                        g.ToName,
                        g.Amount,
                        g.ActivityCode,
                        g.UserType,
                        g.GeneratedAt,
                        g.IsTested,
                        g.TestedAt,
                        g.TestResponse,
                        g.ResponseTimeMs,
                        g.TestSuccessful,
                        g.TestErrorMessage,
                        g.LastStatusCode,
                        g.ApiEndpoint,
                        g.ConfigurationId,
                        TotalCount = total
                    })
                .ToListAsync();

            return Ok(generations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetGeneration(int id)
        {
            var generation = await _context.GeneratedScenarios
                .Where(g => g.Id == id)
                .Select(
                    g => new
                    {
                        g.Id,
                        g.Name,
                        g.Description,
                        g.ScenarioJson,
                        g.RiskLevel,
                        g.UserProfile,
                        g.UserActivity,
                        g.AmountRiskScore,
                        g.AmountZScore,
                        g.HighAmountFlag,
                        g.HasWatchlistMatch,
                        g.FromName,
                        g.ToName,
                        g.Amount,
                        g.ActivityCode,
                        g.UserType,
                        g.GeneratedAt,
                        g.ApiEndpoint,
                        g.IsTested,
                        g.TestedAt,
                        g.TestResponse,
                        g.ResponseTimeMs,
                        g.TestSuccessful,
                        g.TestErrorMessage,
                        g.LastStatusCode
                    })
                .FirstOrDefaultAsync();

            if(generation == null)
                return NotFound();

            return Ok(generation);
        }

        [HttpPost("generate")]
        public async Task<ActionResult<ApiResponseDto<object>>> GenerateScenarios([FromBody] ScenarioGenerationRequestDto request)
        {
            try
            {
                _logger.LogInformation("Generating {Count} scenarios with risk focus: {RiskFocus}", request.Count, request.RiskFocus);
                
                var scenarios = new List<GeneratedScenario>();

                for(int i = 0; i < request.Count; i++)
                {
                    var scenario = GenerateSingleScenario(request.RiskFocus, request.UseDatabase);
                    scenarios.Add(scenario);
                }

                // Save to database if requested
                if(request.SaveToDatabase)
                {
                    _context.GeneratedScenarios.AddRange(scenarios);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved {Count} scenarios to database", scenarios.Count);
                }

                var scenarioResults = scenarios.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    ScenarioJson = JsonSerializer.Deserialize<object>(s.ScenarioJson),
                    s.RiskLevel,
                    s.AmountRiskScore,
                    s.Amount,
                    s.GeneratedAt
                }).ToList();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"Successfully generated {scenarios.Count} scenarios",
                    Data = new
                    {
                        scenarios = scenarioResults,
                        format = request.Format,
                        totalGenerated = scenarios.Count
                    }
                });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error generating scenarios with request: {@Request}", request);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Error generating scenarios",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("{id}/test")]
        public async Task<ActionResult<ApiResponseDto<object>>> TestScenario(int id, [FromBody] TestScenarioRequestDto request)
        {
            var scenario = await _context.GeneratedScenarios.FindAsync(id);
            if(scenario == null)
                return NotFound();

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var httpClient = _httpClientFactory.CreateClient();

                // Parse the scenario JSON to send as request
                var scenarioData = JsonSerializer.Deserialize<object>(scenario.ScenarioJson);
                var content = new StringContent(scenario.ScenarioJson, Encoding.UTF8, "application/json");

                // Add Bearer Token if provided
                if(!string.IsNullOrEmpty(request.BearerToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.BearerToken);
                }

                var response = await httpClient.PostAsync(request.ApiEndpoint, content);
                stopwatch.Stop();

                // Update scenario with test results
                scenario.IsTested = true;
                scenario.TestedAt = DateTime.UtcNow;
                scenario.ApiEndpoint = request.ApiEndpoint;
                scenario.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                scenario.TestSuccessful = response.IsSuccessStatusCode;

                if(response.IsSuccessStatusCode)
                {
                    scenario.TestResponse = await response.Content.ReadAsStringAsync();
                } else
                {
                    scenario.TestErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                }

                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        id = scenario.Id,
                        success = scenario.TestSuccessful,
                        responseTime = scenario.ResponseTimeMs,
                        statusCode = (int)response.StatusCode,
                        response = scenario.TestResponse,
                        error = scenario.TestErrorMessage
                    });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error testing scenario {ScenarioId}", id);

                scenario.IsTested = true;
                scenario.TestedAt = DateTime.UtcNow;
                scenario.TestSuccessful = false;
                scenario.TestErrorMessage = ex.Message;
                await _context.SaveChangesAsync();

                return Ok(
                    new { id = scenario.Id, success = false, responseTime = 0, statusCode = 0, error = ex.Message });
            }
        }

        [HttpGet("random")]
        public async Task<ActionResult<object>> GetRandomScenario([FromQuery] string? riskLevel = null)
        {
            var query = _context.GeneratedScenarios.AsQueryable();

            if(!string.IsNullOrEmpty(riskLevel) && riskLevel != "mixed")
            {
                query = query.Where(g => g.RiskLevel == riskLevel);
            }

            var count = await query.CountAsync();
            if(count == 0)
            {
                return NotFound(new { message = "No scenarios found with the specified criteria" });
            }

            var random = new Random();
            var skip = random.Next(count);

            var scenario = await query.Skip(skip).FirstAsync();

            return Ok(
                new
                {
                    scenario.Id,
                    scenario.Name,
                    scenario.Description,
                    ScenarioJson = JsonSerializer.Deserialize<object>(scenario.ScenarioJson),
                    scenario.RiskLevel,
                    scenario.AmountRiskScore,
                    scenario.Amount,
                    scenario.GeneratedAt
                });
        }


        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGeneration(int id)
        {
            var scenario = await _context.GeneratedScenarios.FindAsync(id);
            if(scenario == null)
                return NotFound();

            // Soft delete
            scenario.IsDeleted = true;
            scenario.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Scenario deleted successfully" });
        }

        [HttpPost("{id}/restore")]
        public async Task<ActionResult> RestoreGeneration(int id)
        {
            var scenario = await _context.GeneratedScenarios
                .IgnoreQueryFilters()
                .Where(g => g.Id == id && g.IsDeleted)
                .FirstOrDefaultAsync();
                
            if(scenario == null)
                return NotFound();

            // Restore from soft delete
            scenario.IsDeleted = false;
            scenario.DeletedAt = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Scenario restored successfully" });
        }

        [HttpGet("deleted")]
        public async Task<ActionResult<IEnumerable<object>>> GetDeletedGenerations()
        {
            var deletedScenarios = await _context.GeneratedScenarios
                .IgnoreQueryFilters()
                .Where(g => g.IsDeleted)
                .OrderByDescending(g => g.DeletedAt)
                .Select(g => new
                {
                    g.Id,
                    g.Name,
                    g.Description,
                    g.RiskLevel,
                    g.Amount,
                    g.GeneratedAt,
                    g.DeletedAt
                })
                .ToListAsync();

            return Ok(deletedScenarios);
        }

        [HttpDelete("clear")]
        public async Task<ActionResult> ClearAllGenerations()
        {
            var scenarios = await _context.GeneratedScenarios.ToListAsync();
            var count = scenarios.Count;
            
            foreach(var scenario in scenarios)
            {
                scenario.IsDeleted = true;
                scenario.DeletedAt = DateTime.UtcNow;
            }
            
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Soft deleted {count} generated scenarios" });
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetStatistics()
        {
            var stats = await _context.GeneratedScenarios
                .GroupBy(g => 1)
                .Select(
                    g => new
                    {
                        totalGenerated = g.Count(),
                        totalTested = g.Count(s => s.IsTested),
                        successfulTests = g.Count(s => s.TestSuccessful == true),
                        averageResponseTime = g.Where(s => s.ResponseTimeMs.HasValue).Average(s => s.ResponseTimeMs) ??
                            0,
                        riskDistribution = new
                        {
                            low = g.Count(s => s.RiskLevel == "low"),
                            medium = g.Count(s => s.RiskLevel == "medium"),
                            high = g.Count(s => s.RiskLevel == "high")
                        },
                        recentGenerations = g.OrderByDescending(s => s.GeneratedAt).Take(5)
                    })
                .FirstOrDefaultAsync();

            if(stats == null)
            {
                return Ok(
                    new
                    {
                        totalGenerated = 0,
                        totalTested = 0,
                        successfulTests = 0,
                        averageResponseTime = 0.0,
                        riskDistribution = new { low = 0, medium = 0, high = 0 },
                        recentGenerations = new object[0]
                    });
            }

            return Ok(stats);
        }

        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<object>>> GetFavoriteScenarios()
        {
            var favorites = await _context.GeneratedScenarios
                .Where(g => g.IsFavorite)
                .OrderByDescending(g => g.GeneratedAt)
                .Select(
                    g => new
                    {
                        g.Id,
                        g.Name,
                        g.Description,
                        g.RiskLevel,
                        g.Amount,
                        g.AmountRiskScore,
                        g.GeneratedAt,
                        g.IsTested,
                        g.TestSuccessful
                    })
                .ToListAsync();

            return Ok(favorites);
        }

        [HttpPost("{id}/favorite")]
        public async Task<ActionResult> ToggleFavorite(int id)
        {
            var scenario = await _context.GeneratedScenarios.FindAsync(id);
            if(scenario == null)
                return NotFound();

            scenario.IsFavorite = !scenario.IsFavorite;
            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    isFavorite = scenario.IsFavorite,
                    message = scenario.IsFavorite ? "Added to favorites" : "Removed from favorites"
                });
        }

        [HttpPut("{id}/notes")]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateNotes(int id, [FromBody] UpdateNotesRequestDto request)
        {
            var scenario = await _context.GeneratedScenarios.FindAsync(id);
            if(scenario == null)
                return NotFound();

            scenario.Notes = request.Notes;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Notes updated successfully" });
        }

        [HttpPost("test-random")]
        public ActionResult<object> TestRandomGeneration()
        {
            // Generate 3 fresh scenarios to test randomness
            var scenarios = new List<object>();
            
            for (int i = 0; i < 3; i++)
            {
                var scenario = GenerateSingleScenario("high", false);
                scenarios.Add(new
                {
                    iteration = i + 1,
                    cnic = scenario.CNIC,
                    fromAccount = scenario.FromAccount,
                    amount = scenario.Amount,
                    fromName = scenario.FromName,
                    toName = scenario.ToName,
                    userProfile = scenario.UserProfile,
                    generatedAt = scenario.GeneratedAt,
                    scenarioJson = scenario.ScenarioJson
                });
            }
            
            return Ok(new { 
                message = "Fresh scenarios generated to test randomness", 
                scenarios = scenarios,
                timestamp = DateTime.UtcNow
            });
        }
        
        [HttpPost("force-fresh")]
        public async Task<ActionResult<ApiResponseDto<object>>> GenerateFreshScenarios([FromBody] ScenarioGenerationRequestDto request)
        {
            try
            {
                _logger.LogInformation("Force generating {Count} fresh scenarios with risk focus: {RiskFocus}", request.Count, request.RiskFocus);
                
                // Force generation of fresh scenarios without checking database
                var scenarios = new List<GeneratedScenario>();

                for(int i = 0; i < request.Count; i++)
                {
                    var scenario = GenerateSingleScenario(request.RiskFocus, false); // Force fresh generation
                    scenario.GeneratedAt = DateTime.UtcNow.AddMilliseconds(i); // Ensure unique timestamps
                    scenarios.Add(scenario);
                }

                // Save to database if requested
                if(request.SaveToDatabase)
                {
                    _context.GeneratedScenarios.AddRange(scenarios);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved {Count} fresh scenarios to database", scenarios.Count);
                }

                var scenarioResults = scenarios.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Description,
                    ScenarioJson = JsonSerializer.Deserialize<object>(s.ScenarioJson),
                    s.RiskLevel,
                    s.AmountRiskScore,
                    s.Amount,
                    s.CNIC,
                    s.FromAccount,
                    s.FromName,
                    s.ToName,
                    s.GeneratedAt
                }).ToList();

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Fresh scenarios generated (not from database cache)",
                    Data = new
                    {
                        scenarios = scenarioResults,
                        format = request.Format,
                        totalGenerated = scenarios.Count
                    }
                });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error generating fresh scenarios with request: {@Request}", request);
                return StatusCode(500, new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Error generating fresh scenarios",
                    Error = ex.Message
                });
            }
        }

        [HttpPost("bulk-test")]
        public async Task<ActionResult<ApiResponseDto<object>>> BulkTestScenarios([FromBody] BulkTestRequestDto request)
        {
            try
            {
                var scenarios = await _context.GeneratedScenarios
                    .Where(s => request.ScenarioIds.Contains(s.Id))
                    .ToListAsync();

                if(scenarios.Count == 0)
                    return BadRequest("No valid scenarios found for testing");

                var results = new List<object>();
                var httpClient = _httpClientFactory.CreateClient();

                // Add Bearer Token if provided
                if(!string.IsNullOrEmpty(request.BearerToken))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", request.BearerToken);
                }

                foreach(var scenario in scenarios)
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        var content = new StringContent(scenario.ScenarioJson, Encoding.UTF8, "application/json");
                        var response = await httpClient.PostAsync(request.ApiEndpoint, content);
                        stopwatch.Stop();

                        // Update scenario
                        scenario.IsTested = true;
                        scenario.TestedAt = DateTime.UtcNow;
                        scenario.ApiEndpoint = request.ApiEndpoint;
                        scenario.ResponseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                        scenario.TestSuccessful = response.IsSuccessStatusCode;
                        scenario.TestCount++;
                        scenario.LastTestedAt = DateTime.UtcNow;
                        scenario.LastStatusCode = (int)response.StatusCode;

                        if(response.IsSuccessStatusCode)
                        {
                            scenario.TestResponse = await response.Content.ReadAsStringAsync();
                        } else
                        {
                            scenario.TestErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                        }

                        results.Add(
                            new
                            {
                                id = scenario.Id,
                                name = scenario.Name,
                                success = scenario.TestSuccessful,
                                responseTime = scenario.ResponseTimeMs,
                                statusCode = (int)response.StatusCode
                            });
                    } catch(Exception ex)
                    {
                        scenario.IsTested = true;
                        scenario.TestedAt = DateTime.UtcNow;
                        scenario.TestSuccessful = false;
                        scenario.TestErrorMessage = ex.Message;
                        scenario.TestCount++;
                        scenario.LastTestedAt = DateTime.UtcNow;

                        results.Add(
                            new
                            {
                                id = scenario.Id,
                                name = scenario.Name,
                                success = false,
                                responseTime = 0,
                                statusCode = 0,
                                error = ex.Message
                            });
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(
                    new
                    {
                        totalTested = scenarios.Count,
                        successful = results.Count(r => (bool)(r.GetType().GetProperty("success")?.GetValue(r) ?? false)),
                        results = results
                    });
            } catch(Exception ex)
            {
                _logger.LogError(ex, "Error during bulk testing");
                return StatusCode(500, new { message = "Error during bulk testing", error = ex.Message });
            }
        }

        private GeneratedScenario GenerateSingleScenario(string riskFocus, bool useDatabase)
        {
            // Risk level logic
            var riskLevel = GetRiskLevel(riskFocus);

            // Generate scenario data
            var profile = GetRandomUserProfile();
            var activity = GetRandomUserActivity();
            var fromName = GetRandomFromName();
            var toName = GetRandomToName();
            var comment = GetRandomTransactionComment();
            var activityCode = GetRandomActivityCode();
            var bank = GetRandomBank();

            var amount = GenerateAmount(riskLevel);
            var cnic = GenerateCNIC();
            var fromAccount = GenerateAccount().ToString();
            var toAccount = GenerateIBAN();
            var userId = GenerateUserId();
            var transactionId = GenerateTransactionId();
            // Generate random date/time within last 30 days with random hours/minutes
            var dateTimeObj = DateTime.UtcNow
                .AddDays(-Random.Shared.Next(0, 30))
                .AddHours(-Random.Shared.Next(0, 24))
                .AddMinutes(-Random.Shared.Next(0, 60))
                .AddSeconds(-Random.Shared.Next(0, 60));
            var dateTime = dateTimeObj.ToString("dd/MM/yyyy, HH:mm:ss");
            var userType = GetRandomElement(new[] { "MOBILE", "WEB", "BRANCH", "API", "USSD", "ATM" });

            var context = GenerateTransactionContext(riskLevel);
            var watchlist = GenerateWatchlistIndicators(riskLevel);

            var prompt = $@"User Profile Summary:
- {profile}.
- {activity}.

Transaction Context:
- Amount Risk Score: {context.AmountRiskScore}
- Amount Z-Score: {context.AmountZScore}
- High Amount Flag: {context.HighAmountFlag}
- New Activity Code: {context.NewActivityCode}
- New NewFrom Account: {context.NewFromAccount}
- New To Account: {context.NewToAccount}
- New To City: {context.NewToCity}
- Outside Usual Day: {context.OutsideUsualDay}

Watchlist Indicators:
- FromAccount: {watchlist.FromAccount}
- FromName: {watchlist.FromName}
- ToAccount: {watchlist.ToAccount}
- ToName: {watchlist.ToName}
- ToBank: {watchlist.ToBank}
- IPAddress: {watchlist.IpAddress}

Transaction Details:
- CNIC: {cnic}
- FromAccount: {fromAccount}
{(context.NewFromAccount == "Yes" ? "- New NewFrom Account: Yes\n" : "")}
- LogDescription: {comment}
- UserId: {userId}
- FromName: {fromName}
- ToAccount: {toAccount}
- ToName: {toName}
- ToBank: {bank}
- Amount: {amount}
- DateTime: {dateTime}
- ActivityCode: {activityCode}
- UserType: {userType}
- TransactionComments: {comment}";

            var scenarioObject = new
            {
                model = "fraud-detector:stable",
                messages = new[] { new { role = "user", content = prompt } },
                stream = false
            };

            var scenarioJson = JsonSerializer.Serialize(
                scenarioObject,
                new JsonSerializerOptions { WriteIndented = true });

            return new GeneratedScenario
            {
                Name = $"{(profile?.Split(' ')?.FirstOrDefault() ?? "Unknown")} - {activityCode}",
                Description = $"{riskLevel.ToUpper()} risk scenario: {profile ?? "Unknown profile"}",
                ScenarioJson = scenarioJson,
                RiskLevel = riskLevel,
                UserProfile = profile,
                UserActivity = activity,
                AmountRiskScore = context.AmountRiskScore,
                AmountZScore = context.AmountZScore,
                HighAmountFlag = context.HighAmountFlag == "Yes",
                HasWatchlistMatch = HasAnyWatchlistMatch(watchlist),
                FromName = fromName,
                ToName = toName,
                Amount = amount,
                ActivityCode = activityCode,
                UserType = userType,
                GeneratedAt = DateTime.UtcNow,

                // Enhanced fields
                GeneratedPrompt = prompt,
                FromAccount = fromAccount,
                ToAccount = toAccount,
                TransactionId = transactionId,
                CNIC = cnic,
                UserId = userId,
                TransactionDateTime = dateTimeObj,
                TransactionComments = comment,
                ToBank = bank,
                NewActivityCode = context.NewActivityCode == "Yes",
                NewFromAccount = context.NewFromAccount == "Yes",
                NewToAccount = context.NewToAccount == "Yes",
                NewToCity = context.NewToCity == "Yes",
                OutsideUsualDay = context.OutsideUsualDay == "Yes",
                WatchlistFromAccount = watchlist.FromAccount == "Yes",
                WatchlistFromName = watchlist.FromName == "Yes",
                WatchlistToAccount = watchlist.ToAccount == "Yes",
                WatchlistToName = watchlist.ToName == "Yes",
                WatchlistToBank = watchlist.ToBank == "Yes",
                WatchlistIPAddress = watchlist.IpAddress == "Yes",
                ConfigurationId = 1 // Default to first configuration for now
            };
        }

        // Helper methods
        private string GetRiskLevel(string focus)
        {
            return focus switch
            {
                "low" => "low",
                "medium" => "medium",
                "high" => "high",
                _ => GetRandomElement(new[] { "low", "medium", "high" })
            };
        }

        private static readonly string[] UserProfiles =
        {
            "Regular grocery trader",
            "Small business owner",
            "Salaried individual",
            "Freelancer",
            "Corporate account holder",
            "Individual customer",
            "Small shopkeeper",
            "Regular utility bill payer",
            "Student account holder",
            "Pensioner",
            "Export business owner",
            "Online merchant",
            "Construction business owner",
            "Medical practitioner",
            "Retail store owner",
            "Restaurant owner",
            "Textile manufacturer",
            "Real estate agent",
            "Transport company owner",
            "IT services provider",
            "Pharmaceutical distributor",
            "Electronics dealer",
            "Automobile trader",
            "Jewelry shop owner",
            "Travel agent",
            "Insurance agent",
            "Educational institute",
            "NGO representative",
            "Government contractor",
            "Investment advisor",
            "Legal consultant",
            "Media house owner",
            "Agricultural supplier",
            "Sports equipment dealer",
            "Beauty salon owner"
        };

        private static readonly string[] UserActivities =
        {
            "Today made 18 transactions",
            "Today made 3 transactions",
            "Today made 1 transaction",
            "Today made 5 transactions",
            "Today made 12 transactions",
            "Today made 25 transactions",
            "Today made 7 transactions",
            "Today made 2 transactions",
            "Today made 15 transactions",
            "Today made 9 transactions",
            "Typically makes 5–7 transactions daily",
            "Typically makes 2–4 transactions daily",
            "Typically makes 10–15 transactions daily",
            "Typically makes 1–2 transactions daily",
            "Usually 2–3 salary transfers per month",
            "Usually 4–5 supplier payments per week",
            "Usually 1–2 international transfers per month",
            "Receives foreign remittances monthly",
            "Receives domestic transfers weekly",
            "Usually transfers 50–70 lakh daily",
            "Usually transfers 10–20 lakh daily",
            "Usually transfers 5–10 lakh daily",
            "Recently showed higher activity than usual",
            "Recently reduced transaction frequency",
            "Unusual number of outward transfers today",
            "Unusual number of inward transfers today",
            "First transaction in 6 months",
            "First transaction in 3 months",
            "First international transaction this year",
            "Weekly bulk transfers",
            "Weekly utility payments",
            "Daily cash deposits",
            "Daily cash withdrawals",
            "Monthly utility payments",
            "Monthly loan payments",
            "Quarterly tax payments",
            "Irregular transaction pattern",
            "High frequency micro-transactions",
            "Low frequency high-value transactions"
        };

        private static readonly string[] FromNames =
        {
            "HUSSAIN TRADERS",
            "UMER ALI",
            "MALIK TRADERS",
            "AHMED ENTERPRISES",
            "WESTERN UNION",
            "STAR IMPORTS LLC",
            "BILAL AHMAD",
            "RASHID STORE",
            "KASHIF MALIK",
            "HASSAN RAZA",
            "FATIMA TEXTILES",
            "KARACHI STEEL",
            "SALMAN KHAN",
            "ZAINAB CORPORATION",
            "ABDUL REHMAN",
            "SARA ENTERPRISES",
            "MUHAMMAD FAROOQ",
            "NADIA TRADING",
            "TARIQ FOODS",
            "AMINAH BOUTIQUE",
            "YOUSUF ELECTRONICS",
            "KHADIJA TEXTILES",
            "OMAR CONSTRUCTION",
            "RAFIA MEDICAL",
            "SAEED TRANSPORT",
            "MARIAM JEWELERS",
            "ADNAN PHARMA",
            "FARAH AUTO PARTS",
            "IBRAHIM STEEL",
            "AYESHA COSMETICS",
            "SHAHID MOTORS",
            "RUBINA FABRICS",
            "NAEEM HARDWARE",
            "SABEEN TRAVELS",
            "WASEEM BOOKS",
            "SAMINA GARMENTS",
            "RAZZAQ FRUITS",
            "BUSHRA BAKERY",
            "FAISAL SPARES",
            "NASREEN CLINIC"
        };

        private static readonly string[] ToNames =
        {
            "K-ELECTRIC",
            "GLOBAL IMPORTS",
            "HESCO BILLING",
            "KASHIF MALIK",
            "HASSAN RAZA",
            "ASIA GLOBAL",
            "EASYPAISA WALLET",
            "SELF ACCOUNT",
            "UTILITY COMPANY",
            "MOBILE ACCOUNT",
            "DARAZ PAKISTAN",
            "FOOD PANDA",
            "CAREEM WALLET",
            "UBER EATS",
            "AMAZON PAYMENTS",
            "ALIBABA GROUP",
            "SSGC BILLING",
            "PTCL PAYMENTS",
            "JAZZ CASH",
            "TELENOR BANK",
            "NATIONAL BANK",
            "MCB DIGITAL",
            "HBL KONNECT",
            "UBL OMNI",
            "GOVT TREASURY",
            "TAX OFFICE",
            "CUSTOMS DEPT",
            "WAPDA BILLING",
            "RAILWAY BOOKING",
            "PIA TICKETING",
            "SERENA HOTELS",
            "PEARL CONTINENTAL",
            "GOURMET FOODS",
            "METRO CASH",
            "IMTIAZ SUPER",
            "HYPERSTAR",
            "AL-FATAH STORES",
            "CHEN ONE",
            "SAPPHIRE RETAIL",
            "KHAADI STORES"
        };

        private static readonly string[] TransactionComments =
        {
            "Electricity Bill",
            "Urgent Import Settlement",
            "Electricity bill for warehouse",
            "Employee salary transfer",
            "Freelance payment",
            "Container clearance payment",
            "Load wallet for shopping",
            "Daily deposit of shop sales",
            "Monthly utility bill",
            "Gas bill payment",
            "Water bill payment",
            "Internet bill payment",
            "Mobile bill payment",
            "Insurance premium",
            "Loan installment",
            "Credit card payment",
            "Rent payment",
            "Medical expenses",
            "School fee payment",
            "University tuition",
            "Travel booking payment",
            "Hotel booking",
            "Flight ticket payment",
            "Online shopping",
            "Grocery purchase",
            "Fuel payment",
            "Car maintenance",
            "Investment deposit",
            "Tax payment",
            "Charity donation",
            "Business equipment purchase",
            "Office supplies",
            "Raw material purchase",
            "Supplier payment",
            "Vendor settlement",
            "Commission payment",
            "Bonus distribution",
            "Dividend payment",
            "Refund processing",
            "Emergency transfer",
            "Family support",
            "Wedding expenses",
            "Festival preparation",
            "Property down payment",
            "Vehicle installment",
            "Construction payment",
            "Equipment lease",
            "Software subscription",
            "Professional services",
            "Legal fees"
        };

        private static readonly string[] ActivityCodes =
        {
            "Bill Payment",
            "Raast FT",
            "Fund Transfer",
            "Credit Inflow",
            "Cash Deposit",
            "Wallet Load",
            "Utility Payment",
            "Salary Transfer",
            "International Transfer",
            "Merchant Payment",
            "Online Payment",
            "ATM Withdrawal",
            "Mobile Banking",
            "Remittance",
            "Investment",
            "Loan Payment"
        };

        private static readonly string[] Banks =
        {
            "HABBPKKA001",
            "MCBLPKKA001",
            "HBLPKKA001",
            "MCBPKKA002",
            "NBPAPKKA004",
            "UBLPKKA007",
            "HBLPKKA009",
            "BAHL12345",
            "ALFAPKKA888",
            "KASHPKKA123",
            "MBLBPKKA007",
            "JSBLPKKA200",
            "FAYSPKKA134",
            "SCBLPKKA890",
            "CITIPKKA567",
            "DEUTPKKA445",
            "SMBCPKKA332",
            "BARPPKKA667",
            "CHASPKKA889",
            "BKIDPKKA778",
            "TEBAPKKA555",
            "SILKPKKA666",
            "BIBLPKKA444"
        };

        private string GetRandomUserProfile() => GetRandomElement(UserProfiles);

        private string GetRandomUserActivity() => GetRandomElement(UserActivities);

        private string GetRandomFromName() => GetRandomElement(FromNames);

        private string GetRandomToName() => GetRandomElement(ToNames);

        private string GetRandomTransactionComment() => GetRandomElement(TransactionComments);

        private string GetRandomActivityCode() => GetRandomElement(ActivityCodes);

        private string GetRandomBank() => GetRandomElement(Banks);

        private string GetRandomElement(string[] array) { return array[Random.Shared.Next(array.Length)]; }

        private decimal GenerateAmount(string riskLevel)
        {
            return riskLevel switch
            {
                "low" => Random.Shared.Next(1000, 50000),
                "medium" => Random.Shared.Next(50000, 500000),
                "high" => Random.Shared.Next(500000, 10000000),
                _ => Random.Shared.Next(1000, 1000000)
            };
        }

        private string GenerateCNIC() => $"CN4210{Random.Shared.Next(100000000, 999999999)}";

        private long GenerateAccount() => Random.Shared.NextInt64(1000000000000000, 9999999999999999);

        private string GenerateIBAN()
        {
            var bankCodes = new[] { "HBL", "MCB", "NBP", "UBL", "BAHL" };
            var bankCode = GetRandomElement(bankCodes);
            return $"PK{Random.Shared.Next(10, 99)}{bankCode}00{Random.Shared.NextInt64(100000000000000, 999999999999999)}";
        }

        private string GenerateUserId()
        {
            var prefixes = new[] { "user", "shop", "corp", "cust", "bus", "trade", "acc", "client", "merchant", "agent", "vendor", "retail" };
            return $"{GetRandomElement(prefixes)}{Random.Shared.Next(100, 99999)}";
        }

        private string GenerateTransactionId()
        { return $"TXN{DateTime.Now:yyyyMMdd}{Random.Shared.Next(100000, 999999)}"; }

        private (int AmountRiskScore, decimal AmountZScore, string HighAmountFlag, string NewActivityCode, 
                string NewFromAccount, string NewToAccount, string NewToCity, string OutsideUsualDay) GenerateTransactionContext(
            string riskLevel)
        {
            return riskLevel switch
            {
                "low" => (
                    Random.Shared.Next(1, 4),
                    (decimal)(Random.Shared.NextDouble() * 1.5),
                    "No",
                    Random.Shared.NextDouble() > 0.8 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.9 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.7 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.8 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.9 ? "Yes" : "No"
                ),
                "medium" => (
                    Random.Shared.Next(3, 7),
                    (decimal)(Random.Shared.NextDouble() * 1.5 + 1.0),
                    Random.Shared.NextDouble() > 0.5 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.6 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.7 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.5 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.6 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.7 ? "Yes" : "No"
                ),
                "high" => (
                    Random.Shared.Next(6, 11),
                    (decimal)(Random.Shared.NextDouble() * 2.0 + 2.0),
                    "Yes",
                    Random.Shared.NextDouble() > 0.4 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.4 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.3 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.4 ? "Yes" : "No",
                    Random.Shared.NextDouble() > 0.5 ? "Yes" : "No"
                ),
                _ => (Random.Shared.Next(1, 11), (decimal)(Random.Shared.NextDouble() * 4.0), 
                     Random.Shared.NextDouble() > 0.6 ? "Yes" : "No",
                     Random.Shared.NextDouble() > 0.6 ? "Yes" : "No",
                     Random.Shared.NextDouble() > 0.7 ? "Yes" : "No",
                     Random.Shared.NextDouble() > 0.6 ? "Yes" : "No",
                     Random.Shared.NextDouble() > 0.7 ? "Yes" : "No",
                     Random.Shared.NextDouble() > 0.7 ? "Yes" : "No")
            };
        }

        private (string FromAccount, string FromName, string ToAccount, string ToName, string ToBank, string IpAddress) GenerateWatchlistIndicators(
            string riskLevel)
        {
            var baseProb = riskLevel switch
            {
                "high" => 0.3,
                "medium" => 0.15,
                "low" => 0.05,
                _ => 0.1
            };

            return (
                Random.Shared.NextDouble() < baseProb ? "Yes" : "No",
                Random.Shared.NextDouble() < baseProb ? "Yes" : "No",
                Random.Shared.NextDouble() < baseProb ? "Yes" : "No",
                Random.Shared.NextDouble() < baseProb ? "Yes" : "No",
                Random.Shared.NextDouble() < baseProb ? "Yes" : "No",
                Random.Shared.NextDouble() < baseProb ? "Yes" : "No"
            );
        }

        private bool HasAnyWatchlistMatch(
            (string FromAccount, string FromName, string ToAccount, string ToName, string ToBank, string IpAddress) watchlist)
        {
            return watchlist.FromAccount == "Yes" ||
                watchlist.FromName == "Yes" ||
                watchlist.ToAccount == "Yes" ||
                watchlist.ToName == "Yes" ||
                watchlist.ToBank == "Yes" ||
                watchlist.IpAddress == "Yes";
        }

    }
}
