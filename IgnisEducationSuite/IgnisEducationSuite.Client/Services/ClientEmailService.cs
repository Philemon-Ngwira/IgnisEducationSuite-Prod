using EDUSphereSharedProject.UniversalModels;
using System.Net.Http.Json;

namespace IgnisEducationSuite.Client.Services
{
    public class ClientEmailService
    {
        private readonly HttpClient _httpClient;

        public ClientEmailService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SendPasswordResetEmailAsync(EmailRequest email, string BaseUri)
        {
            var resetRequest = email;


            var response = await _httpClient.PostAsJsonAsync($"{BaseUri}api/Mail/ResetPassword", resetRequest);

            if (response.IsSuccessStatusCode)
            {
                return "Password reset email sent successfully!";
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return $"Error: {errorMessage}";
            }
        }
        public async Task<string> SendReportCardAsync(ReportCardEmailDTO reportCardEmailDTO, string BasUri)
        {
            var response = await _httpClient.PostAsJsonAsync($"{BasUri}api/Mail/SendReportCard", reportCardEmailDTO);
            if (response.IsSuccessStatusCode)
            {
                return "Report Card Email Sent Successfully";
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                return $"Error: {errorMessage}";
            }
        }
    }
}
