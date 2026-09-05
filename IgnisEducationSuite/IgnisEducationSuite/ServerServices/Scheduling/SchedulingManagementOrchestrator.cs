using EduSphereDomain.Repositories;
using EduSphereDomain.Repositories.Scheduling;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;

namespace IgnisEducationSuite.ServerServices.Scheduling
{
    /// <summary>
    /// Every method takes the requesting user's id and resolves their school server-side, so a
    /// caller cannot reach another school's scheduling data by changing an id in the request. The
    /// rest of this app's older endpoints take SchoolID from the client; new scheduling endpoints
    /// deliberately do not.
    /// </summary>
    public class SchedulingManagementOrchestrator
    {
        private readonly ISchedulingConfigRepository _configRepository;
        private readonly IScheduleGenerationRepository _generationRepository;
        private readonly ITeacherAvailabilityRepository _teacherAvailabilityRepository;
        private readonly SchedulingEngine _engine;
        private readonly EduSphereRepository _repository;

        public SchedulingManagementOrchestrator(
            ISchedulingConfigRepository configRepository,
            IScheduleGenerationRepository generationRepository,
            ITeacherAvailabilityRepository teacherAvailabilityRepository,
            SchedulingEngine engine,
            EduSphereRepository repository)
        {
            _configRepository = configRepository;
            _generationRepository = generationRepository;
            _teacherAvailabilityRepository = teacherAvailabilityRepository;
            _engine = engine;
            _repository = repository;
        }

        // ---------- Reference data ----------

        public async Task<List<DayOptionDto>> ListDaysAsync(string requestingUserId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            return schoolId is null ? new List<DayOptionDto>() : await _configRepository.ListDaysAsync();
        }

        // ---------- Timetable Activities ----------

        public async Task<List<ActivityDto>> ListActivitiesAsync(string requestingUserId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            return schoolId is null ? new List<ActivityDto>() : await _configRepository.ListActivitiesAsync(schoolId.Value);
        }

        public async Task<ActivityResult> CreateActivityAsync(string requestingUserId, ActivityRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new ActivityResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var validationError = await ValidateActivityRequestAsync(schoolId.Value, request);
            if (validationError is not null)
                return new ActivityResult { Succeeded = false, Errors = { validationError } };

            var activityId = await _configRepository.CreateActivityAsync(schoolId.Value, request);
            return new ActivityResult { Succeeded = true, ActivityId = activityId };
        }

        public async Task<ActivityResult> UpdateActivityAsync(string requestingUserId, Guid activityId, ActivityRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new ActivityResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var current = await _configRepository.GetActivityAsync(activityId);
            if (current is null || current.SchoolId != schoolId)
                return new ActivityResult { Succeeded = false, Errors = { "Activity not found." } };

            var validationError = await ValidateActivityRequestAsync(schoolId.Value, request);
            if (validationError is not null)
                return new ActivityResult { Succeeded = false, Errors = { validationError } };

            var updated = await _configRepository.UpdateActivityAsync(activityId, request);
            return new ActivityResult { Succeeded = updated, ActivityId = activityId };
        }

        public async Task<ActivityResult> DeleteActivityAsync(string requestingUserId, Guid activityId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new ActivityResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var current = await _configRepository.GetActivityAsync(activityId);
            if (current is null || current.SchoolId != schoolId)
                return new ActivityResult { Succeeded = false, Errors = { "Activity not found." } };

            if (await _configRepository.ActivityHasDependenciesAsync(activityId))
                return new ActivityResult { Succeeded = false, Errors = { "This activity is used by a saved timetable and cannot be deleted." } };

            var deleted = await _configRepository.DeleteActivityAsync(activityId);
            return new ActivityResult { Succeeded = deleted };
        }

