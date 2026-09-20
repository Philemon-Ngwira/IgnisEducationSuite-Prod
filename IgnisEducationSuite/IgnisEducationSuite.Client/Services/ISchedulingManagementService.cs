using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.Client.Services
{
    public interface ISchedulingManagementService
    {
        // Reference data
        Task<List<DayOptionDto>> ListDaysAsync();

        // Timetable activities
        Task<List<ActivityDto>> ListActivitiesAsync();
        Task<ActivityResult> CreateActivityAsync(ActivityRequest request);
        Task<ActivityResult> UpdateActivityAsync(Guid activityId, ActivityRequest request);
        Task<ActivityResult> DeleteActivityAsync(Guid activityId);

        // Time slots
        Task<List<TimeSlotDto>> ListTimeSlotsAsync();
        Task<TimeSlotResult> CreateTimeSlotAsync(TimeSlotRequest request);
        Task<TimeSlotResult> UpdateTimeSlotAsync(Guid timeslotId, TimeSlotRequest request);
        Task<TimeSlotResult> DeleteTimeSlotAsync(Guid timeslotId);

        // Subject scheduling policy
        Task<List<SubjectScheduleConfigDto>> ListPolicyAsync();
        Task<SubjectScheduleConfigResult> UpsertPolicyAsync(Guid classId, SubjectScheduleConfigRequest request);
        Task<BulkSubjectScheduleConfigResult> UpsertPolicyBulkAsync(BulkSubjectScheduleConfigRequest request);

        // Adjacency
        Task<List<SubjectAdjacencyRuleDto>> ListAdjacencyAsync();
        Task<bool> AddAdjacencyRuleAsync(Guid classId, Guid cannotFollowClassId);
        Task<bool> RemoveAdjacencyRuleAsync(Guid subjectAdjacencyRuleId);

        // Generation
        Task<GenerateScheduleResult> PreviewGenerationAsync(GenerateScheduleRequest request);
        Task<GenerateScheduleResult> ValidateScheduleAsync(ValidateScheduleRequest request);
        Task<SaveGeneratedScheduleResult> SaveScheduleAsync(SaveGeneratedScheduleRequest request);
        Task<List<ScheduledClassItem>> GetCurrentScheduleAsync(int academicLevel, Guid academicLevelSection);

        // Overrides
        Task<List<OverrideListItem>> ListOverridesAsync();
        Task<OverrideDetail?> GetOverrideAsync(Guid overrideId);
        Task<OverrideResult> CreateOverrideAsync(OverrideRequest request);
        Task<OverrideResult> UpdateOverrideAsync(Guid overrideId, OverrideRequest request);
        Task<bool> DeleteOverrideAsync(Guid overrideId);
    }
}
