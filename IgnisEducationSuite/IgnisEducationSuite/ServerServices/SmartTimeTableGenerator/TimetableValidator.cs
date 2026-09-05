using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableReport
    {
        public List<string> InvariantViolations { get; set; } = new();
        public Dictionary<string, int> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Checks daily subject counts, required doubles, weekly periods, time-preference compliance,
    /// teacher double-booking, and adjacency violations.
    ///
    /// Purely observational — callers decide what to do with a non-empty InvariantViolations list.
    /// Messages name the subject and day in plain text so the editor can highlight the offending
    /// cells and an admin can read them without cross-referencing IDs.
    /// </summary>
    public class TimetableValidator
    {
        /// <param name="externalCommitments">
        /// Optional. A checker preloaded with teacher commitments from OTHER sections and never
        /// marked during this run, used to catch a hand-edit that puts a teacher in two sections at
        /// once. Do not pass the generation-time checker: that one accumulates this board's own
        /// placements, so every slot would report a conflict with itself.
        /// </param>
        public TimetableReport Analyze(TimetableState state, TeacherConflictChecker? externalCommitments = null)
        {
            var report = new TimetableReport();

            foreach (var subject in state.Subjects.Values)
            {
                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    var count = state.DailyCount(day, subject.ClassId);
                    if (count > 2)
                        report.InvariantViolations.Add($"{subject.SubjectName}: {count} periods on {day} exceeds the daily max of 2.");
                }

                var doublesRemaining = state.RequiredDoublesRemaining(subject.ClassId);
                if (doublesRemaining > 0)
                    report.InvariantViolations.Add($"{subject.SubjectName}: {doublesRemaining} required double period(s) could not be placed.");

                var weeklyRemaining = state.WeeklyRemaining(subject.ClassId);
                if (weeklyRemaining > 0)
                    report.InvariantViolations.Add($"{subject.SubjectName}: {weeklyRemaining} weekly period(s) could not be placed.");

                if (subject.TimePreference != SubjectTimePreference.Any)
                {
                    var offending = state.Slots
                        .Where(s => s.SubjectId == subject.ClassId && !state.SatisfiesTimePreference(subject, s))
                        .ToList();

                    if (offending.Count > 0)
                    {
                        report.InvariantViolations.Add(
                            $"{subject.SubjectName}: {offending.Count} period(s) placed outside its {Describe(subject.TimePreference)} preference " +
                            $"({string.Join(", ", offending.Select(s => $"{s.Day} {s.StartTime:hh\\:mm}"))}).");
                    }
                }
            }

            // A subject configured EarlyMorningOnly in a school that defines no EarlyMorning slots
            // can never be placed. Say so explicitly rather than reporting N unplaceable periods
            // with no cause.
            if (state.EarlyMorningCutoff == TimeSpan.Zero)
            {
                var stranded = state.Subjects.Values
                    .Where(s => s.TimePreference == SubjectTimePreference.EarlyMorningOnly)
                    .Select(s => s.SubjectName)
                    .ToList();

                if (stranded.Count > 0)
                    report.InvariantViolations.Add(
                        $"No time slots are marked '{SlotTypes.EarlyMorning}', so these early-morning-only subjects cannot be placed: {string.Join(", ", stranded)}.");
            }

            if (state.AfternoonStart == TimeSpan.MaxValue)
            {
                var stranded = state.Subjects.Values
                    .Where(s => s.TimePreference == SubjectTimePreference.AfternoonOnly)
                    .Select(s => s.SubjectName)
                    .ToList();

                if (stranded.Count > 0)
                    report.InvariantViolations.Add(
                        $"No time slots are marked '{SlotTypes.Afternoon}', so these afternoon-only subjects cannot be placed: {string.Join(", ", stranded)}.");
            }

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var daySlots = state.SlotsForDay(day).ToList();
                for (var i = 0; i < daySlots.Count - 1; i++)
                {
                    var prev = daySlots[i];
                    var next = daySlots[i + 1];
                    if (prev.EndTime == next.StartTime && state.ViolatesAdjacency(prev, next))
                        report.InvariantViolations.Add($"Adjacency rule violated: {next.SubjectName} immediately follows {prev.SubjectName} on {day}.");
                }
            }

            // Two subjects sharing a teacher in the same period of this section's own board. Cross-
            // section clashes are prevented by TeacherConflictChecker during placement; this catches
            // the within-board case, which manual edits can reintroduce.
            foreach (var group in state.Slots
                         .Where(s => s.IsAcademic)
                         .Select(s => new { Slot = s, Teacher = TeacherOf(state, s) })
                         .Where(x => x.Teacher != Guid.Empty)
                         .GroupBy(x => (x.Slot.Day, x.Slot.StartTime, x.Teacher))
                         .Where(g => g.Count() > 1))
            {
                var names = string.Join(" and ", group.Select(x => x.Slot.SubjectName).Distinct());
                report.InvariantViolations.Add(
                    $"Teacher double-booked on {group.Key.Day} at {group.Key.StartTime:hh\\:mm}: {names}.");
            }

            // A hand-edit can put a teacher in this section at a time they already teach another.
            // Generation prevents this via TeacherConflictChecker; editing has no such guard, so it
            // is caught here instead.
            if (externalCommitments is not null)
            {
                foreach (var slot in state.Slots.Where(s => s.IsAcademic))
                {
                    if (slot.StartTime is not { } start) continue;

                    var teacherId = TeacherOf(state, slot);
                    if (teacherId == Guid.Empty) continue;

                    if (externalCommitments.IsTeacherBusy(teacherId, slot.Day, start))
                    {
                        var teacherName = state.Subjects.TryGetValue(slot.SubjectId, out var subject)
                            ? subject.TeacherName ?? "The teacher"
                            : "The teacher";

                        report.InvariantViolations.Add(
                            $"{teacherName} is already teaching another section on {slot.Day} at {start:hh\\:mm} ({slot.SubjectName}).");
                    }
                }
            }

            report.Metrics["TotalSlots"] = state.Slots.Count;
            report.Metrics["FreeSlots"] = state.Slots.Count(s => s.IsFree);
            report.Metrics["ScheduledSlots"] = state.Slots.Count(s => s.IsAcademic);
            report.Metrics["ActivitySlots"] = state.Slots.Count(s => s.IsActivity);

            return report;
        }

        private static Guid TeacherOf(TimetableState state, GenerationSlot slot) =>
            state.Subjects.TryGetValue(slot.SubjectId, out var subject) ? subject.TeacherId ?? Guid.Empty : Guid.Empty;

        private static string Describe(SubjectTimePreference preference) => preference switch
        {
            SubjectTimePreference.EarlyMorningOnly => "early-morning-only",
            SubjectTimePreference.MorningOnly => "morning-only",
            SubjectTimePreference.AfternoonOnly => "afternoon-only",
            _ => "time",
        };
    }
}
