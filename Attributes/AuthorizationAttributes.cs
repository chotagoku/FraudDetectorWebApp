using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace FraudDetectorWebApp.Attributes
{
    /// <summary>
    /// Enhanced admin authorization with additional security checks
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireAdminAttribute : Attribute, IAuthorizationFilter
    {
        public bool RequireActiveSessions { get; set; } = true;
        public bool LogAccess { get; set; } = true;
        public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours default

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Check if user is authenticated
            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check if user has Admin role
            if (!user.IsInRole("Admin"))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Additional session validation
            if (RequireActiveSessions)
            {
                var lastActivity = GetLastActivityFromClaims(user);
                if (lastActivity.HasValue && DateTime.UtcNow - lastActivity.Value > TimeSpan.FromMinutes(SessionTimeoutMinutes))
                {
                    // Session expired
                    context.Result = new UnauthorizedResult();
                    return;
                }
            }

            // Log admin access
            if (LogAccess)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireAdminAttribute>>();
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = user.FindFirstValue(ClaimTypes.Name) ?? user.Identity.Name;
                
                logger?.LogInformation("Admin access granted to {UserName} ({UserId}) for {Path}",
                    userName, userId, context.HttpContext.Request.Path);
            }
        }

        private DateTime? GetLastActivityFromClaims(ClaimsPrincipal user)
        {
            var lastActivityClaim = user.FindFirstValue("LastActivity");
            if (DateTime.TryParse(lastActivityClaim, out DateTime lastActivity))
            {
                return lastActivity;
            }
            return null;
        }
    }

    /// <summary>
    /// Requires specific permissions for fine-grained access control
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        public string Permission { get; }
        public string Resource { get; set; } = "";

        public RequirePermissionAttribute(string permission)
        {
            Permission = permission;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Check specific permission
            var hasPermission = HasPermission(user, Permission, Resource);
            if (!hasPermission)
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        private bool HasPermission(ClaimsPrincipal user, string permission, string resource)
        {
            // Get permission service from DI
            var httpContextAccessor = new HttpContextAccessor();
            var context = httpContextAccessor.HttpContext;
            if (context == null) return false;
            
            var permissionService = context.RequestServices.GetService<FraudDetectorWebApp.Services.IPermissionService>();
            if (permissionService == null)
            {
                // Fallback to simple role-based check
                var userRole = user.FindFirstValue(ClaimTypes.Role);
                return userRole == "Admin" || userRole == "SuperAdmin";
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
                return false;

            // Check permission asynchronously - this is a workaround for sync context
            try
            {
                return permissionService.HasPermissionAsync(userIdInt, permission).GetAwaiter().GetResult();
            }
            catch
            {
                // Fallback to role check
                var userRole = user.FindFirstValue(ClaimTypes.Role);
                return userRole == "Admin" || userRole == "SuperAdmin";
            }
        }
    }

    /// <summary>
    /// Rate limiting attribute for sensitive operations
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RateLimitAttribute : Attribute, IActionFilter
    {
        private static readonly Dictionary<string, UserRateLimit> _rateLimits = new();
        private readonly object _lock = new object();

        public int MaxRequests { get; set; } = 10;
        public int WindowMinutes { get; set; } = 60;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated == true)
                return;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
            var actionName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
            var key = $"{userId}:{actionName}";

            lock (_lock)
            {
                var now = DateTime.UtcNow;
                
                if (!_rateLimits.ContainsKey(key))
                {
                    _rateLimits[key] = new UserRateLimit();
                }

                var rateLimit = _rateLimits[key];
                
                // Remove old requests outside the window
                rateLimit.Requests.RemoveAll(r => now - r > TimeSpan.FromMinutes(WindowMinutes));
                
                if (rateLimit.Requests.Count >= MaxRequests)
                {
                    context.Result = new JsonResult(new { error = "Rate limit exceeded" })
                    {
                        StatusCode = 429
                    };
                    return;
                }

                rateLimit.Requests.Add(now);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Not needed for this implementation
        }

        private class UserRateLimit
        {
            public List<DateTime> Requests { get; set; } = new();
        }
    }

    /// <summary>
    /// Attribute for operations that should be logged for security auditing
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SecurityAuditAttribute : Attribute, IActionFilter
    {
        public string Operation { get; set; } = "";
        public string ResourceType { get; set; } = "";
        public bool LogRequestDetails { get; set; } = false;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;
            var logger = context.HttpContext.RequestServices.GetService<ILogger<SecurityAuditAttribute>>();

            if (logger == null || !user.Identity?.IsAuthenticated == true)
                return;

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = user.FindFirstValue(ClaimTypes.Name) ?? user.Identity.Name;
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = context.HttpContext.Request.Headers.UserAgent.ToString();

            var auditInfo = new
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Operation = Operation,
                ResourceType = ResourceType,
                Controller = context.Controller.GetType().Name,
                Action = context.ActionDescriptor.DisplayName,
                RequestPath = context.HttpContext.Request.Path,
                RequestMethod = context.HttpContext.Request.Method
            };

            logger.LogInformation("Security Audit: {@AuditInfo}", auditInfo);

            if (LogRequestDetails && context.ActionArguments.Any())
            {
                // Log action parameters (be careful with sensitive data)
                var sanitizedArgs = SanitizeArguments(context.ActionArguments);
                logger.LogInformation("Security Audit Parameters: {@Parameters}", sanitizedArgs);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<SecurityAuditAttribute>>();
            
            if (logger == null)
                return;

            var user = context.HttpContext.User;
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var result = new
            {
                Timestamp = DateTime.UtcNow,
                UserId = userId,
                Operation = Operation,
                Success = context.Exception == null,
                StatusCode = context.HttpContext.Response.StatusCode,
                Exception = context.Exception?.Message
            };

            if (context.Exception != null)
            {
                logger.LogError("Security Audit - Operation Failed: {@Result}", result);
            }
            else
            {
                logger.LogInformation("Security Audit - Operation Completed: {@Result}", result);
            }
        }

        private Dictionary<string, object> SanitizeArguments(IDictionary<string, object?> arguments)
        {
            var sanitized = new Dictionary<string, object>();
            var sensitiveKeys = new[] { "password", "token", "secret", "key", "credential" };

            foreach (var arg in arguments)
            {
                if (arg.Value == null)
                {
                    sanitized[arg.Key] = null!;
                    continue;
                }

                var key = arg.Key.ToLower();
                if (sensitiveKeys.Any(s => key.Contains(s)))
                {
                    sanitized[arg.Key] = "[REDACTED]";
                }
                else
                {
                    sanitized[arg.Key] = arg.Value;
                }
            }

            return sanitized;
        }
    }

    /// <summary>
    /// Attribute to validate API keys for external integrations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireApiKeyAttribute : Attribute, IAuthorizationFilter
    {
        public string HeaderName { get; set; } = "X-API-Key";
        public string ConfigurationKey { get; set; } = "ApiKeys:Default";

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var providedKey = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();
            
            if (string.IsNullOrEmpty(providedKey))
            {
                context.Result = new UnauthorizedObjectResult(new { error = "API key is required" });
                return;
            }

            var configuration = context.HttpContext.RequestServices.GetService<IConfiguration>();
            var validKeys = configuration?.GetSection("ApiKeys").GetChildren()
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrEmpty(x));

            if (validKeys?.Contains(providedKey) != true)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireApiKeyAttribute>>();
                logger?.LogWarning("Invalid API key attempt from {IpAddress}", 
                    context.HttpContext.Connection.RemoteIpAddress);

                context.Result = new UnauthorizedObjectResult(new { error = "Invalid API key" });
                return;
            }
        }
    }

    /// <summary>
    /// Attribute for operations that require additional confirmation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RequireConfirmationAttribute : Attribute, IActionFilter
    {
        public string ConfirmationParameter { get; set; } = "confirmed";
        public string ConfirmationValue { get; set; } = "true";

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var confirmationProvided = false;

            // Check in action parameters
            if (context.ActionArguments.TryGetValue(ConfirmationParameter, out var paramValue))
            {
                confirmationProvided = paramValue?.ToString()?.Equals(ConfirmationValue, StringComparison.OrdinalIgnoreCase) == true;
            }

            // Check in request headers
            if (!confirmationProvided)
            {
                var headerValue = context.HttpContext.Request.Headers[ConfirmationParameter].FirstOrDefault();
                confirmationProvided = headerValue?.Equals(ConfirmationValue, StringComparison.OrdinalIgnoreCase) == true;
            }

            if (!confirmationProvided)
            {
                context.Result = new BadRequestObjectResult(new { 
                    error = "This operation requires confirmation",
                    required_parameter = ConfirmationParameter,
                    required_value = ConfirmationValue
                });
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Not needed for this implementation
        }
    }

    /// <summary>
    /// Requires admin privileges for service management operations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireServiceManagementAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var permissionService = context.HttpContext.RequestServices.GetService<FraudDetectorWebApp.Services.IPermissionService>();
            if (permissionService == null)
            {
                // Fallback to role check
                if (!user.IsInRole("Admin") && !user.IsInRole("SuperAdmin"))
                {
                    context.Result = new ForbidResult();
                }
                return;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Check for service management permission
            try
            {
                var hasServiceManagement = permissionService.HasPermissionAsync(userIdInt, "SERVICE_MANAGEMENT").GetAwaiter().GetResult();
                var hasSystemAdmin = permissionService.HasPermissionAsync(userIdInt, "SYSTEM_ADMIN").GetAwaiter().GetResult();
                
                if (!hasServiceManagement && !hasSystemAdmin)
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = "Access denied. Service management requires elevated privileges.",
                        errorCode = "INSUFFICIENT_PRIVILEGES"
                    })
                    {
                        StatusCode = 403
                    };
                }
            }
            catch
            {
                // Fallback to role check
                if (!user.IsInRole("Admin") && !user.IsInRole("SuperAdmin"))
                {
                    context.Result = new ForbidResult();
                }
            }
        }
    }

    /// <summary>
    /// Logs admin actions for audit trail
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AuditAdminActionAttribute : ActionFilterAttribute
    {
        private readonly string _action;
        private readonly string _targetType;

        public AuditAdminActionAttribute(string action, string targetType)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            var permissionService = context.HttpContext.RequestServices.GetService<FraudDetectorWebApp.Services.IPermissionService>();
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var executedContext = await next();
            
            if (user.Identity?.IsAuthenticated == true && permissionService != null && 
                !string.IsNullOrEmpty(userId) && int.TryParse(userId, out var userIdInt))
            {
                var request = context.HttpContext.Request;
                var isSuccessful = executedContext.Exception == null && 
                                 (executedContext.Result is OkResult || 
                                  executedContext.Result is OkObjectResult || 
                                  executedContext.Result is JsonResult jsonResult && 
                                  (jsonResult.StatusCode == null || jsonResult.StatusCode == 200));

                var ipAddress = GetClientIpAddress(context.HttpContext);
                var userAgent = request.Headers.UserAgent.ToString();
                
                string? targetName = null;
                int? targetId = null;
                
                // Try to extract target info from route
                if (context.RouteData.Values.TryGetValue("id", out var routeId) && int.TryParse(routeId?.ToString(), out var id))
                {
                    targetId = id;
                }

                await permissionService.LogAdminActionAsync(
                    userIdInt, _action, _targetType, targetId, targetName,
                    description: $"Admin action: {_action} on {_targetType}",
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    isSuccessful: isSuccessful,
                    errorMessage: executedContext.Exception?.Message);
            }
        }

        private static string GetClientIpAddress(HttpContext context)
        {
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
    }

    /// <summary>
    /// Validates that the current user can modify the target user
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ValidateUserModificationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var user = context.HttpContext.User;

            if (!user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            var currentUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId) || !int.TryParse(currentUserId, out var currentUserIdInt))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Get target user ID from route
            if (context.RouteData.Values.TryGetValue("id", out var targetUserIdObj) &&
                int.TryParse(targetUserIdObj?.ToString(), out var targetUserId))
            {
                // Users can always modify themselves
                if (currentUserIdInt == targetUserId)
                {
                    return;
                }

                // Check if current user has permission to modify other users
                var permissionService = context.HttpContext.RequestServices.GetService<FraudDetectorWebApp.Services.IPermissionService>();
                if (permissionService != null)
                {
                    try
                    {
                        var hasUserManagement = permissionService.HasPermissionAsync(currentUserIdInt, "USER_MANAGEMENT").GetAwaiter().GetResult();
                        var hasSystemAdmin = permissionService.HasPermissionAsync(currentUserIdInt, "SYSTEM_ADMIN").GetAwaiter().GetResult();
                        
                        if (!hasUserManagement && !hasSystemAdmin)
                        {
                            context.Result = new JsonResult(new
                            {
                                success = false,
                                message = "Access denied. You don't have permission to modify other users.",
                                errorCode = "INSUFFICIENT_PRIVILEGES"
                            })
                            {
                                StatusCode = 403
                            };
                            return;
                        }
                    }
                    catch
                    {
                        // Fallback to role check
                        if (!user.IsInRole("Admin") && !user.IsInRole("SuperAdmin"))
                        {
                            context.Result = new ForbidResult();
                            return;
                        }
                    }
                }
                else
                {
                    // Fallback to role check
                    if (!user.IsInRole("Admin") && !user.IsInRole("SuperAdmin"))
                    {
                        context.Result = new ForbidResult();
                        return;
                    }
                }

                // Additional validation could be added here (e.g., prevent modifying super admins)
            }

            base.OnActionExecuting(context);
        }
    }
}
