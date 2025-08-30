using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using FraudDetectorWebApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace FraudDetectorWebApp.Services
{
    public class ApiRequestService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ApiRequestService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHubContext<ApiTestHub> _hubContext;
        private CancellationTokenSource? _loopCancellationTokenSource;
        private bool _isRunning = false;
        private bool _disposed = false;
        private readonly object _lockObject = new object();

        public ApiRequestService(IServiceProvider serviceProvider, ILogger<ApiRequestService> logger, IHttpClientFactory httpClientFactory, IHubContext<ApiTestHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested && !_disposed)
            {
                try
                {
                    CancellationTokenSource? currentTokenSource;
                    bool shouldProcess;
                    
                    lock (_lockObject)
                    {
                        currentTokenSource = _loopCancellationTokenSource;
                        shouldProcess = _isRunning && !_disposed && currentTokenSource != null;
                    }
                    
                    if (shouldProcess && currentTokenSource != null && !currentTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await ProcessActiveConfigurations(currentTokenSource.Token);
                        }
                        catch (ObjectDisposedException)
                        {
                            // Token source was disposed while processing - ignore and continue
                            _logger.LogDebug("CancellationTokenSource disposed during processing");
                        }
                    }
                    
                    await Task.Delay(1000, stoppingToken); // Check every second
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Normal shutdown, exit loop
                    break;
                }
                catch (Exception ex)
                {
                    try
                    {
                        _logger.LogError(ex, "Error in ApiRequestService execution");
                    }
                    catch
                    {
                        // Logger may be disposed
                    }
                    
                    try
                    {
                        await Task.Delay(5000, stoppingToken); // Wait before retrying
                    }
                    catch (OperationCanceledException)
                    {
                        // Shutdown requested during delay
                        break;
                    }
                }
            }
        }

        public async Task StartLoop()
        {
            lock (_lockObject)
            {
                if (_disposed)
                {
                    _logger.LogWarning("Cannot start loop - service is disposed");
                    return;
                }
                
                if (_isRunning) return;
                
                // Dispose previous token source if it exists
                try
                {
                    _loopCancellationTokenSource?.Dispose();
                }
                catch (ObjectDisposedException) { }
                
                _loopCancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;
            }
            
            _logger.LogInformation("API request loop started");
            
            // Notify clients about system status change
            try
            {
                await _hubContext.Clients.Group("Dashboard").SendAsync("SystemStatusChanged", new { IsRunning = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error notifying clients about system status change");
            }
        }

        public async Task StopLoop()
        {
            CancellationTokenSource? tokenSourceToCancel = null;
            
            lock (_lockObject)
            {
                if (!_isRunning || _disposed) return;
                
                tokenSourceToCancel = _loopCancellationTokenSource;
                _isRunning = false;
            }
            
            // Cancel outside the lock to prevent deadlocks
            if (tokenSourceToCancel != null)
            {
                try
                {
                    if (!tokenSourceToCancel.IsCancellationRequested)
                    {
                        tokenSourceToCancel.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Token source already disposed - ignore
                    _logger.LogDebug("CancellationTokenSource was already disposed during StopLoop");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cancelling token source during StopLoop");
                }
            }
            
            _logger.LogInformation("API request loop stopped");
            
            // Notify clients about system status change
            try
            {
                await _hubContext.Clients.Group("Dashboard").SendAsync("SystemStatusChanged", new { IsRunning = false });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error notifying clients about system status change");
            }
        }

        public bool IsRunning => _isRunning;

        private async Task ProcessActiveConfigurations(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var activeConfigurations = await context.ApiConfigurations
                .Where(c => c.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var config in activeConfigurations)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await ProcessConfiguration(config, context, cancellationToken);
                
                if (!cancellationToken.IsCancellationRequested && config.DelayBetweenRequests > 0)
                {
                    await Task.Delay(config.DelayBetweenRequests, cancellationToken);
                }
            }
        }

        private async Task ProcessConfiguration(ApiConfiguration config, ApplicationDbContext context, CancellationToken cancellationToken)
        {
            try
            {
                // Get current iteration count
                var currentIteration = await context.ApiRequestLogs
                    .Where(l => l.ApiConfigurationId == config.Id)
                    .CountAsync(cancellationToken) + 1;

                // Check if we've reached max iterations
                if (config.MaxIterations > 0 && currentIteration > config.MaxIterations)
                {
                    config.IsActive = false;
                    await context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Configuration {ConfigName} reached max iterations ({MaxIterations}) and was deactivated", 
                        config.Name, config.MaxIterations);
                    return;
                }

                // Prepare request
                var requestPayload = PrepareRequestPayload(config.RequestTemplate, currentIteration);
                var stopwatch = Stopwatch.StartNew();

                var requestLog = new ApiRequestLog
                {
                    ApiConfigurationId = config.Id,
                    RequestPayload = requestPayload,
                    IterationNumber = currentIteration,
                    RequestTimestamp = DateTime.UtcNow
                };

                try
                {
                    // Create HTTP client with optional SSL certificate validation bypass
                    HttpClient httpClient;
                    if (config.TrustSslCertificate)
                    {
                        var handler = new HttpClientHandler();
                        handler.ClientCertificateOptions = ClientCertificateOption.Manual;
                        handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) => true;
                        httpClient = new HttpClient(handler);
                        _logger.LogInformation("Using SSL certificate bypass for {ConfigName} - {Endpoint}", config.Name, config.ApiEndpoint);
                    }
                    else
                    {
                        httpClient = _httpClientFactory.CreateClient();
                    }

                    using (httpClient)
                    {
                        // Make HTTP request
                        var content = new StringContent(requestPayload, Encoding.UTF8, "application/json");
                        
                        // Create request message to add headers
                        var request = new HttpRequestMessage(HttpMethod.Post, config.ApiEndpoint)
                        {
                            Content = content
                        };
                        
                        // Add Bearer Token if provided
                        if (!string.IsNullOrEmpty(config.BearerToken))
                        {
                            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.BearerToken);
                        }
                        
                        var response = await httpClient.SendAsync(request, cancellationToken);

                        stopwatch.Stop();
                        requestLog.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                        requestLog.StatusCode = (int)response.StatusCode;
                        requestLog.IsSuccessful = response.IsSuccessStatusCode;

                        if (response.IsSuccessStatusCode)
                        {
                            requestLog.ResponseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                            _logger.LogInformation("Request {Iteration} for {ConfigName} completed successfully in {ResponseTime}ms", 
                                currentIteration, config.Name, requestLog.ResponseTimeMs);
                        }
                        else
                        {
                            requestLog.ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                            _logger.LogWarning("Request {Iteration} for {ConfigName} failed: {Error}", 
                                currentIteration, config.Name, requestLog.ErrorMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    requestLog.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                    requestLog.StatusCode = 0;
                    requestLog.IsSuccessful = false;
                    requestLog.ErrorMessage = ex.Message;
                    
                    _logger.LogError(ex, "Exception during request {Iteration} for {ConfigName}", 
                        currentIteration, config.Name);
                }

                // Save request log
                context.ApiRequestLogs.Add(requestLog);
                await context.SaveChangesAsync(cancellationToken);
                
                // Notify clients about new result via SignalR
                await NotifyNewResult(config, requestLog);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing configuration {ConfigName}", config.Name);
            }
        }

        private string PrepareRequestPayload(string template, int iteration)
        {
            var now = DateTime.UtcNow;
            var payload = template;
            
            // Basic placeholders - Generate NEW random values each time
            payload = payload.Replace("{{iteration}}", iteration.ToString());
            payload = payload.Replace("{{timestamp}}", now.ToString("M/d/yyyy hh:mm:ss tt"));
            payload = payload.Replace("{{iso_timestamp}}", now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
            payload = payload.Replace("{{random}}", Random.Shared.Next(1000, 9999).ToString());
            payload = payload.Replace("{{random_amount}}", Random.Shared.Next(10000, 999999).ToString());
            payload = payload.Replace("{{random_cnic}}", $"CN4210{Random.Shared.Next(100000000, 999999999)}");
            payload = payload.Replace("{{random_account}}", $"1063{Random.Shared.Next(100000000, 999999999)}");
            payload = payload.Replace("{{random_iban}}", GenerateRandomIban());
            
            // Expanded user profiles for more variety
            var userProfiles = new[] {
                "Customer is a small retailer with average daily 6–8 Raast transactions",
                "Account mostly used for salary credits and few monthly transfers",
                "Normally local transfers, first international transaction today",
                "This Customer has salary account",
                "Dormant account active after 9 months",
                "Customer usually shops daily online",
                "Pensioner account",
                "Regular grocery trader",
                "Small business owner",
                "Salaried individual",
                "Freelancer",
                "Corporate account holder",
                "Individual customer",
                "Small shopkeeper",
                "Regular utility bill payer",
                "Student account holder",
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
                "Travel agent"
            };
            
            var activities = new[] {
                "Recently showed higher activity than usual",
                "Unusual number of outward transfers today",
                "1 transaction only",
                "Today made 3 transactions",
                "Today made 1 transaction only",
                "Today made 5 transactions",
                "Today made 1 transaction",
                "Today made 18 transactions",
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
                "First transaction in 6 months",
                "First transaction in 3 months",
                "First international transaction this year",
                "Weekly bulk transfers",
                "Monthly utility payments",
                "Daily cash deposits",
                "Irregular transaction pattern",
                "High frequency micro-transactions",
                "Low frequency high-value transactions"
            };
            
            var userNames = new[] {
                "AHMED STORE", "MANSOOR KHAN", "BILAL SAEED", "AHMAD KHAN", 
                "UNKNOWN", "HINA TARIQ", "RASHID ALI", "HUSSAIN TRADERS",
                "UMER ALI", "MALIK TRADERS", "AHMED ENTERPRISES", "WESTERN UNION",
                "STAR IMPORTS LLC", "BILAL AHMAD", "RASHID STORE", "KASHIF MALIK",
                "HASSAN RAZA", "FATIMA TEXTILES", "KARACHI STEEL", "SALMAN KHAN",
                "ZAINAB CORPORATION", "ABDUL REHMAN", "SARA ENTERPRISES",
                "MUHAMMAD FAROOQ", "NADIA TRADING", "TARIQ FOODS", "AMINAH BOUTIQUE",
                "YOUSUF ELECTRONICS", "KHADIJA TEXTILES", "OMAR CONSTRUCTION",
                "RAFIA MEDICAL", "SAEED TRANSPORT", "MARIAM JEWELERS",
                "ADNAN PHARMA", "FARAH AUTO PARTS", "IBRAHIM STEEL",
                "AYESHA COSMETICS", "SHAHID MOTORS", "RUBINA FABRICS",
                "NAEEM HARDWARE", "SABEEN TRAVELS", "WASEEM BOOKS",
                "SAMINA GARMENTS", "RAZZAQ FRUITS", "BUSHRA BAKERY",
                "FAISAL SPARES", "NASREEN CLINIC"
            };
            
            var toNames = new[] {
                "BILAL ASSOCIATES", "FAST UTILITY SERVICE", "MICHAEL BROWN", 
                "FAST INTERNET PVT LTD", "MALIK ENTERPRISES", "DARAZ PAKISTAN", 
                "SELF", "K-ELECTRIC", "GLOBAL IMPORTS", "HESCO BILLING",
                "KASHIF MALIK", "HASSAN RAZA", "ASIA GLOBAL", "EASYPAISA WALLET",
                "UTILITY COMPANY", "MOBILE ACCOUNT", "FOOD PANDA",
                "CAREEM WALLET", "UBER EATS", "AMAZON PAYMENTS", "ALIBABA GROUP",
                "SSGC BILLING", "PTCL PAYMENTS", "JAZZ CASH", "TELENOR BANK",
                "NATIONAL BANK", "MCB DIGITAL", "HBL KONNECT", "UBL OMNI",
                "GOVT TREASURY", "TAX OFFICE", "CUSTOMS DEPT", "WAPDA BILLING",
                "RAILWAY BOOKING", "PIA TICKETING", "SERENA HOTELS",
                "PEARL CONTINENTAL", "GOURMET FOODS", "METRO CASH",
                "IMTIAZ SUPER", "HYPERSTAR", "AL-FATAH STORES", "CHEN ONE",
                "SAPPHIRE RETAIL", "KHAADI STORES"
            };
            
            var comments = new[] {
                "Payment for machinery", "Electricity Bill", "Business Investment",
                "Monthly Internet Bill", "Urgent Machinery Payment", "Order #112233", 
                "Routine Cash Need", "Urgent Import Settlement", "Electricity bill for warehouse",
                "Employee salary transfer", "Freelance payment", "Container clearance payment",
                "Load wallet for shopping", "Daily deposit of shop sales", "Monthly utility bill",
                "Gas bill payment", "Water bill payment", "Internet bill payment",
                "Mobile bill payment", "Insurance premium", "Loan installment",
                "Credit card payment", "Rent payment", "Medical expenses",
                "School fee payment", "University tuition", "Travel booking payment",
                "Hotel booking", "Flight ticket payment", "Online shopping",
                "Grocery purchase", "Fuel payment", "Car maintenance",
                "Investment deposit", "Tax payment", "Charity donation",
                "Business equipment purchase", "Office supplies", "Raw material purchase",
                "Supplier payment", "Vendor settlement", "Commission payment",
                "Bonus distribution", "Dividend payment", "Refund processing",
                "Emergency transfer", "Family support", "Wedding expenses",
                "Festival preparation", "Property down payment", "Vehicle installment",
                "Construction payment", "Equipment lease", "Software subscription",
                "Professional services", "Legal fees"
            };
            
            var activityCodes = new[] {
                "Bill Payment", "Raast FT", "Fund Transfer", "Credit Inflow",
                "Cash Deposit", "Wallet Load", "Utility Payment", "Salary Transfer",
                "International Transfer", "Merchant Payment", "Online Payment",
                "ATM Withdrawal", "Mobile Banking", "Remittance", "Investment", "Loan Payment"
            };
            
            var userTypes = new[] { "MOBILE", "WEB", "BRANCH", "API", "USSD", "ATM" };
            
            var banks = new[] {
                "HABBPKKA001", "MCBLPKKA001", "HBLPKKA001", "MCBPKKA002",
                "NBPAPKKA004", "UBLPKKA007", "HBLPKKA009", "BAHL12345",
                "ALFAPKKA888", "KASHPKKA123", "MBLBPKKA007", "JSBLPKKA200",
                "FAYSPKKA134", "SCBLPKKA890", "CITIPKKA567", "DEUTPKKA445",
                "SMBCPKKA332", "BARPPKKA667", "CHASPKKA889", "BKIDPKKA778",
                "TEBAPKKA555", "SILKPKKA666", "BIBLPKKA444"
            };
            
            // Generate random transaction date/time within last 30 days
            var randomDateTime = now
                .AddDays(-Random.Shared.Next(0, 30))
                .AddHours(-Random.Shared.Next(0, 24))
                .AddMinutes(-Random.Shared.Next(0, 60))
                .AddSeconds(-Random.Shared.Next(0, 60));
            var transactionDateTime = randomDateTime.ToString("dd/MM/yyyy, HH:mm:ss");
            
            // Generate random context values for each request
            var amountRiskScore = Random.Shared.Next(1, 11);
            var amountZScore = Math.Round((decimal)(Random.Shared.NextDouble() * 6.0), 1);
            var highAmountFlag = Random.Shared.NextDouble() > 0.6 ? "Yes" : "No";
            var newActivityCode = Random.Shared.NextDouble() > 0.6 ? "Yes" : "No";
            var newFromAccount = Random.Shared.NextDouble() > 0.7 ? "Yes" : "No";
            var newToAccount = Random.Shared.NextDouble() > 0.6 ? "Yes" : "No";
            var newToCity = Random.Shared.NextDouble() > 0.7 ? "Yes" : "No";
            var outsideUsualDay = Random.Shared.NextDouble() > 0.7 ? "Yes" : "No";
            
            // Generate random watchlist indicators
            var baseWatchlistProb = 0.2; // 20% chance for each watchlist indicator
            var watchlistFromAccount = Random.Shared.NextDouble() < baseWatchlistProb ? "Yes" : "No";
            var watchlistFromName = Random.Shared.NextDouble() < baseWatchlistProb ? "Yes" : "No";
            var watchlistToAccount = Random.Shared.NextDouble() < baseWatchlistProb ? "Yes" : "No";
            var watchlistToName = Random.Shared.NextDouble() < baseWatchlistProb ? "Yes" : "No";
            var watchlistToBank = Random.Shared.NextDouble() < baseWatchlistProb ? "Yes" : "No";
            var watchlistIPAddress = Random.Shared.NextDouble() < baseWatchlistProb ? "Yes" : "No";
            
            // Replace dynamic content with RANDOM selection (not iteration-based)
            payload = payload.Replace("{{user_profile}}", GetRandomElement(userProfiles));
            payload = payload.Replace("{{user_activity}}", GetRandomElement(activities));
            payload = payload.Replace("{{from_name}}", GetRandomElement(userNames));
            payload = payload.Replace("{{to_name}}", GetRandomElement(toNames));
            payload = payload.Replace("{{transaction_comments}}", GetRandomElement(comments));
            payload = payload.Replace("{{activity_code}}", GetRandomElement(activityCodes));
            payload = payload.Replace("{{user_type}}", GetRandomElement(userTypes));
            payload = payload.Replace("{{to_bank}}", GetRandomElement(banks));
            payload = payload.Replace("{{transaction_datetime}}", transactionDateTime);
            payload = payload.Replace("{{user_id}}", GenerateUserId());
            
            // Replace context values
            payload = payload.Replace("{{amount_risk_score}}", amountRiskScore.ToString());
            payload = payload.Replace("{{amount_z_score}}", amountZScore.ToString());
            payload = payload.Replace("{{high_amount_flag}}", highAmountFlag);
            payload = payload.Replace("{{new_activity_code}}", newActivityCode);
            payload = payload.Replace("{{new_from_account}}", newFromAccount);
            payload = payload.Replace("{{new_to_account}}", newToAccount);
            payload = payload.Replace("{{new_to_city}}", newToCity);
            payload = payload.Replace("{{outside_usual_day}}", outsideUsualDay);
            
            // Replace watchlist indicators
            payload = payload.Replace("{{watchlist_from_account}}", watchlistFromAccount);
            payload = payload.Replace("{{watchlist_from_name}}", watchlistFromName);
            payload = payload.Replace("{{watchlist_to_account}}", watchlistToAccount);
            payload = payload.Replace("{{watchlist_to_name}}", watchlistToName);
            payload = payload.Replace("{{watchlist_to_bank}}", watchlistToBank);
            payload = payload.Replace("{{watchlist_ip_address}}", watchlistIPAddress);
            
            return payload;
        }
        
        private string GetRandomElement(string[] array)
        {
            return array[Random.Shared.Next(array.Length)];
        }
        
        private string GenerateRandomIban()
        {
            var bankCodes = new[] { "HBL", "MCB", "NBP", "UBL", "BAHL", "ALFH", "JSBL", "FAYS", "SCBL", "CITI" };
            var bankCode = GetRandomElement(bankCodes);
            return $"PK{Random.Shared.Next(10, 99)}{bankCode}00{Random.Shared.NextInt64(100000000000000, 999999999999999)}";
        }
        
        private string GenerateUserId()
        {
            var prefixes = new[] { "user", "shop", "corp", "cust", "bus", "trade", "acc", "client", "merchant", "agent", "vendor", "retail" };
            return $"{GetRandomElement(prefixes)}{Random.Shared.Next(100, 99999)}";
        }

        private async Task NotifyNewResult(ApiConfiguration config, ApiRequestLog requestLog)
        {
            try
            {
                var resultData = new
                {
                    Id = requestLog.Id,
                    Name = config.Name,
                    ConfigurationId = config.Id,
                    IterationNumber = requestLog.IterationNumber,
                    RequestTimestamp = requestLog.RequestTimestamp,
                    ResponseTimeMs = requestLog.ResponseTimeMs,
                    IsSuccessful = requestLog.IsSuccessful,
                    StatusCode = requestLog.StatusCode,
                    ErrorMessage = requestLog.ErrorMessage
                };

                await _hubContext.Clients.Group("Dashboard").SendAsync("NewResult", resultData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification for new result");
            }
        }

        public override void Dispose()
        {
            CancellationTokenSource? tokenSourceToDispose = null;
            
            lock (_lockObject)
            {
                if (_disposed) return;
                
                _disposed = true;
                _isRunning = false;
                tokenSourceToDispose = _loopCancellationTokenSource;
                _loopCancellationTokenSource = null;
            }
            
            // Dispose outside the lock
            if (tokenSourceToDispose != null)
            {
                try
                {
                    if (!tokenSourceToDispose.IsCancellationRequested)
                    {
                        tokenSourceToDispose.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Token source already disposed - ignore
                }
                catch (Exception ex)
                {
                    try
                    {
                        _logger?.LogWarning(ex, "Error cancelling token source during disposal");
                    }
                    catch
                    {
                        // Logger may also be disposed, ignore
                    }
                }
                
                try
                {
                    tokenSourceToDispose.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Already disposed - ignore
                }
                catch (Exception ex)
                {
                    try
                    {
                        _logger?.LogWarning(ex, "Error disposing token source");
                    }
                    catch
                    {
                        // Logger may also be disposed, ignore
                    }
                }
            }
            
            try
            {
                base.Dispose();
            }
            catch (Exception ex)
            {
                try
                {
                    _logger?.LogError(ex, "Error in base.Dispose()");
                }
                catch
                {
                    // Logger may be disposed, ignore
                }
            }
        }
    }
}
