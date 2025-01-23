using EDUSphereSharedProject.LicensingModel;

namespace IgnisEducationSuite.ServerServices
{
    public class LicenseService
    {
        private readonly HttpClient _httpClient;
        public bool LicenseIsActive { get; private set; } = false;

        public LicenseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> ActivateLicenseAsync(ActivateLicenseRequest request)
        { // Get token from environment variables
            var token = Environment.GetEnvironmentVariable("JWT_SECRET");
            //Debug//
            if (token == null)
            {
                token = "S1jNXM9kchEDJMfT@Kxu6FLvwnMY^TM9n@8Y";
            }
            // Add the token to the Authorization header
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync("https://philtiaraenterpriseslicensingapi.azurewebsites.net/api/license/activate", request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return $"License Activated: {result}";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to activate license: {response.ReasonPhrase} - {error}");
            }
        }

        public async Task<string> ValidateLicenseAsync(string ClientID)
        {
            if (string.IsNullOrEmpty(ClientID))
            {
                return "Not Found";
            }
            else
            {
                var path = $"https://philtiaraenterpriseslicensingapi.azurewebsites.net/api/license/validate?ClientID={ClientID}";
                var response = await _httpClient.GetAsync(path);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    if (result == "{\"status\":\"Expired or Terminated\"}")
                    {
                        LicenseIsActive = false;
                        return $"License Invalid: {result}";
                    }
                    else
                    {
                        LicenseIsActive = true;
                        return $"License Validated: {result}";
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to validate license: {response.ReasonPhrase} - {error}");
                }
            }
        }

        public async Task<string> TerminateLicenseAsync(TerminateLicenseRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("https://philtiaraenterpriseslicensingapi.azurewebsites.net/api/license/terminate", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return $"License Terminated: {result}";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to terminate license: {response.ReasonPhrase} - {error}");
            }
        }
    }
}

