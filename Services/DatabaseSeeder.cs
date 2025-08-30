using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FraudDetectorWebApp.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                await SeedApiConfigurationsAsync();
                await SeedTestScenariosAsync();
                await SeedTestUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding database");
                throw;
            }
        }

        private async Task SeedApiConfigurationsAsync()
        {
            var existingConfigs = await _context.ApiConfigurations.ToListAsync();
            
            if (existingConfigs.Any())
            {
                // Don't update existing configurations to preserve user changes
                _logger.LogInformation("Found {Count} existing API configurations, leaving them unchanged", existingConfigs.Count);
                return;
            }
            else
            {
                var fraudDetectionRequestTemplate = @"{
  ""model"": ""fraud-detector:stable"",
  ""messages"": [
    {
      ""role"": ""user"",
      ""content"": ""User Profile Summary:\n- {{user_profile}}.\n- {{user_activity}}.\n\nTransaction Context:\n- Amount Risk Score: {{amount_risk_score}}\n- Amount Z-Score: {{amount_z_score}}\n- High Amount Flag: {{high_amount_flag}}\n- New Activity Code: {{new_activity_code}}\n- New NewFrom Account: {{new_from_account}}\n- New To Account: {{new_to_account}}\n- New To City: {{new_to_city}}\n- Outside Usual Day: {{outside_usual_day}}\n- Watchlist From Account: {{watchlist_from_account}}\n- Watchlist From Name: {{watchlist_from_name}}\n- Watchlist To Account: {{watchlist_to_account}}\n- Watchlist To Name: {{watchlist_to_name}}\n- Watchlist To Bank: {{watchlist_to_bank}}\n- Watchlist IP Address: {{watchlist_ip_address}}\n\nTransaction Details:\n- CNIC: {{random_cnic}}\n- FromAccount: {{random_account}}\n- FromName: {{from_name}}\n- ToAccount: {{random_iban}}\n- ToName: {{to_name}}\n- Amount: {{random_amount}}\n- ActivityCode: {{activity_code}}\n- UserType: {{user_type}}\n- ToBank: {{to_bank}}\n- TransactionComments: {{transaction_comments}}\n- TransactionDateTime: {{transaction_datetime}}\n- UserId: {{user_id}}\n- TransactionId: TXN{{timestamp}}{{random}}""
    }
  ],
  ""stream"": false
}";

                var configurations = new[]
                {
                    new ApiConfiguration
                    {
                        Name = "Fraud Detection API - Production",
                        ApiEndpoint = "https://10.10.110.107:443/validate-chat",
                        RequestTemplate = fraudDetectionRequestTemplate,
                        BearerToken = "sesoqefHnKglaJKJwRtE4DZW6aqDLGxNRRu/qhiCUug=",
                        DelayBetweenRequests = 2000,
                        MaxIterations = 10,
                        IsActive = true,
                        TrustSslCertificate = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ApiConfiguration
                    {
                        Name = "Fraud Detection API - Test Mode",
                        ApiEndpoint = "https://10.10.110.107:443/validate-chat",
                        RequestTemplate = fraudDetectionRequestTemplate,
                        BearerToken = "sesoqefHnKglaJKJwRtE4DZW6aqDLGxNRRu/qhiCUug=",
                        DelayBetweenRequests = 5000,
                        MaxIterations = 5,
                        IsActive = false,
                        TrustSslCertificate = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new ApiConfiguration
                    {
                        Name = "Local Test API (Backup)",
                        ApiEndpoint = "https://httpbin.org/post",
                        RequestTemplate = """{"test": true, "data": "{{random}}", "timestamp": "{{iso_timestamp}}"}""",
                        DelayBetweenRequests = 1000,
                        MaxIterations = 3,
                        IsActive = false,
                        TrustSslCertificate = false,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.ApiConfigurations.AddRange(configurations);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} API configurations", configurations.Length);
            }
        }

        private async Task SeedTestScenariosAsync()
        {
            if (!await _context.GeneratedScenarios.AnyAsync())
            {
                var scenarios = new List<GeneratedScenario>();

                // Generate sample scenarios for each risk level
                foreach (var riskLevel in new[] { "low", "medium", "high" })
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var scenario = CreateSampleScenario(riskLevel, i + 1);
                        scenarios.Add(scenario);
                    }
                }

                _context.GeneratedScenarios.AddRange(scenarios);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded {Count} test scenarios", scenarios.Count);
            }
        }

        private async Task SeedTestUsersAsync()
        {
            if (!await _context.Users.AnyAsync())
            {
                var testUser = new User
                {
                    FirstName = "Test",
                    LastName = "Admin",
                    Email = "admin@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Phone = "+92-300-1234567",
                    Company = "Test Company",
                    Role = "Administrator",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(testUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Seeded test user: {Email}", testUser.Email);
            }
        }

        private GeneratedScenario CreateSampleScenario(string riskLevel, int index)
        {
            var userProfiles = new[]
            {
                "Regular grocery trader", "Small business owner", "Salaried individual", 
                "Corporate account holder", "Export business owner"
            };

            var userActivities = new[]
            {
                "Today made 5 transactions", "Typically makes 3-5 transactions daily",
                "Usually transfers moderate amounts", "Recent increase in activity",
                "Regular pattern of payments"
            };

            var fromNames = new[]
            {
                "HUSSAIN TRADERS", "MALIK ENTERPRISES", "AHMED CORP", "RASHID STORE", "BILAL TRADING"
            };

            var toNames = new[]
            {
                "K-ELECTRIC", "UTILITY COMPANY", "BUSINESS PARTNER", "GOVERNMENT DEPT", "SUPPLIER"
            };

            var activityCodes = new[]
            {
                "Bill Payment", "Fund Transfer", "Business Payment", "Utility Payment", "Salary Transfer"
            };

            var random = new Random();
            var profile = userProfiles[index % userProfiles.Length];
            var activity = userActivities[index % userActivities.Length];
            var fromName = fromNames[index % fromNames.Length];
            var toName = toNames[index % toNames.Length];
            var activityCode = activityCodes[index % activityCodes.Length];
            var comment = $"Sample {riskLevel} risk transaction";

            var amount = riskLevel switch
            {
                "low" => random.Next(1000, 50000),
                "medium" => random.Next(50000, 500000),
                "high" => random.Next(500000, 5000000),
                _ => random.Next(1000, 100000)
            };

            var amountRiskScore = riskLevel switch
            {
                "low" => random.Next(1, 4),
                "medium" => random.Next(3, 7),
                "high" => random.Next(6, 11),
                _ => random.Next(1, 11)
            };

            var cnic = $"CN4210{random.Next(100000000, 999999999)}";
            var fromAccount = random.NextInt64(1000000000000000, 9999999999999999).ToString();
            var toAccount = $"PK{random.Next(10, 99)}HBL00{random.NextInt64(100000000000000, 999999999999999)}";
            var userId = $"user{random.Next(1000, 99999)}";
            var transactionId = $"TXN{DateTime.Now:yyyyMMdd}{random.Next(100000, 999999)}";
            var userType = new[] { "MOBILE", "WEB", "BRANCH", "API" }[random.Next(4)];

            var prompt = $@"User Profile Summary:
- {profile}.
- {activity}.

Transaction Context:
- Amount Risk Score: {amountRiskScore}
- Amount Z-Score: {(random.NextDouble() * 3.0):F1}
- High Amount Flag: {(riskLevel == "high" ? "Yes" : "No")}

Transaction Details:
- CNIC: {cnic}
- FromAccount: {fromAccount}
- FromName: {fromName}
- ToAccount: {toAccount}
- ToName: {toName}
- Amount: {amount}
- ActivityCode: {activityCode}
- UserType: {userType}
- TransactionComments: {comment}";

            var scenarioObject = new
            {
                model = "fraud-detector:stable",
                messages = new[] { new { role = "user", content = prompt } },
                stream = false
            };

            var scenarioJson = JsonSerializer.Serialize(scenarioObject, new JsonSerializerOptions { WriteIndented = true });

            return new GeneratedScenario
            {
                Name = $"{profile.Split(' ')[0]} - {activityCode}",
                Description = $"{riskLevel.ToUpper()} risk scenario: {profile}",
                ScenarioJson = scenarioJson,
                RiskLevel = riskLevel,
                UserProfile = profile,
                UserActivity = activity,
                AmountRiskScore = amountRiskScore,
                AmountZScore = (decimal)(random.NextDouble() * 3.0),
                HighAmountFlag = riskLevel == "high",
                HasWatchlistMatch = random.NextDouble() < 0.1,
                FromName = fromName,
                ToName = toName,
                Amount = amount,
                ActivityCode = activityCode,
                UserType = userType,
                GeneratedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)),
                GeneratedPrompt = prompt,
                FromAccount = fromAccount,
                ToAccount = toAccount,
                TransactionId = transactionId,
                CNIC = cnic,
                UserId = userId,
                TransactionDateTime = DateTime.UtcNow.AddDays(-random.Next(0, 30)),
                TransactionComments = comment,
                ToBank = "HABLPKKA001",
                ConfigurationId = 1,
                IsFavorite = random.NextDouble() < 0.2 // 20% chance of being favorite
            };
        }
    }
}
