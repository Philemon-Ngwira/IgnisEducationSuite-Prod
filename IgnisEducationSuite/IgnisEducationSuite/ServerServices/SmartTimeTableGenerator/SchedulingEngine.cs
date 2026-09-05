using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Public facade for the scheduling algorithm — pure and DB-free (no DbContext anywhere in
    /// this namespace). Takes one section's full config bundle plus a pre-loaded teacher-conflict
    /// checker (built by the caller, which has DB access) and runs: greedy generation → hard-
    /// constraint repair → cheap fill → hill-climbing optimization → validation.
    ///
    /// Deliberately scoped to one section per call, with the conflict checker passed in rather than
    /// built internally, so a future whole-school batch feature can construct one checker and
    /// thread it through N sequential calls without any change to this API.
    /// </summary>
    public class SchedulingEngine
    {
        public GenerateScheduleResult Generate(SectionScheduleContext context, TeacherConflictChecker teacherChecker)
        {
            var slots = TimetableWeek.BuildEmpty(context.TimeSlots);

            if (slots.Count == 0)
            {
                return new GenerateScheduleResult
                {
                    Success = false,
                    Errors = { $"No teaching time slots are configured. At least one slot must be typed '{SlotTypes.EarlyMorning}', '{SlotTypes.Morning}' or '{SlotTypes.Afternoon}'." },
                };
            }

            TimetableWeek.LockActivities(slots, context.Activities, TimetableWeek.AfternoonStartOf(context.TimeSlots));

            var state = new TimetableState(slots, context.Subjects, context.AdjacencyRules);

            new TimetableGenerator().Generate(state, teacherChecker);
            new TimeTableRepair(teacherChecker).Repair(state);
            new TimetableBuilder(teacherChecker).Fill(state);
            new TimetableOptimizer(teacherChecker).Optimize(state);

            var report = new TimetableValidator().Analyze(state);

            return new GenerateScheduleResult
            {
                Success = true,
                Slots = state.Slots.Select(MapToDto).ToList(),
                Violations = report.InvariantViolations,
                Metrics = report.Metrics,
            };
        }

        /// <summary>
        /// Re-validates a board the admin has edited by hand, without regenerating it. The editor
        /// calls this after every change so violations reflect what is actually on screen — V2
        /// left its generation-time violation list on display while the board changed underneath it.
        /// </summary>
        public GenerateScheduleResult Validate(
            SectionScheduleContext context,
            List<GeneratedSlotDto> editedSlots,
            TeacherConflictChecker? externalCommitments = null)
        {
            var slots = TimetableWeek.BuildEmpty(context.TimeSlots);

            // These slots come from a hand-edited board, so two entries can legitimately claim the
            // same (day, period). Group rather than ToDictionary — the latter throws — and report
            // the collision as a violation instead of failing the whole validation call.
            var bySlot = editedSlots
                .GroupBy(s => (s.Day, s.TimeSlotId))
                .ToDictionary(g => g.Key, g => g.ToList());

            var duplicateViolations = new List<string>();

            foreach (var group in bySlot.Where(g => g.Value.Count > 1))
            {
                var names = group.Value
                    .Select(s => s.ActivityName ?? s.ClassName ?? "(empty)")
                    .Distinct();

                duplicateViolations.Add(
                    $"Two entries occupy {group.Key.Day} at the same period: {string.Join(" and ", names)}.");
            }

            foreach (var slot in slots)
            {
                if (!bySlot.TryGetValue((slot.Day, slot.TimeSlotId), out var entries)) continue;

                var edited = entries[0];

                slot.IsDoublePeriod = edited.IsDoublePeriod;
                slot.IsFiller = edited.IsFiller;

                if (edited.ScheduledActivityId is not null)
                {
                    slot.ScheduledActivityId = edited.ScheduledActivityId;
                    slot.ActivityName = edited.ActivityName;
                    slot.SubjectName = edited.ActivityName;
                    slot.IsLocked = true;
                }
                else if (edited.ClassId is { } classId && classId != Guid.Empty)
                {
                    slot.SubjectId = classId;
                    slot.SubjectName = edited.ClassName;
                }
            }

            var state = new TimetableState(slots, context.Subjects, context.AdjacencyRules);
            var report = new TimetableValidator().Analyze(state, externalCommitments);

            return new GenerateScheduleResult
            {
                Success = true,
                Slots = state.Slots.Select(MapToDto).ToList(),
                Violations = duplicateViolations.Concat(report.InvariantViolations).ToList(),
                Metrics = report.Metrics,
            };
        }

        private static GeneratedSlotDto MapToDto(GenerationSlot slot) => new()
        {
            Day = slot.Day,
            TimeSlotId = slot.TimeSlotId,
            ClassId = slot.SubjectId == Guid.Empty ? null : slot.SubjectId,
            ClassName = slot.SubjectId == Guid.Empty ? null : slot.SubjectName,
            IsDoublePeriod = slot.IsDoublePeriod,
            IsFiller = slot.IsFiller,
            ScheduledActivityId = slot.ScheduledActivityId,
            ActivityName = slot.ActivityName,
        };
    }
}