        private async Task<string?> ValidateActivityRequestAsync(Guid schoolId, ActivityRequest request)
        {
            if (request.PreferredTimeSlotId is not null && (request.MustBeMorning || request.MustBeAfternoon))
                return "Choose either a specific time slot or a morning/afternoon restriction, not both.";

            if (request.MustBeMorning && request.MustBeAfternoon)
                return "An activity cannot be restricted to both the morning and the afternoon.";

            if (request.PreferredDayId is not null && request.PreferredTimeSlotId is null)
                return "A specific day requires a specific time slot.";

            if (request.PreferredTimeSlotId is not null)
            {
                var slot = await _configRepository.GetTimeSlotAsync(request.PreferredTimeSlotId.Value);
                if (slot is null || slot.SchoolId != schoolId)
                    return "Selected time slot not found.";

                if (slot.SlotType is null || !SlotTypes.Teaching.Contains(slot.SlotType))
                    return $"'{slot.Description ?? "That slot"}' is a {slot.SlotType} slot. Activities can only be locked to teaching slots.";
            }

            if (request.PreferredDayId is not null)
            {
                var days = await _configRepository.ListDaysAsync();
                if (!days.Any(d => d.DayId == request.PreferredDayId.Value))
                    return "Selected day not found.";
            }

            return null;
        }

        // ---------- Time Slots ----------

        public async Task<List<TimeSlotDto>> ListTimeSlotsAsync(string requestingUserId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            return schoolId is null ? new List<TimeSlotDto>() : await _configRepository.ListTimeSlotsAsync(schoolId.Value);
        }

        public async Task<TimeSlotResult> CreateTimeSlotAsync(string requestingUserId, TimeSlotRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new TimeSlotResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var error = await ValidateTimeSlotAsync(schoolId.Value, request, excludingTimeslotId: null);
            if (error is not null)
                return new TimeSlotResult { Succeeded = false, Errors = { error } };

            var timeslotId = await _configRepository.CreateTimeSlotAsync(schoolId.Value, request);
            return new TimeSlotResult { Succeeded = true, TimeslotId = timeslotId };
        }

        public async Task<TimeSlotResult> UpdateTimeSlotAsync(string requestingUserId, Guid timeslotId, TimeSlotRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new TimeSlotResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var current = await _configRepository.GetTimeSlotAsync(timeslotId);
            if (current is null || current.SchoolId != schoolId)
                return new TimeSlotResult { Succeeded = false, Errors = { "Time slot not found." } };

            var error = await ValidateTimeSlotAsync(schoolId.Value, request, timeslotId);
            if (error is not null)
                return new TimeSlotResult { Succeeded = false, Errors = { error } };

            var updated = await _configRepository.UpdateTimeSlotAsync(timeslotId, request);
            return new TimeSlotResult { Succeeded = updated, TimeslotId = timeslotId };
        }

        public async Task<TimeSlotResult> DeleteTimeSlotAsync(string requestingUserId, Guid timeslotId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new TimeSlotResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var current = await _configRepository.GetTimeSlotAsync(timeslotId);
            if (current is null || current.SchoolId != schoolId)
                return new TimeSlotResult { Succeeded = false, Errors = { "Time slot not found." } };

            if (await _configRepository.TimeSlotHasDependenciesAsync(timeslotId))
                return new TimeSlotResult { Succeeded = false, Errors = { "This time slot is in use by a timetable, activity or override and cannot be deleted." } };

            var deleted = await _configRepository.DeleteTimeSlotAsync(timeslotId);
            return new TimeSlotResult { Succeeded = deleted };
        }

        private async Task<string?> ValidateTimeSlotAsync(Guid schoolId, TimeSlotRequest request, Guid? excludingTimeslotId)
        {
            if (request.EndTime <= request.StartTime)
                return "End time must be after start time.";

            if (!SlotTypes.All.Contains(request.SlotType))
                return $"'{request.SlotType}' is not a valid slot type.";

            if (await _configRepository.TimeSlotOverlapsAsync(schoolId, request.StartTime, request.EndTime, excludingTimeslotId))
                return "This time range overlaps an existing time slot.";

            return null;
        }

        // ---------- Subject scheduling policy ----------

        public async Task<List<SubjectScheduleConfigDto>> ListPolicyAsync(string requestingUserId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            return schoolId is null ? new List<SubjectScheduleConfigDto>() : await _configRepository.ListPolicyAsync(schoolId.Value);
        }

