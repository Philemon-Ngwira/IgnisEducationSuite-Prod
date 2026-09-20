using System.Net.Http.Json;
using EDUSphereSharedProject.UniversalModels.ParentLinking;

namespace IgnisEducationSuite.Client.Services
{
    public interface IParentLinkingClientService
    {
        Task<UnparentedSummaryDto> GetSummaryAsync();
        Task<List<UnparentedStudentDto>> ListUnparentedAsync(int? academicLevel = null, string? gradeSection = null, string? search = null);
        Task<List<ParentOptionDto>> ListParentsAsync(string? search = null);
        Task<LinkStudentsToParentResult> LinkStudentsAsync(LinkStudentsToParentRequest request);
        Task<bool> UnlinkStudentAsync(Guid studentId);

        /// <summary>Runs class matching and reports what the student was enrolled in.</summary>
        Task<SyncStudentClassesResult> SyncStudentClassesAsync(Guid? studentId = null);

        Task<List<StudentGroupOptionDto>> ListGroupOptionsAsync();
    }

    public class ParentLinkingClientService : IParentLinkingClientService
    {
        private readonly HttpClient _http;

        public ParentLinkingClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<UnparentedSummaryDto> GetSummaryAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<UnparentedSummaryDto>("api/ParentLinking/summary")
                       ?? new UnparentedSummaryDto();
            }
            catch (Exception)
            {
                // The dashboard tile is informational; a failure here must not break the page.
                return new UnparentedSummaryDto();
            }
        }

        public async Task<List<UnparentedStudentDto>> ListUnparentedAsync(
            int? academicLevel = null, string? gradeSection = null, string? search = null)
        {
            var query = new List<string>();
            if (academicLevel.HasValue) query.Add($"academicLevel={academicLevel.Value}");
            if (!string.IsNullOrWhiteSpace(gradeSection)) query.Add($"gradeSection={Uri.EscapeDataString(gradeSection)}");
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");

            var url = "api/ParentLinking/unparented" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

            try
            {
                return await _http.GetFromJsonAsync<List<UnparentedStudentDto>>(url) ?? new List<UnparentedStudentDto>();
            }
            catch (Exception)
            {
                return new List<UnparentedStudentDto>();
            }
        }

        public async Task<List<ParentOptionDto>> ListParentsAsync(string? search = null)
        {
            var url = "api/ParentLinking/parents" +
                      (string.IsNullOrWhiteSpace(search) ? "" : $"?search={Uri.EscapeDataString(search)}");

            try
            {
                return await _http.GetFromJsonAsync<List<ParentOptionDto>>(url) ?? new List<ParentOptionDto>();
            }
            catch (Exception)
            {
                return new List<ParentOptionDto>();
            }
        }

        public async Task<LinkStudentsToParentResult> LinkStudentsAsync(LinkStudentsToParentRequest request)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/ParentLinking/link", request);

                if (!response.IsSuccessStatusCode)
                {
                    return new LinkStudentsToParentResult
                    {
                        Errors = { $"The server rejected the request ({(int)response.StatusCode})." },
                    };
                }

                return await response.Content.ReadFromJsonAsync<LinkStudentsToParentResult>()
                       ?? new LinkStudentsToParentResult { Errors = { "The server returned an empty response." } };
            }
            catch (Exception ex)
            {
                return new LinkStudentsToParentResult { Errors = { $"Could not reach the server: {ex.Message}" } };
            }
        }

        public async Task<SyncStudentClassesResult> SyncStudentClassesAsync(Guid? studentId = null)
        {
            var url = "api/ParentLinking/sync-classes" + (studentId.HasValue ? $"?studentId={studentId.Value}" : "");

            try
            {
                var response = await _http.PostAsync(url, null);

                if (!response.IsSuccessStatusCode)
                {
                    return new SyncStudentClassesResult
                    {
                        Errors = { $"Class matching was rejected by the server ({(int)response.StatusCode})." },
                    };
                }

                return await response.Content.ReadFromJsonAsync<SyncStudentClassesResult>()
                       ?? new SyncStudentClassesResult { Errors = { "The server returned an empty response." } };
            }
            catch (Exception ex)
            {
                return new SyncStudentClassesResult { Errors = { $"Could not reach the server: {ex.Message}" } };
            }
        }

        public async Task<List<StudentGroupOptionDto>> ListGroupOptionsAsync()
        {
            try
            {
                return await _http.GetFromJsonAsync<List<StudentGroupOptionDto>>("api/ParentLinking/group-options")
                       ?? new List<StudentGroupOptionDto>();
            }
            catch (Exception)
            {
                return new List<StudentGroupOptionDto>();
            }
        }

        public async Task<bool> UnlinkStudentAsync(Guid studentId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/ParentLinking/unlink",
                    new UnlinkStudentRequest { StudentId = studentId });

                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
