using EDUSphereSharedProject.LicensingModel;
using EDUSphereSharedProject.UniversalModels;
using EDUSphereSharedProject.UniversalModels.SuperAdmin;
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
            _cache.Set(CacheKey(clientId), status, CacheDurationFor(status));

            LicenseIsActive = status.IsLicensed;
            return status;
        }

        /// <summary>
        /// How long to trust a result.
        ///
        /// Deliberately asymmetric. A working licence is the steady state and is held for the full
        /// window. The two states that take something away are held only briefly, so a wrong answer
        /// corrects itself in minutes rather than persisting for half an hour — the difference
        /// between a blip and a school locked out of adding users all morning.
        /// </summary>
        private TimeSpan CacheDurationFor(LicenseStatusDto status)
        {
            // Could not check: retry soon, but not on every page load — an outage would otherwise
            // mean every request inherits the timeout.
            if (!status.Verified) return TimeSpan.FromMinutes(2);

            // Checked, and restrictive. Also the state a licence renewal is meant to clear, and a
            // renewal made outside Ignis does not invalidate this cache.
            if (!status.IsLicensed) return TimeSpan.FromMinutes(5);

            return CacheDuration;
        }

        private async Task<LicenseStatusDto> FetchLicenseStatusAsync(Guid clientId)
        {
            try
            {
                var lookup = await TryGetCompanyLicenseAsync(clientId);

                // The service did not answer. This is the case that used to be reported as "no
                // licence found" with Verified = true — a licensed school shown as unlicensed, and
                // cached in that state for the full window because it claimed to be verified.
                // A timeout is not evidence of anything about the licence.
                if (!lookup.Answered)
                {
                    return UnverifiedFallback();
                }

                var current = SelectCurrentPeriod(lookup.Periods);

                if (current is null)
                {
                    // The service answered, and it knows of no licence for this school. Only now is
                    // "unlicensed" a fact rather than an assumption.
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
        /// <summary>
        /// The outcome of a licence lookup, keeping "the service answered" separate from "there are
        /// no periods".
        ///
        /// This distinction is the whole point of the type. Collapsing the two into an empty list
        /// meant every timeout, 502 and cold start was reported as a school with no licence — and,
        /// because that reads as a verified answer, it was then cached in that state.
        /// </summary>
        private readonly record struct LicenseLookup(
            bool Answered,
            List<usp_GetPharmacyLicenseStatusResult> Periods)
        {
            public static LicenseLookup Unreachable() => new(false, new());
            public static LicenseLookup Found(List<usp_GetPharmacyLicenseStatusResult> periods) => new(true, periods);
        }

        /// <summary>
        /// Licence periods for a school, reporting whether the service actually answered.
        ///
        /// A 200 is the only response treated as an answer. Everything else — including 404, which
        /// means the route or base address is wrong rather than that the school is unlicensed — is
        /// reported as unreachable so the caller fails open.
        /// </summary>
        private async Task<LicenseLookup> TryGetCompanyLicenseAsync(Guid companyID)
        {
            var path = $"api/license/GetClientActiveLicensePeriod/{companyID}";

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.Timeout = RequestTimeout;

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
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        return LicenseLookup.Found(licenses ?? new());
                    }

                    // Retrying an authorisation failure or a missing route will not change the
                    // answer, so stop — but report it as unreachable, not as "no licence". A 404
                    // here says this app is pointed at the wrong address; it says nothing about the
                    // school.
                    if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                            or System.Net.HttpStatusCode.Forbidden
                                            or System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning(
                            "Licence lookup for {ClientId} returned {Status}; treating as unverified",
                            companyID, response.StatusCode);

                        return LicenseLookup.Unreachable();
                    }

                    _logger.LogWarning("Licence lookup for {ClientId} returned {Status} (attempt {Attempt})",
                        companyID, response.StatusCode, attempt);
                }
                catch (TaskCanceledException)
                {
                    // Timeout. Common when the licensing service is cold-starting, which is exactly
                    // the case that must not be mistaken for an unlicensed school.
                    _logger.LogWarning("Licence lookup for {ClientId} timed out (attempt {Attempt})", companyID, attempt);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning(ex, "Licence lookup for {ClientId} failed (attempt {Attempt})", companyID, attempt);
                }
                catch (JsonException ex)
                {
                    // A malformed body is not an empty licence list.
                    _logger.LogWarning(ex, "Licence lookup for {ClientId} returned unreadable content", companyID);
                    return LicenseLookup.Unreachable();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Licence lookup for {ClientId} failed unexpectedly", companyID);
                    return LicenseLookup.Unreachable();
                }

                if (attempt < 3) await Task.Delay(300 * attempt);
            }

            return LicenseLookup.Unreachable();
        }

        /// <summary>
        /// The period that decides the licence.
        ///
        /// FirstOrDefault() used to take whatever the procedure happened to return first, so a
        /// school holding both a lapsed licence and its renewal could be judged on the lapsed one.
        /// A valid period always wins; failing that, the one that ran most recently, so the message
        /// describes the latest licence rather than an arbitrary old one.
        /// </summary>
        private static usp_GetPharmacyLicenseStatusResult? SelectCurrentPeriod(
            List<usp_GetPharmacyLicenseStatusResult> periods)
        {
            if (periods.Count == 0) return null;

            return periods
                .OrderByDescending(p => p.IsValid == 1)
                .ThenByDescending(p => p.EndDate)
                .First();
        }

        /// <summary>
        /// Licence periods for a school. Returns an empty list when the service cannot be reached,
        /// so callers cannot tell a failure from an unlicensed school — use
        /// <see cref="GetLicenseStatusAsync"/> for anything that makes a decision.
        /// </summary>
        public async Task<List<usp_GetPharmacyLicenseStatusResult>> GetCompanyLicense(Guid companyID) =>
            (await TryGetCompanyLicenseAsync(companyID)).Periods;

        /// <summary>
        /// Every licence ever issued to a school, newest first.
        ///
        /// For the SuperAdmin console only — it is the difference between "renew this school" and
        /// guessing. Deliberately uncached: history is read on demand when someone opens one school,
        /// not on the hot path, and a stale history right after an activation would be misleading.
        /// </summary>
        public async Task<List<TenantLicenseDto>> GetClientLicensesAsync(Guid clientId, int pageNumber = 1, int pageSize = 25)
        {
            if (clientId == Guid.Empty) return new List<TenantLicenseDto>();

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(BaseUrl);
            client.Timeout = RequestTimeout;

            var path = $"api/license/GetClientLicenses/{clientId}?pageNumber={pageNumber}&pageSize={pageSize}";

            using var response = await client.GetAsync(path);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Licence history for {ClientId} returned {Status}", clientId, response.StatusCode);
                return new List<TenantLicenseDto>();
            }

            var payload = await response.Content.ReadFromJsonAsync<TenantLicenseListDto>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return payload?.Licenses
                       .OrderByDescending(l => l.EndDate)
                       .ToList()
                   ?? new List<TenantLicenseDto>();
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