        public async Task<SubjectScheduleConfigResult> UpsertPolicyAsync(string requestingUserId, Guid classId, SubjectScheduleConfigRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new SubjectScheduleConfigResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var current = await _configRepository.GetPolicyAsync(classId);
            if (current is null || current.SchoolId != schoolId)
                return new SubjectScheduleConfigResult { Succeeded = false, Errors = { "Subject not found." } };

            if (request.RequiredDoubles * 2 > request.WeeklyPeriods)
                return new SubjectScheduleConfigResult
                {
                    Succeeded = false,
                    Errors = { $"{request.RequiredDoubles} double period(s) need {request.RequiredDoubles * 2} periods, but only {request.WeeklyPeriods} are allocated per week." },
                };

            await _configRepository.UpsertPolicyAsync(classId, schoolId.Value, request);
            return new SubjectScheduleConfigResult { Succeeded = true };
        }

        /// <summary>
        /// Saves a whole section's policy at once. Every item is validated first — ownership and the
        /// doubles-fit-in-weekly-periods rule — and nothing is written unless all of them pass, so a
        /// bulk save either takes effect completely or not at all.
        /// </summary>
        public async Task<BulkSubjectScheduleConfigResult> UpsertPolicyBulkAsync(string requestingUserId, BulkSubjectScheduleConfigRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new BulkSubjectScheduleConfigResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            if (request.Items.Count == 0)
                return new BulkSubjectScheduleConfigResult { Succeeded = true, SavedCount = 0 };

            var duplicates = request.Items
                .GroupBy(i => i.ClassId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
                return new BulkSubjectScheduleConfigResult { Succeeded = false, Errors = { "The same subject appeared more than once in this save." } };

            var classIds = request.Items.Select(i => i.ClassId).ToList();
            var owned = await _configRepository.FilterClassIdsInSchoolAsync(schoolId.Value, classIds);

            var errors = new List<string>();

            // Names are only needed to write readable errors, so look them up once rather than
            // per item.
            var namesByClassId = (await _configRepository.ListPolicyAsync(schoolId.Value))
                .ToDictionary(p => p.ClassId, p => p.SubjectName ?? "Subject");

            foreach (var item in request.Items)
            {
                if (!owned.Contains(item.ClassId))
                {
                    errors.Add("One or more subjects were not found in this school.");
                    continue;
                }

                var name = namesByClassId.TryGetValue(item.ClassId, out var n) ? n : "Subject";

                if (item.WeeklyPeriods < 0 || item.WeeklyPeriods > 50)
                    errors.Add($"{name}: weekly periods must be between 0 and 50.");

                if (item.RequiredDoubles < 0 || item.RequiredDoubles > 25)
                    errors.Add($"{name}: double periods must be between 0 and 25.");

                if (item.RequiredDoubles * 2 > item.WeeklyPeriods)
                    errors.Add($"{name}: {item.RequiredDoubles} double period(s) need {item.RequiredDoubles * 2} periods, but only {item.WeeklyPeriods} are allocated.");
            }

            if (errors.Count > 0)
                return new BulkSubjectScheduleConfigResult { Succeeded = false, Errors = errors.Distinct().ToList() };

            var saved = await _configRepository.UpsertPolicyBulkAsync(schoolId.Value, request.Items);
            return new BulkSubjectScheduleConfigResult { Succeeded = true, SavedCount = saved };
        }

        // ---------- Adjacency ----------

        public async Task<List<SubjectAdjacencyRuleDto>> ListAdjacencyAsync(string requestingUserId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            return schoolId is null ? new List<SubjectAdjacencyRuleDto>() : await _configRepository.ListAdjacencyAsync(schoolId.Value);
        }

        public async Task<bool> AddAdjacencyRuleAsync(string requestingUserId, Guid classId, Guid cannotFollowClassId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null || classId == cannotFollowClassId) return false;

            var subject = await _configRepository.GetPolicyAsync(classId);
            var cannotFollow = await _configRepository.GetPolicyAsync(cannotFollowClassId);
            if (subject is null || subject.SchoolId != schoolId || cannotFollow is null || cannotFollow.SchoolId != schoolId)
                return false;

            if (await _configRepository.AdjacencyRuleExistsAsync(classId, cannotFollowClassId))
                return false;

            await _configRepository.AddAdjacencyRuleAsync(schoolId.Value, classId, cannotFollowClassId);
            return true;
        }

