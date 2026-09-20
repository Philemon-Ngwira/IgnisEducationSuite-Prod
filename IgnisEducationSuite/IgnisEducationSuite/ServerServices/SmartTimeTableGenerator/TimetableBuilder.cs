namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Cheap greedy fill for genuinely-empty slots left after repair — picks whichever subject
    /// still needs periods, isn't already scheduled that day, and doesn't conflict. Distinct from
    /// (and runs before) TimetableOptimizer's proper hill-climbing swap pass.
    /// </summary>
    public class TimetableBuilder
    {
        private readonly TeacherConflictChecker _teacherChecker;

        public TimetableBuilder(TeacherConflictChecker teacherChecker)
        {
            _teacherChecker = teacherChecker;
        }

        public void Fill(TimetableState state)
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

                var daySlots = state.SlotsForDay(day).ToList();
                var subjectsToday = new HashSet<Guid>(
                    daySlots.Where(s => s.IsAcademic && !s.IsActivity).Select(s => s.SubjectId));

                foreach (var slot in daySlots)
                {
                    if (slot.SubjectId != Guid.Empty) continue;
                    if (slot.IsActivity || slot.IsLocked) continue;

                    var candidates = state.Subjects.Values
                        .Where(s =>
                            state.WeeklyRemaining(s.ClassId) > 0 &&
                            !subjectsToday.Contains(s.ClassId) &&
                            state.DailyCount(day, s.ClassId) == 0 &&
                            state.SatisfiesTimePreference(s, slot) &&
                            !_teacherChecker.IsTeacherBusy(s.TeacherId ?? Guid.Empty, day, slot.StartTime!.Value))
                        .OrderByDescending(s => state.WeeklyRemaining(s.ClassId))
                        .ThenBy(s => s.SubjectName)
                        .ToList();

                    if (candidates.Count == 0) continue;

                    // Don't create an adjacency violation while filling.
                    var prev = daySlots.FirstOrDefault(s => s.EndTime == slot.StartTime);
                    var next = daySlots.FirstOrDefault(s => s.StartTime == slot.EndTime);

                    var chosen = candidates.FirstOrDefault(c =>
                    {
                        var probe = new GenerationSlot { SubjectId = c.ClassId };
                        if (prev is not null && state.ViolatesAdjacency(prev, probe)) return false;
                        if (next is not null && state.ViolatesAdjacency(probe, next)) return false;
                        return true;
                    });

                    if (chosen is null) continue;

                    state.PlaceSubject(slot, chosen.ClassId);
                    _teacherChecker.MarkBusy(chosen.TeacherId ?? Guid.Empty, day, slot.StartTime!.Value);
                    subjectsToday.Add(chosen.ClassId);
                }
            }
        }
    }
}
