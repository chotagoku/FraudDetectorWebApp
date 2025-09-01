using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FraudDetectorWebApp.Services
{
    public class LiveDataService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LiveDataService> _logger;
        private readonly IHubContext<ApiTestHub> _hubContext;

        public LiveDataService(
            IServiceProvider serviceProvider,
            ILogger<LiveDataService> logger,
            IHubContext<ApiTestHub> hubContext)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Live Data Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessLiveData(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Update every 5 seconds
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Live Data Service");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Wait longer on error
                }
            }

            _logger.LogInformation("Live Data Service stopped");
        }

        private async Task ProcessLiveData(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Get real-time statistics
                var stats = await GetLiveStatistics(context);
                
                // Send live updates to all connected clients
                await _hubContext.Clients.All.SendAsync("LiveDataUpdate", new
                {
                    timestamp = DateTime.UtcNow,
                    statistics = stats,
                    systemStatus = GetSystemStatus()
                }, cancellationToken);

                _logger.LogDebug("Live data update sent to clients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing live data");
            }
        }

        private async Task<object> GetLiveStatistics(ApplicationDbContext context)
        {
            var now = DateTime.UtcNow;
            var last24Hours = now.AddHours(-24);
            var lastHour = now.AddHours(-1);

            // Get statistics for different time periods
            var totalRequests = await context.ApiRequestLogs
                .Where(r => !r.IsDeleted)
                .CountAsync();

            var last24HourRequests = await context.ApiRequestLogs
                .Where(r => !r.IsDeleted && r.RequestTimestamp >= last24Hours)
                .CountAsync();

            var lastHourRequests = await context.ApiRequestLogs
                .Where(r => !r.IsDeleted && r.RequestTimestamp >= lastHour)
                .CountAsync();

            var successfulRequests = await context.ApiRequestLogs
                .Where(r => !r.IsDeleted && r.IsSuccessful)
                .CountAsync();

            var avgResponseTime = await context.ApiRequestLogs
                .Where(r => !r.IsDeleted && r.IsSuccessful)
                .AverageAsync(r => (double?)r.ResponseTimeMs) ?? 0;

            var totalScenarios = await context.GeneratedScenarios
                .Where(s => !s.IsDeleted)
                .CountAsync();

            var testedScenarios = await context.GeneratedScenarios
                .Where(s => !s.IsDeleted && s.IsTested)
                .CountAsync();

            // Get recent activity (last 10 requests)
            var recentActivity = await context.ApiRequestLogs
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.RequestTimestamp)
                .Take(10)
                .Select(r => new
                {
                    r.Id,
                    r.RequestTimestamp,
                    r.IsSuccessful,
                    r.StatusCode,
                    r.ResponseTimeMs,
                    ConfigurationName = r.ApiConfiguration.Name
                })
                .ToListAsync();

            // Get hourly statistics for the last 24 hours
            var hourlyStats = await context.ApiRequestLogs
                .Where(r => !r.IsDeleted && r.RequestTimestamp >= last24Hours)
                .GroupBy(r => new { 
                    Hour = r.RequestTimestamp.Hour,
                    Date = r.RequestTimestamp.Date 
                })
                .Select(g => new
                {
                    Hour = g.Key.Hour,
                    Date = g.Key.Date,
                    Count = g.Count(),
                    SuccessCount = g.Count(r => r.IsSuccessful),
                    AvgResponseTime = g.Average(r => r.ResponseTimeMs)
                })
                .OrderByDescending(g => g.Date)
                .ThenByDescending(g => g.Hour)
                .Take(24)
                .ToListAsync();

            return new
            {
                overview = new
                {
                    totalRequests,
                    last24HourRequests,
                    lastHourRequests,
                    successfulRequests,
                    successRate = totalRequests > 0 ? (double)successfulRequests / totalRequests * 100 : 0,
                    avgResponseTime,
                    totalScenarios,
                    testedScenarios
                },
                recentActivity,
                hourlyStats,
                trends = new
                {
                    requestsPerHour = lastHourRequests,
                    requestsLast24h = last24HourRequests,
                    successRateLast24h = last24HourRequests > 0 
                        ? await context.ApiRequestLogs
                            .Where(r => !r.IsDeleted && r.RequestTimestamp >= last24Hours && r.IsSuccessful)
                            .CountAsync() / (double)last24HourRequests * 100 
                        : 0
                }
            };
        }

        private object GetSystemStatus()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            
            return new
            {
                processId = process.Id,
                startTime = process.StartTime,
                workingSet = process.WorkingSet64,
                cpuUsage = GetCpuUsage(),
                memoryUsage = GC.GetTotalMemory(false),
                threadCount = process.Threads.Count,
                uptime = DateTime.UtcNow - process.StartTime
            };
        }

        private double GetCpuUsage()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                return process.TotalProcessorTime.TotalMilliseconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}
