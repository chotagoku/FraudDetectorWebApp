using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace FraudDetectorWebApp.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class SystemLogsModel : PageModel
    {
        public void OnGet()
        {
            // Page initialization logic can go here if needed
            // Currently, all functionality is handled via JavaScript/API calls
        }
    }
}
