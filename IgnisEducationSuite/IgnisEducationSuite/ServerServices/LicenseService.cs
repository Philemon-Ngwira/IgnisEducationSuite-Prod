using EDUSphereSharedProject.LicensingModel;
using EDUSphereSharedProject.UniversalModels;
using System.Text.Json;

namespace IgnisEducationSuite.ServerServices
{
    public class LicenseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public bool LicenseIsActive { get; private set; } = false;

        //private const string BaseUrl = "https://licensingapi-a8gjawera8h5cefw.southafricanorth-01.azurewebsites.net/";
        private const string BaseUrl = "https://ptelicensing-b0e9h6ajaterg9bg.southafricanorth-01.azurewebsites.net/";

        public LicenseService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // ---------------------------
        // IGNIS-ONLY METHOD
        // GET LICENSE SLOTS / USER LIMIT
        // ---------------------------
        public async Task<LicenseSlots> GetUserLimitAsync(Guid ClientID)
        {
            if (ClientID == Guid.Empty)
                return new LicenseSlots();

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);

            var path = $"api/license/GetLicenseLimit?ClientID={ClientID}";
            var response = await client.GetAsync(path);

            if (!response.IsSuccessStatusCode)
                return new LicenseSlots(); // fail-safe

            var created = await response.Content.ReadFromJsonAsync<LicenseSlots>();
            return created ?? new LicenseSlots();
        }

        // ---------------------------
        // ACTIVATE LICENSE
        // ---------------------------
        public async Task<string> ActivateLicenseAsync(ActivateLicenseRequest request)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);

            var token = Environment.GetEnvironmentVariable("JWT_SECRET")
                       ?? "S1jNXM9kchEDJMfT@Kxu6FLvwnMY^TM9n@8Y";

            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsJsonAsync("api/license/activate", request);

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

        // ---------------------------
        // VALIDATE LICENSE
        // ---------------------------
        public async Task<string> ValidateLicenseAsync(string ClientID)
        {
            if (string.IsNullOrEmpty(ClientID))
                return "Not Found";

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);

            var path = $"api/license/validate?ClientID={ClientID}";
            var response = await client.GetAsync(path);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();

                if (result.Contains("Expired"))
                {
                    LicenseIsActive = false;
                    return $"License Invalid: {result}";
                }

                LicenseIsActive = true;
                return $"License Validated: {result}";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to validate license: {response.ReasonPhrase} - {error}");
            }
        }
        public async Task<List<usp_GetPharmacyLicenseStatusResult>> GetCompanyLicense(Guid companyID)
        {
            var path = $"api/license/GetClientActiveLicensePeriod/{companyID}";

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://ptelicensing-b0e9h6ajaterg9bg.southafricanorth-01.azurewebsites.net/");
            client.Timeout = TimeSpan.FromSeconds(8);

            List<usp_GetPharmacyLicenseStatusResult> empty = new();

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var response = await client.GetAsync(path);

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();

                        var licenses = JsonSerializer.Deserialize<List<usp_GetPharmacyLicenseStatusResult>>(
                            content,
                            new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                        return licenses ?? empty;
                    }

                    // If unauthorized or not found → don't retry aggressively
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                        response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                        response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return empty;
                    }
                }
                catch (TaskCanceledException)
                {
                    // timeout → retry
                }
                catch (HttpRequestException)
                {
                    // network failure → retry
                }
                catch (Exception)
                {
                    // unknown error → break early (don't loop forever)
                    break;
                }

                await Task.Delay(300 * attempt); // exponential backoff
            }

            // Final fallback (never throw to caller)
            return empty;
        }

        // ---------------------------
        // TERMINATE LICENSE
        // ---------------------------
        public async Task<string> TerminateLicenseAsync(TerminateLicenseRequest request)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);

            var response = await client.PostAsJsonAsync("api/license/terminate", request);

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