        public async Task<bool> RemoveAdjacencyRuleAsync(string requestingUserId, Guid subjectAdjacencyRuleId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return false;

            var rule = await _configRepository.GetAdjacencyRuleAsync(subjectAdjacencyRuleId);
            if (rule is null || rule.SchoolId != schoolId) return false;

            return await _configRepository.RemoveAdjacencyRuleAsync(subjectAdjacencyRuleId);
        }

        // ---------- Generation ----------

        public async Task<GenerateScheduleResult> PreviewGenerationAsync(string requestingUserId, GenerateScheduleRequest request)
        {
            var context = await BuildContextAsync(requestingUserId, request.AcademicLevel, request.AcademicLevelSection, request.ActivityIds, request.RunOverrides);
            if (context.Error is not null)
                return new GenerateScheduleResult { Success = false, Errors = { context.Error } };

            if (context.Value!.Subjects.Count == 0)
            {
                return new GenerateScheduleResult
                {
                    Success = false,
                    Errors = { "No subjects with weekly periods are configured for this section. Set the subject policy first." },
                };
            }

            // Exclude this section's own saved rows, otherwise regenerating a section that already
            // has a timetable would treat its own teachers as busy and place almost nothing.
            var busyByTeacher = await _teacherAvailabilityRepository.GetBusySlotsBySchoolAsync(
                context.Value.SchoolId, request.AcademicLevel, request.AcademicLevelSection);

            var teacherChecker = new TeacherConflictChecker();
            foreach (var (teacherId, busySlots) in busyByTeacher)
            {
                teacherChecker.Preload(teacherId, busySlots);
            }

            return _engine.Generate(context.Value, teacherChecker);
        }

        /// <summary>
        /// Re-validates a hand-edited board without regenerating it, so the violations shown to the
        /// admin describe what is currently on screen.
        /// </summary>
        public async Task<GenerateScheduleResult> ValidateEditedScheduleAsync(string requestingUserId, ValidateScheduleRequest request)
        {
            var context = await BuildContextAsync(requestingUserId, request.AcademicLevel, request.AcademicLevelSection, request.ActivityIds, request.RunOverrides);
            if (context.Error is not null)
                return new GenerateScheduleResult { Success = false, Errors = { context.Error } };

            // Other sections' commitments only — this board's own placements are being validated, so
            // including them would make every slot conflict with itself.
            var busyByTeacher = await _teacherAvailabilityRepository.GetBusySlotsBySchoolAsync(
                context.Value!.SchoolId, request.AcademicLevel, request.AcademicLevelSection);

            var externalCommitments = new TeacherConflictChecker();
            foreach (var (teacherId, busySlots) in busyByTeacher)
            {
                externalCommitments.Preload(teacherId, busySlots);
            }

            return _engine.Validate(context.Value, request.Slots, externalCommitments);
        }

        private async Task<(SectionScheduleContext? Value, string? Error)> BuildContextAsync(
            string requestingUserId,
            int academicLevel,
            Guid academicLevelSection,
            List<Guid> activityIds,
            List<SubjectScheduleConfigOverride> runOverrides)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return (null, "Could not determine the requesting admin's school.");

            var timeSlots = await _configRepository.ListTimeSlotsAsync(schoolId.Value);
            if (timeSlots.Count == 0)
                return (null, "No time slots are configured for this school.");

            var allSubjects = await _configRepository.ListPolicyAsync(schoolId.Value);
            var adjacency = await _configRepository.ListAdjacencyAsync(schoolId.Value);

            ApplyRunOverrides(allSubjects, runOverrides);

