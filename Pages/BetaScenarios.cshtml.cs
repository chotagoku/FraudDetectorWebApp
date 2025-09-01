using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FraudDetectorWebApp.Data;
using FraudDetectorWebApp.Models;
using FraudDetectorWebApp.Services;
using FraudDetectorWebApp.Attributes;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FraudDetectorWebApp.Pages
{
    [Authorize]
    [RequirePermission("BETA_SCENARIOS")]
    public class BetaScenariosModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<BetaScenariosModel> _logger;

        public BetaScenariosModel(ApplicationDbContext context, IPermissionService permissionService, 
            ILogger<BetaScenariosModel> logger)
        {
            _context = context;
            _permissionService = permissionService;
            _logger = logger;
        }

        public BetaScenariosViewModel ViewModel { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var userIdInt))
                {
                    return Unauthorized();
                }

                // Check permissions
                var hasPermission = await _permissionService.HasPermissionAsync(userIdInt, "BETA_SCENARIOS");
                if (!hasPermission)
                {
                    return Forbid();
                }

                // Load initial data
                await LoadViewModelDataAsync(userIdInt);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading BetaScenarios page for user {UserId}", User.Identity?.Name);
                TempData["ErrorMessage"] = "An error occurred while loading the scenarios page.";
                return Page();
            }
        }

        private async Task LoadViewModelDataAsync(int userId)
        {
            // Get user's permissions for UI rendering
            ViewModel.UserPermissions = await _permissionService.GetUserPermissionsAsync(userId);
            ViewModel.CanCreateScenarios = ViewModel.UserPermissions.Contains("GENERATE_SCENARIOS") || 
                                           ViewModel.UserPermissions.Contains("SYSTEM_ADMIN");
            ViewModel.CanTestScenarios = ViewModel.UserPermissions.Contains("TEST_SCENARIOS") || 
                                         ViewModel.UserPermissions.Contains("SYSTEM_ADMIN");
            ViewModel.CanManageScenarios = ViewModel.UserPermissions.Contains("USER_MANAGEMENT") || 
                                           ViewModel.UserPermissions.Contains("SYSTEM_ADMIN");

            // Get basic statistics
            var totalScenarios = await _context.BetaScenarios.CountAsync(s => !s.IsDeleted);
            var testedScenarios = await _context.BetaScenarios.CountAsync(s => !s.IsDeleted && s.IsTested);
            var highRiskScenarios = await _context.BetaScenarios.CountAsync(s => !s.IsDeleted && s.RiskLevel == "High");
            var favoriteScenarios = await _context.BetaScenarios.CountAsync(s => !s.IsDeleted && s.IsFavorite);

            ViewModel.Statistics = new BetaScenariosStatistics
            {
                TotalScenarios = totalScenarios,
                TestedScenarios = testedScenarios,
                HighRiskScenarios = highRiskScenarios,
                FavoriteScenarios = favoriteScenarios
            };

            // Get available API configurations for testing
            ViewModel.ApiConfigurations = await _context.ApiConfigurations
                .Where(c => !c.IsDeleted && c.IsActive)
                .Select(c => new ApiConfigurationSummary
                {
                    Id = c.Id,
                    Name = c.Name,
                    ApiEndpoint = c.ApiEndpoint
                })
                .ToListAsync();

            // Get categories and risk levels for filters
            ViewModel.Categories = await _context.BetaScenarios
                .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.Category))
                .Select(s => s.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewModel.RiskLevels = await _context.BetaScenarios
                .Where(s => !s.IsDeleted && !string.IsNullOrEmpty(s.RiskLevel))
                .Select(s => s.RiskLevel!)
                .Distinct()
                .OrderBy(r => r)
                .ToListAsync();

            // Initialize default filter values
            ViewModel.FilterOptions = new BetaScenariosFilterOptions
            {
                Page = 1,
                PageSize = 20,
                SortBy = "GeneratedAt",
                SortDirection = "desc"
            };

            _logger.LogInformation("BetaScenarios page loaded for user {UserId}. Total scenarios: {Count}", 
                userId, totalScenarios);
        }
    }

    public class BetaScenariosViewModel
    {
        public List<string> UserPermissions { get; set; } = new();
        public bool CanCreateScenarios { get; set; }
        public bool CanTestScenarios { get; set; }
        public bool CanManageScenarios { get; set; }
        public BetaScenariosStatistics Statistics { get; set; } = new();
        public List<ApiConfigurationSummary> ApiConfigurations { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public List<string> RiskLevels { get; set; } = new();
        public BetaScenariosFilterOptions FilterOptions { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }

    public class BetaScenariosStatistics
    {
        public int TotalScenarios { get; set; }
        public int TestedScenarios { get; set; }
        public int HighRiskScenarios { get; set; }
        public int FavoriteScenarios { get; set; }
        public double TestSuccessRate => TotalScenarios > 0 ? (double)TestedScenarios / TotalScenarios * 100 : 0;
        public double HighRiskPercentage => TotalScenarios > 0 ? (double)HighRiskScenarios / TotalScenarios * 100 : 0;
    }

    public class ApiConfigurationSummary
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
    }

    public class BetaScenariosFilterOptions
    {
        public string? Search { get; set; }
        public string? RiskLevel { get; set; }
        public string? Category { get; set; }
        public string? Status { get; set; }
        public bool? IsFavorite { get; set; }
        public bool? IsTested { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "GeneratedAt";
        public string SortDirection { get; set; } = "desc";
    }
}
