using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace FraudDetectorWebApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserManagementModel : PageModel
    {
        private readonly ILogger<UserManagementModel> _logger;

        public UserManagementModel(ILogger<UserManagementModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("User Management page accessed by {User}", User.Identity?.Name);
        }
    }
}
