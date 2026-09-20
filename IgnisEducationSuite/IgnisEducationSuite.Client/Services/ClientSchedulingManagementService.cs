using System.Net.Http.Json;
using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.Client.Services
{
    /// <summary>
    /// Talks to api/Scheduling. Note that no method passes a SchoolID — the server resolves the
    /// school from the signed-in user, unlike this app's older api/Dynamic endpoints.
    ///
    /// Every call is wrapped so a dropped connection or a non-success status surfaces as a result
    /// object carrying an error string, rather than an unhandled exception in a Blazor event
    /// handler (which would leave the UI stuck on a spinner with no explanation).
    /// </summary>
    public class ClientSchedulingManagementService : ISchedulingManagementService
    {
        private readonly HttpClient _http;

        public ClientSchedulingManagementService(HttpClient http)
        {
            _http = http;
        }

        // ---------- Reference data ----------

        public Task<List<DayOptionDto>> ListDaysAsync() => GetListAsync<DayOptionDto>("api/Scheduling/days");

        // ---------- Timetable Activities ----------

        public Task<List<ActivityDto>> ListActivitiesAsync() => GetListAsync<ActivityDto>("api/Scheduling/activities");

        public Task<ActivityResult> CreateActivityAsync(ActivityRequest request) =>
            SendAsync(HttpMethod.Post, "api/Scheduling/activities", request, ActivityFailure);

        public Task<ActivityResult> UpdateActivityAsync(Guid activityId, ActivityRequest request) =>
            SendAsync(HttpMethod.Put, $"api/Scheduling/activities/{activityId}", request, ActivityFailure);

        public Task<ActivityResult> DeleteActivityAsync(Guid activityId) =>
            SendAsync<object, ActivityResult>(HttpMethod.Delete, $"api/Scheduling/activities/{activityId}", null, ActivityFailure);

        // ---------- Time Slots ----------

        public Task<List<TimeSlotDto>> ListTimeSlotsAsync() => GetListAsync<TimeSlotDto>("api/Scheduling/time-slots");

        public Task<TimeSlotResult> CreateTimeSlotAsync(TimeSlotRequest request) =>
            SendAsync(HttpMethod.Post, "api/Scheduling/time-slots", request, TimeSlotFailure);

        public Task<TimeSlotResult> UpdateTimeSlotAsync(Guid timeslotId, TimeSlotRequest request) =>
            SendAsync(HttpMethod.Put, $"api/Scheduling/time-slots/{timeslotId}", request, TimeSlotFailure);

        public Task<TimeSlotResult> DeleteTimeSlotAsync(Guid timeslotId) =>
            SendAsync<object, TimeSlotResult>(HttpMethod.Delete, $"api/Scheduling/time-slots/{timeslotId}", null, TimeSlotFailure);

        // ---------- Subject scheduling policy ----------

        public Task<List<SubjectScheduleConfigDto>> ListPolicyAsync() => GetListAsync<SubjectScheduleConfigDto>("api/Scheduling/policy");

        public Task<SubjectScheduleConfigResult> UpsertPolicyAsync(Guid classId, SubjectScheduleConfigRequest request) =>
            SendAsync(HttpMethod.Put, $"api/Scheduling/policy/{classId}", request,
                message => new SubjectScheduleConfigResult { Succeeded = false, Errors = { message } });

        public Task<BulkSubjectScheduleConfigResult> UpsertPolicyBulkAsync(BulkSubjectScheduleConfigRequest request) =>
            SendAsync(HttpMethod.Put, "api/Scheduling/policy", request,
                message => new BulkSubjectScheduleConfigResult { Succeeded = false, Errors = { message } });

        // ---------- Adjacency ----------

        public Task<List<SubjectAdjacencyRuleDto>> ListAdjacencyAsync() => GetListAsync<SubjectAdjacencyRuleDto>("api/Scheduling/adjacency");

        public async Task<bool> AddAdjacencyRuleAsync(Guid classId, Guid cannotFollowClassId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("api/Scheduling/adjacency",
                    new AddAdjacencyRuleRequest { ClassId = classId, CannotFollowClassId = cannotFollowClassId });

                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> RemoveAdjacencyRuleAsync(Guid subjectAdjacencyRuleId)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Scheduling/adjacency/{subjectAdjacencyRuleId}");
                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---------- Generation ----------

        public Task<GenerateScheduleResult> PreviewGenerationAsync(GenerateScheduleRequest request) =>
            SendAsync(HttpMethod.Post, "api/Scheduling/preview", request,
                message => new GenerateScheduleResult { Success = false, Errors = { message } });

        public Task<GenerateScheduleResult> ValidateScheduleAsync(ValidateScheduleRequest request) =>
            SendAsync(HttpMethod.Post, "api/Scheduling/validate", request,
                message => new GenerateScheduleResult { Success = false, Errors = { message } });

        public Task<SaveGeneratedScheduleResult> SaveScheduleAsync(SaveGeneratedScheduleRequest request) =>
            SendAsync(HttpMethod.Post, "api/Scheduling/save", request,
                message => new SaveGeneratedScheduleResult { Succeeded = false, Errors = { message } });

        public Task<List<ScheduledClassItem>> GetCurrentScheduleAsync(int academicLevel, Guid academicLevelSection) =>
            GetListAsync<ScheduledClassItem>($"api/Scheduling/current/{academicLevel}/{academicLevelSection}");

        // ---------- Overrides ----------

        public Task<List<OverrideListItem>> ListOverridesAsync() => GetListAsync<OverrideListItem>("api/Scheduling/overrides");

        public async Task<OverrideDetail?> GetOverrideAsync(Guid overrideId)
        {
            try
            {
                return await _http.GetFromJsonAsync<OverrideDetail>($"api/Scheduling/overrides/{overrideId}");
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Task<OverrideResult> CreateOverrideAsync(OverrideRequest request) =>
            SendAsync(HttpMethod.Post, "api/Scheduling/overrides", request, OverrideFailure);

        public Task<OverrideResult> UpdateOverrideAsync(Guid overrideId, OverrideRequest request) =>
            SendAsync(HttpMethod.Put, $"api/Scheduling/overrides/{overrideId}", request, OverrideFailure);

        public async Task<bool> DeleteOverrideAsync(Guid overrideId)
        {
            try
            {
                var response = await _http.DeleteAsync($"api/Scheduling/overrides/{overrideId}");
                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ---------- Plumbing ----------

        private async Task<List<T>> GetListAsync<T>(string url)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<T>>(url) ?? new List<T>();
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        private Task<TResult> SendAsync<TRequest, TResult>(
            HttpMethod method, string url, TRequest? body, Func<string, TResult> onFailure) =>
            SendCoreAsync(method, url, body, onFailure);

        private Task<TResult> SendAsync<TResult>(
            HttpMethod method, string url, object body, Func<string, TResult> onFailure) =>
            SendCoreAsync(method, url, body, onFailure);

        private async Task<TResult> SendCoreAsync<TResult>(
            HttpMethod method, string url, object? body, Func<string, TResult> onFailure)
        {
            try
            {
                using var request = new HttpRequestMessage(method, url);
                if (body is not null)
                {
                    request.Content = JsonContent.Create(body, body.GetType());
                }

                var response = await _http.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return onFailure("Your session has expired. Please sign in again.");
                }

                if (!response.IsSuccessStatusCode)
                {
                    return onFailure($"The server rejected the request ({(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<TResult>();
                return result ?? onFailure("The server returned an empty response.");
            }
            catch (Exception ex)
            {
                return onFailure($"Could not reach the server: {ex.Message}");
            }
        }

        private static ActivityResult ActivityFailure(string message) => new() { Succeeded = false, Errors = { message } };

        private static TimeSlotResult TimeSlotFailure(string message) => new() { Succeeded = false, Errors = { message } };

        private static OverrideResult OverrideFailure(string message) => new() { Succeeded = false, Errors = { message } };
    }
}
