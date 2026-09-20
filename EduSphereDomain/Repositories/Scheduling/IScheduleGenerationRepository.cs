using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace EduSphereDomain.Repositories.Scheduling
{
    public interface IScheduleGenerationRepository
    {
        Task<SaveGeneratedScheduleResult> SaveGeneratedScheduleAsync(Guid schoolId, SaveGeneratedScheduleRequest request);
        Task<List<ScheduledClassItem>> GetCurrentScheduleAsync(Guid schoolId, int academicLevel, Guid academicLevelSection);

        // Overrides
        Task<List<OverrideListItem>> ListOverridesAsync(Guid schoolId);
        Task<OverrideDetail?> GetOverrideAsync(Guid overrideId);
        Task<Guid> CreateOverrideAsync(Guid? createdBy, OverrideRequest request);
        Task<bool> UpdateOverrideAsync(Guid overrideId, OverrideRequest request);
        Task<bool> DeleteOverrideAsync(Guid overrideId);
        Task<bool> OverrideOverlapsAsync(
            Guid academicLevelId, Guid levelSectionId, string studentGroup, Guid dayId, Guid timeSlotId,
            DateOnly? effectiveFrom, DateOnly? effectiveTo, Guid? excludingOverrideId);
        Task<bool> AcademicLevelBelongsToSchoolAsync(Guid academicLevelId, Guid schoolId);
        Task<Guid?> GetOverrideSchoolIdAsync(Guid overrideId);
    }
}
