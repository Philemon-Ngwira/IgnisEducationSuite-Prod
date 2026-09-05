namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>Heuristic quality score for a generated timetable — higher is better. Penalizes empty
    /// slots and "holes" (a free slot sandwiched between two occupied ones), rewards spreading a
    /// subject's periods across more distinct days and placing core subjects earlier in the day.
    ///
    /// The morning/afternoon boundary comes from the school's own slot definitions via
    /// TimetableState, not a hardcoded clock time, so the core-subject preference means the same
    /// thing here as it does in every other pass.</summary>
    public static class TimetableScorer
    {
        public static int Score(TimetableState state)
        {
            var score = 0;

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

                var daySlots = state.SlotsForDay(day).ToList();
                for (var i = 0; i < daySlots.Count; i++)
                {
                    if (!daySlots[i].IsFree) continue;

                    score -= 10;

                    var hasPrev = i > 0 && !daySlots[i - 1].IsFree;
                    var hasNext = i < daySlots.Count - 1 && !daySlots[i + 1].IsFree;
                    if (hasPrev && hasNext) score -= 10; // a "hole" is worse than a trailing gap
                }
            }

            foreach (var subject in state.Subjects.Values)
            {
                var daysUsed = Enum.GetValues(typeof(DayOfWeek))
                    .Cast<DayOfWeek>()
                    .Count(d => state.DailyCount(d, subject.ClassId) > 0);
                score += daysUsed * 5;

                if (!subject.IsCore) continue;

                foreach (var slot in state.Slots.Where(s => s.SubjectId == subject.ClassId))
                {
                    if (slot.StartTime is not { } start) continue;

                    if (start < state.AfternoonStart) score += 5;
                    else score -= 5;
                }
            }

            return score;
        }
    }
}
