using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FraudDetectorWebApp.Pages
{
    [Authorize]
    public class GeneratorModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
