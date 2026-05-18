using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimetableBuilder
    {
        private readonly HashSet<Guid> _coreSubjects;
        private readonly TeacherConflictChecker _teacherChecker;

        public TimetableBuilder(HashSet<Guid> coreSubjects, TeacherConflictChecker teacherChecker)
        {
            _coreSubjects = coreSubjects;
            _teacherChecker = teacherChecker;
        }

        public void Build(TimetableState state, TimeTableActivity? prepActivity = null)
        {
            TimetableDebugPrinter.Print("BEFORE REPAIR", state);

            // -------- STAGE 2: REPAIR (HARD CONSTRAINTS) --------
            var repair = new TimeTableRepair(_teacherChecker);
            repair.Repair(state, prepActivity);

            TimetableDebugPrinter.Print("AFTER REPAIR", state);

            // -------- STAGE 3: OPTIMIZATION (SOFT CONSTRAINTS) --------
            Optimize(state);
        }

        private void Optimize(TimetableState state)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                var daySlots = state.SlotsForDay(day)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                var subjectsToday = new HashSet<Guid>(
                    daySlots
                        .Where(s => s.SubjectId != Guid.Empty && !s.IsActivity())
                        .Select(s => s.SubjectId)
                );

                foreach (var slot in daySlots)
                {
                    if (slot.SubjectId != Guid.Empty) continue;
                    if (slot.IsActivity() || slot.IsLocked) continue;

                    var candidates = state.Subjects.Values
                        .Where(s =>
                            state.WeeklyRemaining(s.SubjectId) > 0 &&
                            !subjectsToday.Contains(s.SubjectId) &&
                            state.DailyCount(day, s.SubjectId) == 0 &&
                            (!s.EarlyMorningOnly || slot.StartTime < s.EarlyMorningEnd) &&
                            // ✅ Teacher conflict check in optimizer
                            !_teacherChecker.IsTeacherBusy(s.TeacherId, day, slot.StartTime.Value))
                        .OrderByDescending(s => state.WeeklyRemaining(s.SubjectId))
                        .ThenBy(s => s.SubjectName)
                        .ToList();

                    if (!candidates.Any()) continue;

                    var chosen = candidates.First();
                    state.PlaceSubject(slot, chosen.SubjectId);

                    // ✅ Mark teacher busy after optimizer placement
                    _teacherChecker.MarkBusy(chosen.TeacherId, day, slot.StartTime.Value);

                    subjectsToday.Add(chosen.SubjectId);
                }
            }
        }
    }
}