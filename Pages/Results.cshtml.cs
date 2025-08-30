using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FraudDetectorWebApp.Pages
{
    [Authorize]
    public class ResultsModel : PageModel
    {
        public int? ConfigurationId { get; set; }
        
        public void OnGet(int? configId = null)
        {
            ConfigurationId = configId;
        }
    }
}
