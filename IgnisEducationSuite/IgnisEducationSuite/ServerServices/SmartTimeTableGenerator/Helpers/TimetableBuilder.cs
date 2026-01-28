using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimetableBuilder
    {
        private readonly HashSet<Guid> _coreSubjects;

        public TimetableBuilder(HashSet<Guid> coreSubjects)
        {
            _coreSubjects = coreSubjects;
        }

        /// <summary>
        /// Runs a full deterministic pipeline: repair hard constraints, then optimize soft preferences.
        /// </summary>
        public void Build(TimetableState state, TimeTableActivity? prepActivity = null)
        {
            TimetableDebugPrinter.Print("BEFORE REPAIR", state);
            // -------- STAGE 2: REPAIR (HARD CONSTRAINTS) --------
            var repair = new TimeTableRepair();
            repair.Repair(state, prepActivity);

            TimetableDebugPrinter.Print("AFTER REPAIR", state);
            // -------- STAGE 3: OPTIMIZATION (SOFT CONSTRAINTS) --------
            Optimize(state);

        }

        /// <summary>
        /// Stage 3 soft-constraint optimization.
        /// Swaps slots deterministically if the score improves.
        /// Only swaps safe slots (not required doubles, not early-morning-only violations, not adjacency violations).
        /// </summary>
        private void Optimize(TimetableState state)
        {
            // 🔒 SAFE OPTIMIZER
            // Only moves subjects into free slots on the SAME DAY
            // Never swaps subject <-> subject
            // Never touches activities
            // Never creates same-day duplicates
            // Never splits doubles

            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                var daySlots = state.SlotsForDay(day)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                // Track what already exists today
                var subjectsToday = new HashSet<Guid>(
                    daySlots
                        .Where(s => s.SubjectId != Guid.Empty && !s.IsActivity())
                        .Select(s => s.SubjectId)
                );

                foreach (var slot in daySlots)
                {
                    // Only consider FREE, NON-ACTIVITY slots
                    if (slot.SubjectId != Guid.Empty)
                        continue;

                    if (slot.IsActivity() || slot.IsLocked)
                        continue;

                    var candidates = state.Subjects.Values
                        .Where(s =>
                            state.WeeklyRemaining(s.SubjectId) > 0 &&
                            !subjectsToday.Contains(s.SubjectId) &&             // ❗ no same-day repeat
                            state.DailyCount(day, s.SubjectId) == 0 &&           // ❗ single only
                            (!s.EarlyMorningOnly || slot.StartTime < s.EarlyMorningEnd))
                        .OrderByDescending(s => state.WeeklyRemaining(s.SubjectId))
                        .ThenBy(s => s.SubjectName)
                        .ToList();

                    if (!candidates.Any())
                        continue;

                    var chosen = candidates.First();

                    state.PlaceSubject(slot, chosen.SubjectId);
                    subjectsToday.Add(chosen.SubjectId);
                }
            }
        }

    }
}
