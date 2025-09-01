using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;

namespace FraudDetectorWebApp.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Add("X-Frame-Options", "DENY");
            context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
            context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Add("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            // Add Content Security Policy
            var csp = "default-src 'self'; " +
                     "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com; " +
                     "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com; " +
                     "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
                     "img-src 'self' data: https:; " +
                     "connect-src 'self' wss: ws:; " +
                     "frame-ancestors 'none';";
            
            context.Response.Headers.Add("Content-Security-Policy", csp);

            // Only add HSTS in production with HTTPS
            if (!context.Request.IsHttps && !IsLocalhost(context.Request))
            {
                context.Response.Headers.Add("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
            }

            await _next(context);
        }

        private static bool IsLocalhost(HttpRequest request)
        {
            return request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                   request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);
        }
    }

    public class ApiRateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiRateLimitingMiddleware> _logger;
        private static readonly Dictionary<string, UserRequestInfo> _requests = new();
        private readonly int _maxRequests = 100; // requests per minute
        private readonly TimeSpan _timeWindow = TimeSpan.FromMinutes(1);

        public ApiRateLimitingMiddleware(RequestDelegate next, ILogger<ApiRateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply rate limiting to API endpoints
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await _next(context);
                return;
            }

            var clientId = GetClientIdentifier(context);
            var now = DateTime.UtcNow;
            bool rateLimitExceeded = false;

            lock (_requests)
            {
                // Clean up old requests
                CleanupOldRequests(now);

                if (!_requests.ContainsKey(clientId))
                {
                    _requests[clientId] = new UserRequestInfo();
                }

                var userInfo = _requests[clientId];
                userInfo.RequestTimes.Add(now);

                var recentRequests = userInfo.RequestTimes.Count(t => now - t < _timeWindow);

                if (recentRequests > _maxRequests)
                {
                    rateLimitExceeded = true;
                    _logger.LogWarning("Rate limit exceeded for client {ClientId}. Requests: {RequestCount}", 
                        clientId, recentRequests);
                }
            }

            if (rateLimitExceeded)
            {
                context.Response.StatusCode = 429; // Too Many Requests
                context.Response.Headers.Add("Retry-After", _timeWindow.TotalSeconds.ToString());
                await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
                return;
            }

            await _next(context);
        }

        private string GetClientIdentifier(HttpContext context)
        {
            // Prefer user ID if authenticated, otherwise use IP
            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
                return $"user_{userId}";

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return $"ip_{ip}";
        }

        private void CleanupOldRequests(DateTime now)
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in _requests)
            {
                kvp.Value.RequestTimes.RemoveAll(t => now - t > _timeWindow);
                
                if (kvp.Value.RequestTimes.Count == 0)
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
            {
                _requests.Remove(key);
            }
        }

        private class UserRequestInfo
        {
            public List<DateTime> RequestTimes { get; set; } = new();
        }
    }

    public class AdminAccessLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AdminAccessLoggingMiddleware> _logger;

        public AdminAccessLoggingMiddleware(RequestDelegate next, ILogger<AdminAccessLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log access to admin pages and endpoints
            var path = context.Request.Path.Value?.ToLower();
            var isAdminPath = path?.Contains("/admin") == true || 
                             path?.Contains("/api/") == true && context.User.IsInRole("Admin");

            if (isAdminPath && context.User.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = context.User.FindFirstValue(ClaimTypes.Name) ?? context.User.Identity.Name;
                var userAgent = context.Request.Headers.UserAgent.ToString();
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();

                _logger.LogInformation("Admin access: User {UserId} ({UserName}) accessed {Path} from {IpAddress} using {UserAgent}",
                    userId, userName, context.Request.Path, ipAddress, userAgent);
            }

            await _next(context);
        }
    }

    public class CsrfProtectionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<CsrfProtectionMiddleware> _logger;

        public CsrfProtectionMiddleware(RequestDelegate next, IAntiforgery antiforgery, ILogger<CsrfProtectionMiddleware> logger)
        {
            _next = next;
            _antiforgery = antiforgery;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            var method = context.Request.Method.ToUpper();

            // Skip CSRF validation for GET, HEAD, OPTIONS, and TRACE
            if (method == "GET" || method == "HEAD" || method == "OPTIONS" || method == "TRACE")
            {
                await _next(context);
                return;
            }

            // Skip CSRF validation for API endpoints (they should use bearer tokens)
            if (path?.StartsWith("/api/") == true)
            {
                await _next(context);
                return;
            }

            // Skip CSRF validation for login/logout endpoints
            if (path?.Contains("/account/login") == true || path?.Contains("/account/logout") == true)
            {
                await _next(context);
                return;
            }

            // Validate CSRF token for POST/PUT/DELETE requests to pages
            if (context.User.Identity?.IsAuthenticated == true && 
                (method == "POST" || method == "PUT" || method == "DELETE" || method == "PATCH"))
            {
                try
                {
                    await _antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException)
                {
                    _logger.LogWarning("CSRF validation failed for user {User} accessing {Path}",
                        context.User.Identity.Name, context.Request.Path);
                    
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsync("CSRF validation failed");
                    return;
                }
            }

            await _next(context);
        }
    }

    public class IpWhitelistMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<IpWhitelistMiddleware> _logger;
        private readonly List<string> _allowedIps;

        public IpWhitelistMiddleware(RequestDelegate next, ILogger<IpWhitelistMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _allowedIps = configuration.GetSection("Security:AllowedIPs").Get<List<string>>() ?? new List<string>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply IP whitelist to admin API endpoints
            var path = context.Request.Path.Value?.ToLower();
            var isAdminApi = path?.StartsWith("/api/") == true && context.User.IsInRole("Admin");

            if (isAdminApi && _allowedIps.Any())
            {
                var clientIp = GetClientIpAddress(context);
                
                if (!IsIpAllowed(clientIp))
                {
                    _logger.LogWarning("Access denied from IP {IpAddress} to admin endpoint {Path}", 
                        clientIp, context.Request.Path);
                    
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Access denied");
                    return;
                }
            }

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP first (in case behind proxy/load balancer)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private bool IsIpAllowed(string clientIp)
        {
            // Always allow localhost for development
            if (clientIp == "127.0.0.1" || clientIp == "::1" || clientIp == "localhost")
                return true;

            return _allowedIps.Contains(clientIp);
        }
    }

    public class RequestSizeLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly long _maxRequestSize;
        private readonly ILogger<RequestSizeLimitMiddleware> _logger;

        public RequestSizeLimitMiddleware(RequestDelegate next, ILogger<RequestSizeLimitMiddleware> logger, long maxRequestSize = 10 * 1024 * 1024) // 10MB default
        {
            _next = next;
            _maxRequestSize = maxRequestSize;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.ContentLength.HasValue && context.Request.ContentLength.Value > _maxRequestSize)
            {
                _logger.LogWarning("Request size {Size} exceeds limit {Limit} for {Path}",
                    context.Request.ContentLength.Value, _maxRequestSize, context.Request.Path);
                
                context.Response.StatusCode = 413; // Payload Too Large
                await context.Response.WriteAsync("Request size exceeds the maximum allowed limit");
                return;
            }

            await _next(context);
        }
    }
}
