using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimeTableRepair
    {
        /// <summary>
        /// Repairs hard constraints deterministically.
        /// </summary>
        public void Repair(TimetableState state, TimeTableActivity? prepActivity = null)
        {
            FixDailyMax(state);                        // max 2 per day
            FixEarlyMorningSubjects(state);            // early-morning-only enforcement
            ConsolidateNonRequiredDoubles(state);      // make non-required doubles consecutive
            FixRequiredDoubles(state);                 // required doubles atomic placement
            FillFreeSlotsSafely(state, prepActivity);  // fill remaining free slots
            FixAdjacencyViolations(state);             // deterministic adjacency cleanup
        }

        // --------------------------
        // 0. DAILY MAX ENFORCEMENT
        // --------------------------
        private void FixDailyMax(TimetableState state)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var daySlots = state.SlotsForDay(day).ToList();

                var grouped = daySlots
                    .Where(s => s.SubjectId != Guid.Empty)
                    .GroupBy(s => s.SubjectId)
                    .Where(g => g.Count() > 2);

                foreach (var g in grouped)
                {
                    var extras = g.OrderBy(s => s.StartTime).Skip(2).ToList();
                    foreach (var slot in extras)
                        state.RemoveSubject(slot);
                }
            }
        }

        // ---------------------------
        // 1. EARLY-MORNING ONLY REPAIR
        // ---------------------------
        private void FixEarlyMorningSubjects(TimetableState state)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var daySlots = state.SlotsForDay(day).ToList();

                foreach (var slot in daySlots)
                {
                    if (slot.SubjectId == Guid.Empty) continue;
                    var subject = state.Subjects[slot.SubjectId];
                    if (!subject.EarlyMorningOnly) continue;
                    if (slot.StartTime < TimeSpan.FromHours(10.5)) continue;

                    // Find earliest free early slot
                    var target = daySlots.FirstOrDefault(s => s.SubjectId == Guid.Empty && s.StartTime < TimeSpan.FromHours(10.5));
                    if (target != null)
                    {
                        state.RemoveSubject(slot);
                        state.PlaceSubject(target, subject.SubjectId);
                    }
                }
            }
        }

        // ----------------------------------------
        // 2. CONSOLIDATE NON-REQUIRED DOUBLES
        // ----------------------------------------
        private void ConsolidateNonRequiredDoubles(TimetableState state)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var daySlots = state.SlotsForDay(day).ToList();

                var subjectsTwice = daySlots
                    .Where(s => s.SubjectId != Guid.Empty)
                    .GroupBy(s => s.SubjectId)
                    .Where(g => g.Count() == 2)
                    .ToList();

                foreach (var g in subjectsTwice)
                {
                    var slots = g.OrderBy(s => s.StartTime).ToList();
                    if (slots[0].EndTime == slots[1].StartTime) continue;

                    var firstIndex = daySlots.IndexOf(slots[0]);
                    var nextSlot = daySlots.Skip(firstIndex + 1).FirstOrDefault(s => s.SubjectId == Guid.Empty);

                    if (nextSlot != null)
                    {
                        state.RemoveSubject(slots[1]);
                        state.PlaceSubject(nextSlot, g.Key);
                    }
                }
            }
        }

        // -------------------------------
        // 3. REQUIRED DOUBLES (ATOMIC)
        // -------------------------------
        private void FixRequiredDoubles(TimetableState state)
        {
            foreach (var subject in state.Subjects.Values.OrderBy(s => s.SubjectName))
            {
                while (state.RequiredDoublesRemaining(subject.SubjectId) > 0)
                {
                    bool placed = false;

                    foreach (var day in Enum.GetValues<DayOfWeek>())
                    {
                        if (state.DailyCount(day, subject.SubjectId) != 0) continue;

                        var slots = state.SlotsForDay(day).ToList();
                        for (int i = 0; i < slots.Count - 1; i++)
                        {
                            var a = slots[i];
                            var b = slots[i + 1];

                            if (a.SubjectId == Guid.Empty &&
                                b.SubjectId == Guid.Empty &&
                                a.EndTime == b.StartTime)
                            {
                                state.PlaceSubject(a, subject.SubjectId);
                                state.PlaceSubject(b, subject.SubjectId);
                                placed = true;
                                break;
                            }
                        }

                        if (placed) break;
                    }

                    if (!placed) break; // cannot place safely
                }
            }
        }

        // -------------------------------
        // 4. SAFE FREE SLOT FILLING
        // -------------------------------
        private void FillFreeSlotsSafely(TimetableState state, TimeTableActivity? prepActivity)
        {
            foreach (var slot in state.FreeSlots().ToList())
            {
                if (prepActivity != null && slot.StartTime >= prepActivity.StartFrom) continue;

                var candidates = state.Subjects.Values
                    .Where(s => state.WeeklyRemaining(s.SubjectId) > 0 &&
                                state.DailyCount(slot.Day, s.SubjectId) < 2 &&
                                (!s.EarlyMorningOnly || slot.StartTime < TimeSpan.FromHours(10.5)))
                    .OrderByDescending(s => state.WeeklyRemaining(s.SubjectId))
                    .ThenBy(s => s.SubjectName)
                    .ToList();

                if (!candidates.Any()) continue;

                var chosen = candidates.First();
                state.PlaceSubject(slot, chosen.SubjectId);
            }
        }

        // ----------------------------------------
        // 5. ADJACENCY CLEANUP
        // ----------------------------------------
        private void FixAdjacencyViolations(TimetableState state)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var slots = state.SlotsForDay(day).ToList();

                for (int i = 1; i < slots.Count; i++)
                {
                    var prev = slots[i - 1];
                    var curr = slots[i];

                    if (curr.SubjectId == Guid.Empty || prev.SubjectId == Guid.Empty) continue;

                    if (state.ViolatesAdjacency(prev, curr))
                    {
                        state.RemoveSubject(curr); // deterministic removal
                    }
                }
            }
        }
    }
}
