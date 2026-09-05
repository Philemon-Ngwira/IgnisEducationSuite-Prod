using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace EduSphereDomain.Repositories.Scheduling
{
    public interface ISchedulingConfigRepository
    {
        // Reference data
        Task<List<DayOptionDto>> ListDaysAsync();

        // Timetable activities
        Task<List<ActivityDto>> ListActivitiesAsync(Guid schoolId);
        Task<ActivityDto?> GetActivityAsync(Guid activityId);
        Task<Guid> CreateActivityAsync(Guid schoolId, ActivityRequest request);
        Task<bool> UpdateActivityAsync(Guid activityId, ActivityRequest request);
        Task<bool> DeleteActivityAsync(Guid activityId);
        Task<bool> ActivityHasDependenciesAsync(Guid activityId);

        // Time slots
        Task<List<TimeSlotDto>> ListTimeSlotsAsync(Guid schoolId);
        Task<TimeSlotDto?> GetTimeSlotAsync(Guid timeslotId);
        Task<Guid> CreateTimeSlotAsync(Guid schoolId, TimeSlotRequest request);
        Task<bool> UpdateTimeSlotAsync(Guid timeslotId, TimeSlotRequest request);
        Task<bool> DeleteTimeSlotAsync(Guid timeslotId);
        Task<bool> TimeSlotOverlapsAsync(Guid schoolId, TimeOnly start, TimeOnly end, Guid? excludingTimeslotId);
        Task<bool> TimeSlotHasDependenciesAsync(Guid timeslotId);

        // Subject scheduling policy
        Task<List<SubjectScheduleConfigDto>> ListPolicyAsync(Guid schoolId);
        Task<SubjectScheduleConfigDto?> GetPolicyAsync(Guid classId);
        Task UpsertPolicyAsync(Guid classId, Guid schoolId, SubjectScheduleConfigRequest request);

        /// <summary>Upserts many subjects' policy in one SaveChanges, so a section's worth of edits
        /// is a single round trip and a single transaction rather than one per subject.</summary>
        Task<int> UpsertPolicyBulkAsync(Guid schoolId, List<SubjectSchedulePolicyItem> items);

        /// <summary>The class ids from <paramref name="classIds"/> that belong to this school —
        /// used to reject a bulk save that references anything outside it.</summary>
        Task<HashSet<Guid>> FilterClassIdsInSchoolAsync(Guid schoolId, List<Guid> classIds);

        // Adjacency
        Task<List<SubjectAdjacencyRuleDto>> ListAdjacencyAsync(Guid schoolId);
        Task<SubjectAdjacencyRuleDto?> GetAdjacencyRuleAsync(Guid subjectAdjacencyRuleId);
        Task<bool> AdjacencyRuleExistsAsync(Guid classId, Guid cannotFollowClassId);
        Task AddAdjacencyRuleAsync(Guid schoolId, Guid classId, Guid cannotFollowClassId);
        Task<bool> RemoveAdjacencyRuleAsync(Guid subjectAdjacencyRuleId);
    }
}
