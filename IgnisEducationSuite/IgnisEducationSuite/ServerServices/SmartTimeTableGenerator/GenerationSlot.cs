namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Mutable per-slot working state used only during generation/repair/optimization — distinct
    /// from EDUSphereSharedProject.Models.TimeSlot (the DB entity for a school's daily time-of-day
    /// definitions) and TimeSlotDto (its read shape). One GenerationSlot exists per
    /// (day, base time slot) for the week being generated.
    /// </summary>
    public class GenerationSlot
    {
        public Guid TimeSlotId { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string SlotType { get; set; } = "";

        public Guid SubjectId { get; set; } = Guid.Empty;
        public string? SubjectName { get; set; }
        public Guid? ScheduledActivityId { get; set; }
        public string? ActivityName { get; set; }

        public bool IsLocked { get; set; }
        public bool IsDoublePeriod { get; set; }
        public bool IsFiller { get; set; }

        public bool IsFree => !IsLocked && SubjectId == Guid.Empty && ScheduledActivityId is null;
        public bool IsAcademic => SubjectId != Guid.Empty;
        public bool IsActivity => ScheduledActivityId is not null;
    }
}
