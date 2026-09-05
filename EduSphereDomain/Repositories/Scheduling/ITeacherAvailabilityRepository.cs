namespace EduSphereDomain.Repositories.Scheduling
{
    public interface ITeacherAvailabilityRepository
    {
        /// <summary>
        /// Every teacher's already-committed (day, start time) slots across the school, in one
        /// round trip.
        ///
        /// <paramref name="excludeAcademicLevel"/> / <paramref name="excludeAcademicLevelSection"/>
        /// omit the section currently being regenerated. Without that exclusion a section that
        /// already has a saved timetable would see its own teachers as busy in every one of their
        /// existing periods, and regeneration would place almost nothing.
        /// </summary>
        Task<Dictionary<Guid, HashSet<(DayOfWeek Day, TimeSpan StartTime)>>> GetBusySlotsBySchoolAsync(
            Guid schoolId,
            int? excludeAcademicLevel = null,
            Guid? excludeAcademicLevelSection = null);
    }
}