            // Only this section's subjects, and only those actually allocated periods — a subject
            // left at zero weekly periods is unconfigured, not a request for zero.
            //
            // Scoped by the numeric AcademicLevel rather than LevelName: that integer is what
            // ClassSchedule stores, so matching on it cannot drift from the saved schedule the way
            // a display name can.
            //
            // Group-specific classes are excluded. Only "All" classes form the base timetable; an
            // option-set class (one group takes French while another takes Chichewa in the same
            // period) reaches students through a TimetableOverride. Scheduling one as a base subject
            // would put it in front of the whole section.
            var sectionCode = await ResolveSectionCodeAsync(schoolId.Value, academicLevelSection);

            var subjects = allSubjects
                .Where(s => s.WeeklyPeriods > 0)
                .Where(s => s.AcademicLevel == academicLevel)
                .Where(s => sectionCode is null || string.Equals(s.GradeSection, sectionCode, StringComparison.OrdinalIgnoreCase))
                .Where(s => IsBaseTimetableClass(s.GroupName))
                .ToList();

            var subjectIds = subjects.Select(s => s.ClassId).ToHashSet();
            adjacency = adjacency
                .Where(a => subjectIds.Contains(a.ClassId) && subjectIds.Contains(a.CannotFollowClassId))
                .ToList();

            var activities = new List<TimeTableActivityDto>();
            if (activityIds.Count > 0)
            {
                var loadedActivities = new List<ActivityDto>();
                foreach (var activityId in activityIds.Distinct())
                {
                    var activity = await _configRepository.GetActivityAsync(activityId);
                    if (activity is null || activity.SchoolId != schoolId)
                        return (null, "One or more selected activities were not found.");

                    loadedActivities.Add(activity);
                }

                var validationError = ValidateActivitySelection(loadedActivities, timeSlots);
                if (validationError is not null)
                    return (null, validationError);

                var days = loadedActivities.Any(a => a.PreferredDayId is not null)
                    ? await _configRepository.ListDaysAsync()
                    : new List<DayOptionDto>();

                activities = loadedActivities.Select(a => new TimeTableActivityDto
                {
                    ActivityId = a.ActivityId,
                    ActivityName = a.ActivityName,
                    MustBeMorning = a.MustBeMorning,
                    MustBeAfternoon = a.MustBeAfternoon,
                    PreferredTimeSlotId = a.PreferredTimeSlotId,
                    PreferredDay = a.PreferredDayId is null
                        ? null
                        : ParseDayOfWeek(days.FirstOrDefault(d => d.DayId == a.PreferredDayId.Value)?.DayName),
                }).ToList();
            }

            return (new SectionScheduleContext
            {
                SchoolId = schoolId.Value,
                AcademicLevel = academicLevel,
                AcademicLevelSection = academicLevelSection,
                TimeSlots = timeSlots,
                Subjects = subjects,
                AdjacencyRules = adjacency,
                Activities = activities,
            }, null);
        }

        public async Task<SaveGeneratedScheduleResult> SaveScheduleAsync(string requestingUserId, SaveGeneratedScheduleRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new SaveGeneratedScheduleResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            if (request.EndDate < request.StartDate)
                return new SaveGeneratedScheduleResult { Succeeded = false, Errors = { "End date must be on or after the start date." } };

            return await _generationRepository.SaveGeneratedScheduleAsync(schoolId.Value, request);
        }

