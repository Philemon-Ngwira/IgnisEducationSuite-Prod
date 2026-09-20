using System.Net.Http.Json;
using EDUSphereSharedProject.UniversalModels.SuperAdmin;

namespace IgnisEducationSuite.Client.Services
{
    public interface ISuperAdminClientService
    {
        /// <param name="refresh">Bypasses the server's licence cache. Use for an explicit Refresh,
        /// not for a routine page load — every school then costs a real call to the licensing service.</param>
        Task<TenantPortfolioDto> GetPortfolioAsync(bool withLicenses = true, bool refresh = false);
        Task<TenantDetailDto?> GetTenantAsync(Guid schoolId);
        Task<List<TenantAdminDto>> ListAdminsAsync(Guid schoolId);

        Task<TenantActionResult> ActivateLicenseAsync(ActivateTenantLicenseRequest request);
        Task<TenantActionResult> TerminateLicenseAsync(Guid schoolId, string licenseKey);

        Task<TenantActionResult> SetAdminActiveAsync(Guid schoolId, string userId, bool active);
        Task<AdminPasswordResetResult> ResetAdminPasswordAsync(Guid schoolId, string userId);

        Task<TenantActionResult> SetManualPositionRecalculationAsync(Guid schoolId, bool enabled);
    }

    /// <summary>
    /// Client half of the SuperAdmin console.
    ///
    /// Reads swallow failures and return an empty result — a console tile that cannot load should
    /// show nothing rather than take the page down. Writes do the opposite: they always surface what
    /// happened, because a licence that silently failed to activate is worse than one that visibly
    /// did not.
    /// </summary>
    public class SuperAdminClientService : ISuperAdminClientService
    {
        private readonly HttpClient _http;

        public SuperAdminClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<TenantPortfolioDto> GetPortfolioAsync(bool withLicenses = true, bool refresh = false)
        {
            try
            {
                return await _http.GetFromJsonAsync<TenantPortfolioDto>(
                           $"api/SuperAdmin/tenants?withLicenses={Flag(withLicenses)}&refresh={Flag(refresh)}")
                       ?? new TenantPortfolioDto();
            }
            catch (Exception)
            {
                return new TenantPortfolioDto { LicensesIncluded = false };
            }
        }

        public async Task<TenantDetailDto?> GetTenantAsync(Guid schoolId)
        {
            try
            {
                return await _http.GetFromJsonAsync<TenantDetailDto>($"api/SuperAdmin/tenants/{schoolId}");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<TenantAdminDto>> ListAdminsAsync(Guid schoolId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<TenantAdminDto>>($"api/SuperAdmin/tenants/{schoolId}/admins")
                       ?? new List<TenantAdminDto>();
            }
            catch (Exception)
            {
                return new List<TenantAdminDto>();
            }
        }

        public Task<TenantActionResult> ActivateLicenseAsync(ActivateTenantLicenseRequest request) =>
            PostCoreAsync("api/SuperAdmin/license/activate", request);

        public Task<TenantActionResult> TerminateLicenseAsync(Guid schoolId, string licenseKey) =>
            PostCoreAsync("api/SuperAdmin/license/terminate",
                new TerminateTenantLicenseRequest { SchoolId = schoolId, LicenseKey = licenseKey });

        public Task<TenantActionResult> SetAdminActiveAsync(Guid schoolId, string userId, bool active) =>
            PostCoreAsync<object?>(
                $"api/SuperAdmin/tenants/{schoolId}/admins/{userId}/{(active ? "activate" : "deactivate")}", null);

        public Task<TenantActionResult> SetManualPositionRecalculationAsync(Guid schoolId, bool enabled) =>
            PostCoreAsync($"api/SuperAdmin/tenants/{schoolId}/settings/manual-position-recalculation",
                new SetManualPositionRecalculationRequest { Enabled = enabled });

        public async Task<AdminPasswordResetResult> ResetAdminPasswordAsync(Guid schoolId, string userId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync<object?>(
                    $"api/SuperAdmin/tenants/{schoolId}/admins/{userId}/reset-password", null);

                if (!response.IsSuccessStatusCode)
                {
                    return new AdminPasswordResetResult
                    {
                        Succeeded = false,
                        Message = Describe(response.StatusCode),
                    };
                }

                return await response.Content.ReadFromJsonAsync<AdminPasswordResetResult>()
                       ?? new AdminPasswordResetResult { Succeeded = false, Message = "The server returned no result." };
            }
            catch (Exception ex)
            {
                return new AdminPasswordResetResult { Succeeded = false, Message = ex.Message };
            }
        }

        private async Task<TenantActionResult> PostCoreAsync<T>(string url, T body)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(url, body);

                if (!response.IsSuccessStatusCode)
                    return TenantActionResult.Fail(Describe(response.StatusCode));

                return await response.Content.ReadFromJsonAsync<TenantActionResult>()
                       ?? TenantActionResult.Fail("The server returned no result.");
            }
            catch (Exception ex)
            {
                return TenantActionResult.Fail(ex.Message);
            }
        }

        /// <summary>Turns a status code into something an operator can act on. 403 in particular
        /// needs saying plainly — it means the signed-in account is not a SuperAdmin, not that the
        /// action failed.</summary>
        private static string Flag(bool value) => value ? "true" : "false";

        private static string Describe(System.Net.HttpStatusCode code) => code switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Your session has expired. Sign in again.",
            System.Net.HttpStatusCode.Forbidden => "This action requires a SuperAdmin account.",
            System.Net.HttpStatusCode.NotFound => "That school or account no longer exists.",
            _ => $"The request failed ({(int)code}).",
        };
    }
}
