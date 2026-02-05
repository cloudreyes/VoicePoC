using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoicePoC.Pages
{
    public class IndexModel(IConfiguration configuration) : PageModel
    {
        public string TwilioAccountSID { get; private set; } = configuration["TwilioAccountSID"] ?? "";
        public string TwilioAPIKey { get; private set; } = configuration["TwilioAPIKey"] ?? "";
        public string TwilioAPISecret { get; private set; } = configuration["TwilioAPISecret"] ?? "";
        public string TwilioAppID { get; private set; } = configuration["TwilioAppID"] ?? "";
        public string GeminiAPIKey { get; private set; } = configuration["GeminiAPIKey"] ?? "";
        public string FromPhoneNumber { get; private set; } = configuration["FromPhoneNumber"] ?? "";
        public string ToPhoneNumber { get; private set; } = configuration["ToPhoneNumber"] ?? "";

        public void OnGet()
        {

        }
    }
}
