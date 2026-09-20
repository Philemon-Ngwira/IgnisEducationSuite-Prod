using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Greedy initial placement over an already-built, activity-locked board: doubles before
    /// singles, time-restricted subjects before unrestricted ones, checking teacher conflicts at
    /// every placement.
    ///
    /// Placement bookkeeping goes through TimetableState rather than a private day-count map, so
    /// the counts the later repair/optimize passes read are the same ones written here.
    /// </summary>
    public class TimetableGenerator
    {
        public void Generate(TimetableState state, TeacherConflictChecker teacherChecker)
        {
            var slotsByDay = state.Slots
                .GroupBy(s => s.Day)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

            // Subjects with a time restriction go first: they have strictly fewer legal slots, so
            // placing them after the unrestricted ones would leave them nothing to take.
            var restricted = state.Subjects.Values
                .Where(s => s.TimePreference != SubjectTimePreference.Any)
                .ToList();
            var unrestricted = state.Subjects.Values
                .Where(s => s.TimePreference == SubjectTimePreference.Any)
                .ToList();

            PlaceDoubles(state, slotsByDay, restricted, teacherChecker);
            PlaceSingles(state, slotsByDay, restricted, teacherChecker);
            PlaceDoubles(state, slotsByDay, unrestricted, teacherChecker);
            PlaceSingles(state, slotsByDay, unrestricted, teacherChecker);
        }

        private static void PlaceDoubles(
            TimetableState state,
            Dictionary<DayOfWeek, List<GenerationSlot>> slotsByDay,
            List<SubjectScheduleConfigDto> subjects,
            TeacherConflictChecker teacherChecker)
        {
            foreach (var subject in subjects)
            {
                while (state.RequiredDoublesRemaining(subject.ClassId) > 0 &&
                       state.WeeklyRemaining(subject.ClassId) >= 2)
                {
                    var placed = false;

                    foreach (var day in slotsByDay.Keys)
                    {
                        if (state.DailyCount(day, subject.ClassId) != 0) continue;

                        var daySlots = slotsByDay[day];

                        for (var i = 0; i < daySlots.Count - 1; i++)
                        {
                            var a = daySlots[i];
                            var b = daySlots[i + 1];

                            if (!a.IsFree || !b.IsFree) continue;
                            if (a.EndTime != b.StartTime) continue;
                            if (!state.SatisfiesTimePreference(subject, a)) continue;
                            if (!state.SatisfiesTimePreference(subject, b)) continue;

                            var teacherId = subject.TeacherId ?? Guid.Empty;
                            if (teacherChecker.IsTeacherBusy(teacherId, day, a.StartTime!.Value)) continue;
                            if (teacherChecker.IsTeacherBusy(teacherId, day, b.StartTime!.Value)) continue;

                            if (state.WeeklyRemaining(subject.ClassId) < 2) break;

                            Place(state, a, subject, teacherChecker);
                            Place(state, b, subject, teacherChecker);
                            a.IsDoublePeriod = true;
                            b.IsDoublePeriod = true;

                            placed = true;
                            break;
                        }

                        if (placed) break;
                    }

                    if (!placed) break;
                }
            }
        }

        private static void PlaceSingles(
            TimetableState state,
            Dictionary<DayOfWeek, List<GenerationSlot>> slotsByDay,
            List<SubjectScheduleConfigDto> subjects,
            TeacherConflictChecker teacherChecker)
        {
            foreach (var subject in subjects)
            {
                foreach (var day in slotsByDay.Keys)
                {
                    if (state.WeeklyRemaining(subject.ClassId) <= 0) break;
                    if (state.DailyCount(day, subject.ClassId) >= 2) continue;

                    foreach (var slot in slotsByDay[day])
                    {
                        if (state.WeeklyRemaining(subject.ClassId) <= 0) break;
                        if (!slot.IsFree) continue;
                        if (!state.SatisfiesTimePreference(subject, slot)) continue;
                        if (teacherChecker.IsTeacherBusy(subject.TeacherId ?? Guid.Empty, day, slot.StartTime!.Value)) continue;

                        Place(state, slot, subject, teacherChecker);
                        break;
                    }
                }
            }
        }

        private static void Place(
            TimetableState state,
            GenerationSlot slot,
            SubjectScheduleConfigDto subject,
            TeacherConflictChecker teacherChecker)
        {
            if (state.WeeklyRemaining(subject.ClassId) <= 0) return;

            var teacherId = subject.TeacherId ?? Guid.Empty;
            if (teacherChecker.IsTeacherBusy(teacherId, slot.Day, slot.StartTime!.Value)) return;

            state.PlaceSubject(slot, subject.ClassId);
            teacherChecker.MarkBusy(teacherId, slot.Day, slot.StartTime.Value);
        }
    }
}
