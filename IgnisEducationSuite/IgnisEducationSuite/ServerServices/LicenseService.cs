using EDUSphereSharedProject.LicensingModel;
using System.Text.Json;

namespace IgnisEducationSuite.ServerServices
{
    public class LicenseService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public bool LicenseIsActive { get; private set; } = false;

        //private const string BaseUrl = "https://licensingapi-a8gjawera8h5cefw.southafricanorth-01.azurewebsites.net/";
        private const string BaseUrl = "https://localhost:7207/";

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
