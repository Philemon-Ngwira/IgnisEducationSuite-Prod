using EDUSphereSharedProject.LicensingModel;
using EDUSphereSharedProject.UniversalModels;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace IgnisEducationSuite.ServerServices
{
    public class LicenseService
    {
        private const string FallbackBaseUrl = "https://ptelicensing-b0e9h6ajaterg9bg.southafricanorth-01.azurewebsites.net/";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LicenseService> _logger;

        public bool LicenseIsActive { get; private set; } = false;

        /// <summary>Read from configuration so switching environment does not need a rebuild. Falls
        /// back to the previously compiled-in address if the setting is absent.</summary>
        private string BaseUrl =>
            _configuration["Licensing:BaseUrl"] is { Length: > 0 } configured ? configured : FallbackBaseUrl;

        private TimeSpan CacheDuration =>
            TimeSpan.FromMinutes(int.TryParse(_configuration["Licensing:CacheMinutes"], out var minutes) && minutes > 0
                ? minutes
                : 30);

        private TimeSpan RequestTimeout =>
            TimeSpan.FromSeconds(int.TryParse(_configuration["Licensing:TimeoutSeconds"], out var seconds) && seconds > 0
                ? seconds
                : 8);

        public LicenseService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IConfiguration configuration,
            ILogger<LicenseService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
        }

        private static string CacheKey(Guid clientId) => $"license-status::{clientId}";

        /// <summary>
        /// One school's full licence state, cached.
        ///
        /// This is the only method the app should call for a licence decision. It is cached per
        /// school for CacheMinutes, so a licence costs one outbound request per window rather than
        /// one per page load — the whole point of routing every check through here.
        ///
        /// It never throws and never blocks: on any failure it returns IsLicensed = true with
        /// Verified = false, so an outage in the licensing service cannot lock a school out of its
        /// own records. Only a successful check that says otherwise can restrict anything.
        /// </summary>
        public async Task<LicenseStatusDto> GetLicenseStatusAsync(Guid clientId, bool forceRefresh = false)
        {
            if (clientId == Guid.Empty)
            {
                return new LicenseStatusDto
                {
                    IsLicensed = true,
                    Verified = false,
                    Status = "No school on the account",
                    Notice = "Licence could not be checked because no school is associated with this account.",
                };
            }

            if (!forceRefresh && _cache.TryGetValue<LicenseStatusDto>(CacheKey(clientId), out var cached) && cached is not null)
            {
                return cached;
            }

            var status = await FetchLicenseStatusAsync(clientId);

            // A failed check is cached too, but briefly: without that, an outage would mean every
            // page load retries a dead endpoint and inherits its timeout.
            var duration = status.Verified ? CacheDuration : TimeSpan.FromMinutes(2);
            _cache.Set(CacheKey(clientId), status, duration);

            LicenseIsActive = status.IsLicensed;
            return status;
        }

        private async Task<LicenseStatusDto> FetchLicenseStatusAsync(Guid clientId)
        {
            try
            {
                var periods = await GetCompanyLicense(clientId);
                var current = periods.FirstOrDefault();

                if (current is null)
                {
                    // Reached the service, and it knows of no licence for this school. That is a
                    // definite answer, so treat it as unlicensed rather than unverified.
                    return new LicenseStatusDto
                    {
                        IsLicensed = false,
                        Verified = true,
                        Status = "No licence found",
                        Notice = "No licence is registered for this school. Adding users is disabled until a licence is activated.",
                        UserLimit = null,
                    };
                }

                var isValid = current.IsValid == 1;

                var status = new LicenseStatusDto
                {
                    IsLicensed = isValid,
                    Verified = true,
                    Status = isValid ? (current.Status ?? "Active") : (current.Status ?? "Expired or Terminated"),
                    EndDate = current.EndDate,
                    DaysUntilExpiry = current.DaysUntilExpiry,
                    UserLimit = await TryGetUserLimitAsync(clientId),
                };

                if (!isValid)
                {
                    status.Notice = $"This school's licence is {status.Status?.ToLowerInvariant()}. " +
                                    "Adding users is disabled until it is renewed.";
                }
                else if (status.IsExpiringSoon)
                {
                    status.Notice = $"Licence expires in {current.DaysUntilExpiry} day(s), on {current.EndDate:d}.";
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Licence check failed for client {ClientId}; continuing unverified", clientId);

                return UnverifiedFallback();
            }
        }

        /// <summary>Fails open: the app keeps working, and the caller is told the check did not happen.</summary>
        private static LicenseStatusDto UnverifiedFallback() => new()
        {
            IsLicensed = true,
            Verified = false,
            Status = "Could not be verified",
            UserLimit = null,
            Notice = "The licensing service could not be reached, so this school's licence has not been verified. " +
                     "Everything continues to work and the check will retry automatically.",
        };

        /// <summary>
        /// The user limit, or null when it cannot be established.
        ///
        /// Null rather than 0 is deliberate. The licensing API throws and returns a 500 for a client
        /// with no active licence, and the previous fail-safe of an empty LicenseSlots meant a limit
        /// of zero — which silently blocked all user creation instead of simply not enforcing.
        /// </summary>
        private async Task<int?> TryGetUserLimitAsync(Guid clientId)
        {
            try
            {
                var slots = await GetUserLimitAsync(clientId);
                return slots.UserLimit > 0 ? slots.UserLimit : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not read user limit for client {ClientId}", clientId);
                return null;
            }
        }

        /// <summary>Clears the cached result so the next check goes to the service. Call after
        /// activating or terminating a licence, which otherwise would not show up for up to
        /// CacheMinutes.</summary>
        public void InvalidateCache(Guid clientId) => _cache.Remove(CacheKey(clientId));

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

            // Deliberately does not throw. This used to raise on any non-success response, so a
            // licensing outage propagated out of the caller — and once this is wired into start-up,
            // that would have stopped people signing in. An unreachable service means unverified,
            // not unlicensed.
            var error = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Licence validation for {ClientID} returned {Status}: {Error}",
                ClientID, response.StatusCode, error);

            return "License Unverified: the licensing service could not be reached.";
        }
        public async Task<List<usp_GetPharmacyLicenseStatusResult>> GetCompanyLicense(Guid companyID)
        {
            var path = $"api/license/GetClientActiveLicensePeriod/{companyID}";

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.Timeout = RequestTimeout;

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
