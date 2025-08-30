using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FraudDetectorWebApp.Pages.Account
{
    public class RegisterModel : PageModel
    {
        public void OnGet()
        {
            // If user is already authenticated, redirect to home
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect("/");
                return;
            }
        }
    }
}
