using System.ComponentModel.DataAnnotations;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    // ---------- Reference data ----------

    public class DayOptionDto
    {
        public Guid DayId { get; set; }
        public string? DayName { get; set; }
    }

    // ---------- Time Slots ----------

    public class TimeSlotDto
    {
        public Guid TimeslotId { get; set; }
        public Guid? SchoolId { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? Description { get; set; }
        public string? SlotType { get; set; }
        public int? MaxOccupancy { get; set; }
    }

    public class TimeSlotRequest
    {
        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        [StringLength(50)]
        public string? Description { get; set; }

        [Required, StringLength(50)]
        public string SlotType { get; set; } = "";

        public int? MaxOccupancy { get; set; }
    }

    public class TimeSlotResult
    {
        public bool Succeeded { get; set; }
        public Guid? TimeslotId { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // ---------- Subject scheduling policy ----------

    public class SubjectScheduleConfigDto
    {
        public Guid ClassId { get; set; }
        public Guid? SchoolId { get; set; }
        public string? SubjectName { get; set; }
        public string? LevelName { get; set; }

        /// <summary>Numeric grade, matched against ClassSchedule.AcademicLevel. Preferred over
        /// LevelName for scoping a generation run — it is the value actually stored on the schedule.</summary>
        public int? AcademicLevel { get; set; }

        public string? GradeSection { get; set; }

        /// <summary>
        /// Option-set marker. Only "All" (or unset) classes form the base timetable; a group-specific
        /// class — e.g. French taken by one group while another takes Chichewa in the same period —
        /// reaches students through a TimetableOverride, never through generation.
        /// </summary>
        public string? GroupName { get; set; }

        public Guid? TeacherId { get; set; }
        public string? TeacherName { get; set; }
        public bool IsCore { get; set; }
        public int WeeklyPeriods { get; set; }
        public int RequiredDoubles { get; set; }
        public SubjectTimePreference TimePreference { get; set; }
    }

    public class SubjectScheduleConfigRequest
    {
        public bool IsCore { get; set; }

        [Range(0, 50)]
        public int WeeklyPeriods { get; set; }

        [Range(0, 25)]
        public int RequiredDoubles { get; set; }

        public SubjectTimePreference TimePreference { get; set; } = SubjectTimePreference.Any;
    }

    public class SubjectScheduleConfigResult
    {
        public bool Succeeded { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>One subject's policy inside a bulk save.</summary>
    public class SubjectSchedulePolicyItem
    {
        [Required]
        public Guid ClassId { get; set; }

        public bool IsCore { get; set; }

        [Range(0, 50)]
        public int WeeklyPeriods { get; set; }

        [Range(0, 25)]
        public int RequiredDoubles { get; set; }

        public SubjectTimePreference TimePreference { get; set; } = SubjectTimePreference.Any;
    }

    public class BulkSubjectScheduleConfigRequest
    {
        public List<SubjectSchedulePolicyItem> Items { get; set; } = new();
    }

    /// <summary>
    /// All-or-nothing: every item is validated before anything is written, so a single bad row
    /// cannot leave the section half-saved and the admin unsure which subjects took effect.
    /// </summary>
    public class BulkSubjectScheduleConfigResult
    {
        public bool Succeeded { get; set; }
        public int SavedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class AddAdjacencyRuleRequest
    {
        [Required]
        public Guid ClassId { get; set; }

        [Required]
        public Guid CannotFollowClassId { get; set; }
    }

    public class SubjectAdjacencyRuleDto
    {
        public Guid SubjectAdjacencyRuleId { get; set; }
        public Guid SchoolId { get; set; }
        public Guid ClassId { get; set; }
        public string? SubjectName { get; set; }
        public Guid CannotFollowClassId { get; set; }
        public string? CannotFollowSubjectName { get; set; }
    }

    // ---------- Timetable Activities ----------

    public class ActivityDto
    {
        public Guid ActivityId { get; set; }
        public Guid? SchoolId { get; set; }
        public string? ActivityName { get; set; }
        public int? DefaultDuration { get; set; }
        public string? OptionalNotes { get; set; }
        public bool MustBeMorning { get; set; }
        public bool MustBeAfternoon { get; set; }

        /// <summary>When set, this activity locks exactly this slot (see PreferredDayId) instead of
        /// the whole morning/afternoon window - MustBeMorning/MustBeAfternoon are ignored.</summary>
        public Guid? PreferredTimeSlotId { get; set; }
        public string? PreferredTimeSlotLabel { get; set; }

        /// <summary>Only meaningful alongside PreferredTimeSlotId. Null means every weekday.</summary>
        public Guid? PreferredDayId { get; set; }
        public string? PreferredDayName { get; set; }
    }

    public class ActivityRequest
    {
        [Required, StringLength(100)]
        public string ActivityName { get; set; } = "";

        public int? DefaultDuration { get; set; }

        [StringLength(250)]
        public string? OptionalNotes { get; set; }

        public bool MustBeMorning { get; set; }
        public bool MustBeAfternoon { get; set; }

        public Guid? PreferredTimeSlotId { get; set; }
        public Guid? PreferredDayId { get; set; }
    }

    public class ActivityResult
    {
        public bool Succeeded { get; set; }
        public Guid? ActivityId { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // ---------- Generation ----------

    public class TimeTableActivityDto
    {
        public Guid ActivityId { get; set; }
        public string? ActivityName { get; set; }
        public bool MustBeMorning { get; set; }
        public bool MustBeAfternoon { get; set; }

        /// <summary>When set, takes priority over MustBeMorning/MustBeAfternoon - see
        /// TimetableGenerator.LockActivities.</summary>
        public Guid? PreferredTimeSlotId { get; set; }
        public DayOfWeek? PreferredDay { get; set; }
    }

    /// <summary>One section's full bundle for the generation engine - see SchedulingEngine.</summary>
    public class SectionScheduleContext
    {
        public Guid SchoolId { get; set; }
        public int AcademicLevel { get; set; }
        public Guid AcademicLevelSection { get; set; }
        public List<TimeSlotDto> TimeSlots { get; set; } = new();
        public List<SubjectScheduleConfigDto> Subjects { get; set; } = new();
        public List<SubjectAdjacencyRuleDto> AdjacencyRules { get; set; } = new();

        /// <summary>Activities (Prep, P.E., Assembly, ...) to lock into this run. Each activity's
        /// MustBeMorning/MustBeAfternoon flags bound the time window it locks - see
        /// TimetableGenerator.LockActivities. Multiple activities may be combined in one run as long
        /// as their windows don't overlap (validated by the caller before generation).</summary>
        public List<TimeTableActivityDto> Activities { get; set; } = new();
    }

    /// <summary>A sparse, per-run-only adjustment to a subject's persisted policy - not saved back
    /// to SubjectScheduleConfig.</summary>
    public class SubjectScheduleConfigOverride
    {
        public Guid ClassId { get; set; }
        public bool? IsCore { get; set; }
        public int? WeeklyPeriods { get; set; }
        public int? RequiredDoubles { get; set; }
        public SubjectTimePreference? TimePreference { get; set; }
    }

    public class GenerateScheduleRequest
    {
        [Required]
        public int AcademicLevel { get; set; }

        [Required]
        public Guid AcademicLevelSection { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>Optional - the activities (e.g. Prep, P.E.) to lock into this run's week.</summary>
        public List<Guid> ActivityIds { get; set; } = new();

        public List<SubjectScheduleConfigOverride> RunOverrides { get; set; } = new();
    }

    /// <summary>Re-validation of a board the admin has edited by hand, without regenerating it.</summary>
    public class ValidateScheduleRequest
    {
        [Required]
        public int AcademicLevel { get; set; }

        [Required]
        public Guid AcademicLevelSection { get; set; }

        public List<Guid> ActivityIds { get; set; } = new();
        public List<SubjectScheduleConfigOverride> RunOverrides { get; set; } = new();
        public List<GeneratedSlotDto> Slots { get; set; } = new();
    }

    public class GeneratedSlotDto
    {
        public DayOfWeek Day { get; set; }
        public Guid TimeSlotId { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? TeacherName { get; set; }
        public bool IsDoublePeriod { get; set; }
        public bool IsFiller { get; set; }
        public Guid? ScheduledActivityId { get; set; }
        public string? ActivityName { get; set; }
    }

    public class GenerateScheduleResult
    {
        public bool Success { get; set; }
        public List<GeneratedSlotDto> Slots { get; set; } = new();
        public List<string> Violations { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        /// <summary>Slot counts and other diagnostics from TimetableValidator.</summary>
        public Dictionary<string, int> Metrics { get; set; } = new();
    }

    public class SaveGeneratedScheduleRequest
    {
        [Required]
        public int AcademicLevel { get; set; }

        [Required]
        public Guid AcademicLevelSection { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public List<GeneratedSlotDto> Slots { get; set; } = new();
    }

    public class SaveGeneratedScheduleResult
    {
        public bool Succeeded { get; set; }
        public int RetiredRowCount { get; set; }

        /// <summary>Number of StudentClassSchedules rows rebuilt for the section. Zero after an
        /// otherwise successful save means no students are enrolled in the section - the timetable
        /// will not appear for anyone, so the UI surfaces it as a warning rather than a success.</summary>
        public int StudentLinkCount { get; set; }

        public List<string> Errors { get; set; } = new();
    }

    public class ScheduledClassItem
    {
        public Guid ClassScheduleId { get; set; }
        public string? DayName { get; set; }
        public Guid TimeSlotId { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? TeacherName { get; set; }
        public bool IsDoublePeriod { get; set; }
        public Guid? ActivityId { get; set; }
        public string? ActivityName { get; set; }
    }

    // ---------- Overrides ----------

    public class OverrideListItem
    {
        public Guid OverrideId { get; set; }
        public string? DayName { get; set; }
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
        public string? LevelName { get; set; }
        public string? SectionCode { get; set; }
        public string? StudentGroup { get; set; }
        public string? ReplacementClassName { get; set; }
        public string? Reason { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
        public bool IsActive { get; set; }
    }

    public class OverrideDetail
    {
        public Guid OverrideId { get; set; }
        public Guid AcademicLevelId { get; set; }
        public Guid LevelSectionId { get; set; }
        public string StudentGroup { get; set; } = "All";
        public Guid DayOfTheWeekId { get; set; }
        public Guid TimeSlotId { get; set; }
        public Guid ReplacementClassId { get; set; }
        public string? Reason { get; set; }
        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }

    public class OverrideRequest
    {
        [Required]
        public Guid AcademicLevelId { get; set; }

        [Required]
        public Guid LevelSectionId { get; set; }

        [Required, StringLength(50)]
        public string StudentGroup { get; set; } = "All";

        [Required]
        public Guid DayOfTheWeekId { get; set; }

        [Required]
        public Guid TimeSlotId { get; set; }

        [Required]
        public Guid ReplacementClassId { get; set; }

        [StringLength(255)]
        public string? Reason { get; set; }

        public DateOnly? EffectiveFrom { get; set; }
        public DateOnly? EffectiveTo { get; set; }
    }

    public class OverrideResult
    {
        public bool Succeeded { get; set; }
        public Guid? OverrideId { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
