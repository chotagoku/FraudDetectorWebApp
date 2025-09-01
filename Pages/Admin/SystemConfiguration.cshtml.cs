using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using FraudDetectorWebApp.Attributes;
using FraudDetectorWebApp.Services;
using System.Security.Claims;

namespace FraudDetectorWebApp.Pages.Admin
{
    [Authorize]
    [RequirePermission("SYSTEM_CONFIGURATION")]
    public class SystemConfigurationModel : PageModel
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<SystemConfigurationModel> _logger;

        public SystemConfigurationModel(IPermissionService permissionService, ILogger<SystemConfigurationModel> logger)
        {
            _permissionService = permissionService;
            _logger = logger;
        }

        public SystemConfigurationViewModel ViewModel { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
                {
                    return;
                }

                // Load user permissions for UI rendering
                var permissions = await _permissionService.GetUserPermissionsAsync(userIdInt);
                
                ViewModel.CanManageAdvancedSettings = permissions.Contains("SYSTEM_ADMIN") || 
                                                     permissions.Contains("ADVANCED_CONFIG");
                ViewModel.CanRestartServices = permissions.Contains("SERVICE_MANAGEMENT") || 
                                              permissions.Contains("SYSTEM_ADMIN");
                ViewModel.CanImportExportConfigs = permissions.Contains("CONFIG_IMPORT_EXPORT") || 
                                                   permissions.Contains("SYSTEM_ADMIN");
                ViewModel.CanApplyTemplates = permissions.Contains("CONFIG_TEMPLATES") || 
                                             permissions.Contains("SYSTEM_ADMIN");
                ViewModel.CanViewSystemHealth = permissions.Contains("SYSTEM_HEALTH") || 
                                               permissions.Contains("SYSTEM_ADMIN") ||
                                               permissions.Contains("SYSTEM_CONFIGURATION");
                
                ViewModel.UserPermissions = permissions;
                
                _logger.LogInformation("SystemConfiguration page loaded for user {UserId} with {PermissionCount} permissions", 
                    userIdInt, permissions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading SystemConfiguration page for user {UserId}", User.Identity?.Name);
                // Don't expose error details to user, but ensure basic permissions are set
                ViewModel.CanManageAdvancedSettings = User.IsInRole("Admin");
                ViewModel.CanRestartServices = User.IsInRole("Admin");
                ViewModel.CanImportExportConfigs = User.IsInRole("Admin");
                ViewModel.CanApplyTemplates = User.IsInRole("Admin");
                ViewModel.CanViewSystemHealth = User.IsInRole("Admin");
            }
        }
    }

    public class SystemConfigurationViewModel
    {
        public List<string> UserPermissions { get; set; } = new();
        public bool CanManageAdvancedSettings { get; set; }
        public bool CanRestartServices { get; set; }
        public bool CanImportExportConfigs { get; set; }
        public bool CanApplyTemplates { get; set; }
        public bool CanViewSystemHealth { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
