using System.Net.Http.Json;
using EDUSphereSharedProject.UniversalModels.ReportCards;

namespace IgnisEducationSuite.Client.Services
{
    /// <summary>
    /// Talks to api/ReportCardEntry. No call passes a school id or teacher id — the server derives
    /// those from the signed-in user. The active role IS passed, but only as an assertion the server
    /// verifies against the roles the account actually holds.
    /// </summary>
    public class ClientReportCardEntryService : IReportCardEntryService
    {
        private readonly HttpClient _http;

        public ClientReportCardEntryService(HttpClient http)
        {
            _http = http;
        }

        public async Task<ClassEntryScopeDto> ListClassesAsync(string reportCardType, bool includeAllClasses = false, string? activeRole = null)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<ClassEntryScopeDto>(
                    $"api/ReportCardEntry/classes/{Uri.EscapeDataString(reportCardType)}?all={includeAllClasses}{RoleQuery(activeRole, "&")}");

                return result ?? new ClassEntryScopeDto { Errors = { "The server returned an empty response." } };
            }
            catch (Exception ex)
            {
                return new ClassEntryScopeDto { Errors = { $"Could not load your classes: {ex.Message}" } };
            }
        }

        public async Task<ClassResultsSheetDto> GetClassSheetAsync(Guid classId, string reportCardType, string? activeRole = null)
        {
            try
            {
                var result = await _http.GetFromJsonAsync<ClassResultsSheetDto>(
                    $"api/ReportCardEntry/sheet/{classId}/{Uri.EscapeDataString(reportCardType)}{RoleQuery(activeRole, "?")}");

                return result ?? new ClassResultsSheetDto { Errors = { "The server returned an empty response." } };
            }
            catch (Exception ex)
            {
                return new ClassResultsSheetDto { Errors = { $"Could not load the class: {ex.Message}" } };
            }
        }

        public async Task<SaveClassResultsResult> SaveClassResultsAsync(SaveClassResultsRequest request, string? activeRole = null)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(
                    $"api/ReportCardEntry/save{RoleQuery(activeRole, "?")}", request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return new SaveClassResultsResult { Errors = { "Your session has expired. Please sign in again." } };

                if (!response.IsSuccessStatusCode)
                    return new SaveClassResultsResult { Errors = { $"The server rejected the request ({(int)response.StatusCode})." } };

                var result = await response.Content.ReadFromJsonAsync<SaveClassResultsResult>();
                return result ?? new SaveClassResultsResult { Errors = { "The server returned an empty response." } };
            }
            catch (Exception ex)
            {
                return new SaveClassResultsResult { Errors = { $"Could not reach the server: {ex.Message}" } };
            }
        }

        private static string RoleQuery(string? activeRole, string separator) =>
            string.IsNullOrWhiteSpace(activeRole) ? "" : $"{separator}role={Uri.EscapeDataString(activeRole)}";
    }
}