        public async Task<List<ScheduledClassItem>> GetCurrentScheduleAsync(string requestingUserId, int academicLevel, Guid academicLevelSection)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            return schoolId is null
                ? new List<ScheduledClassItem>()
                : await _generationRepository.GetCurrentScheduleAsync(schoolId.Value, academicLevel, academicLevelSection);
        }

        // ---------- Overrides ----------

        public async Task<List<OverrideListItem>> ListOverridesAsync(string requestingUserId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            return schoolId is null ? new List<OverrideListItem>() : await _generationRepository.ListOverridesAsync(schoolId.Value);
        }

        public async Task<OverrideDetail?> GetOverrideAsync(string requestingUserId, Guid overrideId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return null;

            var currentSchoolId = await _generationRepository.GetOverrideSchoolIdAsync(overrideId);
            if (currentSchoolId is null || currentSchoolId != schoolId) return null;

            return await _generationRepository.GetOverrideAsync(overrideId);
        }

        public async Task<OverrideResult> CreateOverrideAsync(string requestingUserId, OverrideRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new OverrideResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var error = await ValidateOverrideAsync(schoolId.Value, request, excludingOverrideId: null);
            if (error is not null)
                return new OverrideResult { Succeeded = false, Errors = { error } };

            var requestingUserGuid = Guid.TryParse(requestingUserId, out var parsed) ? (Guid?)parsed : null;
            var overrideId = await _generationRepository.CreateOverrideAsync(requestingUserGuid, request);
            return new OverrideResult { Succeeded = true, OverrideId = overrideId };
        }

        public async Task<OverrideResult> UpdateOverrideAsync(string requestingUserId, Guid overrideId, OverrideRequest request)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null)
                return new OverrideResult { Succeeded = false, Errors = { "Could not determine the requesting admin's school." } };

            var currentSchoolId = await _generationRepository.GetOverrideSchoolIdAsync(overrideId);
            if (currentSchoolId is null || currentSchoolId != schoolId)
                return new OverrideResult { Succeeded = false, Errors = { "Override not found." } };

            var error = await ValidateOverrideAsync(schoolId.Value, request, overrideId);
            if (error is not null)
                return new OverrideResult { Succeeded = false, Errors = { error } };

            var updated = await _generationRepository.UpdateOverrideAsync(overrideId, request);
            return new OverrideResult { Succeeded = updated, OverrideId = overrideId };
        }

        public async Task<bool> DeleteOverrideAsync(string requestingUserId, Guid overrideId)
        {
            var schoolId = await ResolveSchoolIdAsync(requestingUserId);
            if (schoolId is null) return false;

            var currentSchoolId = await _generationRepository.GetOverrideSchoolIdAsync(overrideId);
            if (currentSchoolId is null || currentSchoolId != schoolId) return false;

            return await _generationRepository.DeleteOverrideAsync(overrideId);
        }

        private async Task<string?> ValidateOverrideAsync(Guid schoolId, OverrideRequest request, Guid? excludingOverrideId)
        {
            if (!await _generationRepository.AcademicLevelBelongsToSchoolAsync(request.AcademicLevelId, schoolId))
                return "Academic level not found.";

            if (request.EffectiveFrom is not null && request.EffectiveTo is not null && request.EffectiveTo < request.EffectiveFrom)
                return "The 'effective to' date must be on or after the 'effective from' date.";

            if (await _generationRepository.OverrideOverlapsAsync(
                    request.AcademicLevelId, request.LevelSectionId, request.StudentGroup, request.DayOfTheWeekId, request.TimeSlotId,
                    request.EffectiveFrom, request.EffectiveTo, excludingOverrideId))
                return "An override already exists for this group, day, and time slot in the given date range.";

            return null;
        }

        /// <summary>Each blanket (no PreferredTimeSlotId) activity locks a time window (see
        /// TimetableWeek.LockActivities): afternoon-only -> [afternoonStart, end of day),
        /// morning-only -> [start of day, afternoonStart), unrestricted -> the whole day. A specific-slot
        /// activity (PreferredTimeSlotId set) locks exactly one slot, on PreferredDay or every weekday.
        /// Combining activities in one run is only safe when nothing they lock overlaps.</summary>
        private static string? ValidateActivitySelection(List<ActivityDto> activities, List<TimeSlotDto> timeSlots)
        {
            if (activities.Count <= 1) return null;

            var blanket = activities.Where(a => a.PreferredTimeSlotId is null).ToList();
            var specific = activities.Where(a => a.PreferredTimeSlotId is not null).ToList();

            var unrestrictedCount = blanket.Count(a => !a.MustBeMorning && !a.MustBeAfternoon);
            if (unrestrictedCount > 0)
                return "An activity with no time restriction locks the entire day and cannot be combined with other activities in the same run.";

            if (blanket.Count(a => a.MustBeMorning && !a.MustBeAfternoon) > 1)
                return "Only one morning-only activity can be locked per run.";

            if (blanket.Count(a => a.MustBeAfternoon && !a.MustBeMorning) > 1)
                return "Only one afternoon-only activity can be locked per run.";

            for (var i = 0; i < specific.Count; i++)
            {
                for (var j = i + 1; j < specific.Count; j++)
                {
                    var a = specific[i];
                    var b = specific[j];
                    var sameDay = a.PreferredDayId is null || b.PreferredDayId is null || a.PreferredDayId == b.PreferredDayId;

                    if (a.PreferredTimeSlotId == b.PreferredTimeSlotId && sameDay)
                        return $"'{a.ActivityName}' and '{b.ActivityName}' both target the same time slot on overlapping days.";
                }
            }

            if (blanket.Count > 0 && specific.Count > 0)
            {
                var afternoonStart = timeSlots
                    .Where(s => s.SlotType == SlotTypes.Afternoon)
                    .OrderBy(s => s.StartTime)
                    .FirstOrDefault()?.StartTime?.ToTimeSpan() ?? TimeSpan.Zero;

                foreach (var b in blanket)
                {
                    var startFrom = b.MustBeAfternoon ? afternoonStart : TimeSpan.Zero;
                    var endBefore = b.MustBeMorning && !b.MustBeAfternoon ? afternoonStart : (TimeSpan?)null;

                    foreach (var s in specific)
                    {
                        var slotTime = timeSlots.FirstOrDefault(t => t.TimeslotId == s.PreferredTimeSlotId)?.StartTime?.ToTimeSpan();
                        if (slotTime is null) continue;

                        if (slotTime >= startFrom && (endBefore is null || slotTime < endBefore))
                            return $"'{s.ActivityName}' falls inside the time window already locked by '{b.ActivityName}'.";
                    }
                }
            }

            return null;
        }

        private static DayOfWeek? ParseDayOfWeek(string? dayName) =>
            !string.IsNullOrEmpty(dayName) && Enum.TryParse<DayOfWeek>(dayName, true, out var day) ? day : null;

        private static void ApplyRunOverrides(List<SubjectScheduleConfigDto> subjects, List<SubjectScheduleConfigOverride> overrides)
        {
            if (overrides.Count == 0) return;

            var byClassId = subjects.ToDictionary(s => s.ClassId);
            foreach (var ov in overrides)
            {
                if (!byClassId.TryGetValue(ov.ClassId, out var subject)) continue;

                if (ov.IsCore.HasValue) subject.IsCore = ov.IsCore.Value;
                if (ov.WeeklyPeriods.HasValue) subject.WeeklyPeriods = ov.WeeklyPeriods.Value;
                if (ov.RequiredDoubles.HasValue) subject.RequiredDoubles = ov.RequiredDoubles.Value;
                if (ov.TimePreference.HasValue) subject.TimePreference = ov.TimePreference.Value;
            }
        }

        /// <summary>"All" (or unset) means the class is taught to the whole section and belongs in
        /// the base timetable. Anything else is an option-set class, applied via overrides.</summary>
        private static bool IsBaseTimetableClass(string? groupName) =>
            string.IsNullOrWhiteSpace(groupName) || string.Equals(groupName, "All", StringComparison.OrdinalIgnoreCase);

        private async Task<string?> ResolveSectionCodeAsync(Guid schoolId, Guid academicLevelSection)
        {
            var sections = await _repository.GetLevelSectionsBySchoolAsync(schoolId);
            return sections?.FirstOrDefault(s => s.LevelSectionID == academicLevelSection)?.SectionCode;
        }

        private async Task<Guid?> ResolveSchoolIdAsync(string requestingUserId)
        {
            if (string.IsNullOrWhiteSpace(requestingUserId)) return null;

            var initData = await _repository.GetInitializationDataResults(requestingUserId);
            return initData?.FirstOrDefault()?.SchoolID;
        }
    }
}
