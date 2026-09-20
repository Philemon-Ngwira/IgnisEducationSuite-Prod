using System.Net.Http.Json;
using EDUSphereSharedProject.LicensingModel;

namespace IgnisEducationSuite.Client.Services
{
    public interface ILicenseClientService
    {
        /// <summary>The signed-in user's own school. No school id is sent — the server resolves it
        /// from the identity.</summary>
        Task<SchoolLicenseSummaryDto?> GetMySchoolAsync(bool refresh = false);
    }

    public class LicenseClientService : ILicenseClientService
    {
        private readonly HttpClient _http;

        public LicenseClientService(HttpClient http)
        {
            _http = http;
        }

        public async Task<SchoolLicenseSummaryDto?> GetMySchoolAsync(bool refresh = false)
        {
            try
            {
                return await _http.GetFromJsonAsync<SchoolLicenseSummaryDto>(
                    $"api/Verification/MySchool?refresh={(refresh ? "true" : "false")}");
            }
            catch (Exception)
            {
                // Null, not an empty summary. An empty one would render as a school with no students
                // and no licence, which is a claim this failed to establish rather than a fact.
                return null;
            }
        }
    }
}
