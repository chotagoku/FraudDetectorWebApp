using System.Text.Json;

namespace FraudDetectorWebApp.Middleware
{
    public class ErrorLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorLoggingMiddleware> _logger;

        public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await LogErrorAsync(context, ex);
                throw; // Re-throw to let other error handlers process it
            }
        }

        private Task LogErrorAsync(HttpContext context, Exception ex)
        {
            var requestId = context.TraceIdentifier;
            var userAgent = context.Request.Headers.UserAgent.ToString();
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;
            var userId = context.User?.Identity?.Name ?? "Anonymous";

            // Create structured log data (without sensitive information)
            var logData = new
            {
                RequestId = requestId,
                UserId = userId,
                RequestPath = requestPath.Value,
                RequestMethod = requestMethod,
                UserAgent = userAgent,
                IpAddress = ipAddress,
                ExceptionType = ex.GetType().Name,
                ExceptionMessage = ex.Message,
                StackTrace = ex.StackTrace
            };

            _logger.LogError(ex, 
                "Unhandled exception occurred. RequestId: {RequestId}, UserId: {UserId}, Path: {RequestPath}, Method: {RequestMethod}, IP: {IpAddress}, Exception: {ExceptionType}",
                requestId, userId, requestPath, requestMethod, ipAddress, ex.GetType().Name);

            // Log additional context in development
            if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            {
                _logger.LogDebug("Full error context: {ErrorContext}", JsonSerializer.Serialize(logData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                }));
            }
            
            return Task.CompletedTask;
        }
    }
}
