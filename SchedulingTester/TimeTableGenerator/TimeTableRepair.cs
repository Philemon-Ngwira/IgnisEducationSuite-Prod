using System;
using System.Collections.Generic;
using System.Linq;

namespace SchedulingTester.TimeTableGenerator
{
    public class TimeTableRepair
    {
        public void Repair(TimetableState state, Activity? prepActivity = null)
        {
            FixDailyMax(state);                        // enforce max 2 per day
            FixEarlyMorningSubjects(state);            // early-morning only repair
            ConsolidateNonRequiredDoubles(state);      // make doubles consecutive
            FixRequiredDoubles(state);                 // required doubles atomic placement
            FillFreeSlotsSafely(state, prepActivity);  // fill remaining free slots deterministically
            FixAdjacencyViolationsWithSwap(state);     // swap or remove adjacency violations deterministically
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
                    // Keep first two, remove extras
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
                    if (slot.SubjectId == Guid.Empty)
                        continue;

                    var subject = state.Subjects[slot.SubjectId];

                    if (!subject.EarlyMorningOnly)
                        continue;

                    if (slot.StartTime < TimeSpan.FromHours(10.5))
                        continue;

                    // Try to find earliest free early slot
                    var target = daySlots.FirstOrDefault(s =>
                        s.SubjectId == Guid.Empty &&
                        s.StartTime < TimeSpan.FromHours(10.5));

                    if (target != null)
                    {
                        state.RemoveSubject(slot);
                        state.PlaceSubject(target, subject.SubjectId);
                        continue;
                    }

                    // Swap with non-early subject deterministically
                    var swapCandidate = daySlots
                        .Where(s => s.StartTime < TimeSpan.FromHours(10.5) &&
                                    !state.Subjects[s.SubjectId].EarlyMorningOnly)
                        .OrderBy(s => s.StartTime)
                        .FirstOrDefault();

                    if (swapCandidate != null)
                    {
                        state.RemoveSubject(swapCandidate);
                        state.PlaceSubject(swapCandidate, subject.SubjectId);
                        state.PlaceSubject(slot, swapCandidate.SubjectId);
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

                    if (slots[0].EndTime != slots[1].StartTime)
                    {
                        var firstIndex = daySlots.IndexOf(slots[0]);
                        var nextSlot = daySlots[firstIndex + 1];

                        if (nextSlot.SubjectId == Guid.Empty)
                        {
                            state.RemoveSubject(slots[1]);
                            state.PlaceSubject(nextSlot, g.Key);
                        }
                        else
                        {
                            var freeAdj = daySlots.Skip(firstIndex + 1)
                                .FirstOrDefault(s => s.SubjectId == Guid.Empty);

                            if (freeAdj != null)
                            {
                                state.RemoveSubject(slots[1]);
                                state.PlaceSubject(freeAdj, g.Key);
                            }
                        }
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
                        if (state.DailyCount(day, subject.SubjectId) != 0)
                            continue;

                        var slots = state.SlotsForDay(day).ToList();

                        for (int i = 0; i < slots.Count - 1; i++)
                        {
                            var a = slots[i];
                            var b = slots[i + 1];

                            if (a.SubjectId == Guid.Empty &&
                                b.SubjectId == Guid.Empty &&
                                a.EndTime == b.StartTime &&
                                state.CanPlaceSubject(day, subject.SubjectId))
                            {
                                state.PlaceSubject(a, subject.SubjectId);
                                state.PlaceSubject(b, subject.SubjectId);
                                placed = true;
                                break;
                            }
                        }

                        if (placed)
                            break;
                    }

                    if (!placed)
                    {
                        // Deterministic forced replacement
                        foreach (var day in Enum.GetValues<DayOfWeek>())
                        {
                            var slots = state.SlotsForDay(day).ToList();

                            for (int i = 0; i < slots.Count - 1; i++)
                            {
                                var a = slots[i];
                                var b = slots[i + 1];

                                if (a.EndTime != b.StartTime)
                                    continue;

                                var replaceCandidates = new[] { a, b }
                                    .Select(s => state.Subjects[s.SubjectId])
                                    .Where(s => s.RequiredDoubles == 0)
                                    .OrderBy(s => state.WeeklyRemaining(s.SubjectId))
                                    .ThenBy(s => s.SubjectName)
                                    .ToList();

                                if (replaceCandidates.Count == 2)
                                {
                                    state.RemoveSubject(a);
                                    state.RemoveSubject(b);
                                    state.PlaceSubject(a, subject.SubjectId);
                                    state.PlaceSubject(b, subject.SubjectId);
                                    placed = true;
                                    break;
                                }
                            }

                            if (placed)
                                break;
                        }
                    }

                    if (!placed)
                        break; // cannot place
                }
            }
        }

        // -------------------------------
        // 4. SAFE FREE SLOT FILLING (DETERMINISTIC)
        // -------------------------------
        private void FillFreeSlotsSafely(TimetableState state, Activity? prepActivity)
        {
            foreach (var slot in state.FreeSlots().ToList())
            {
                if (prepActivity != null && slot.StartTime >= prepActivity.StartFrom)
                    continue;

                var candidates = state.Subjects.Values
                    .Where(s =>
                        state.WeeklyRemaining(s.SubjectId) > 0 &&
                        state.DailyCount(slot.Day, s.SubjectId) < 2 &&
                        (!s.EarlyMorningOnly || slot.StartTime < TimeSpan.FromHours(10.5)))
                    .OrderByDescending(s => state.WeeklyRemaining(s.SubjectId))
                    .ThenBy(s => s.SubjectName)
                    .ToList();

                if (!candidates.Any())
                    continue;

                TimeSlot nextSlot = null;
                var chosen = candidates.First();

                var todayCount = state.DailyCount(slot.Day, chosen.SubjectId);
                if (todayCount == 0)
                {
                    var daySlots = state.SlotsForDay(slot.Day).ToList();
                    var index = daySlots.IndexOf(slot);

                    if (index < daySlots.Count - 1 && daySlots[index + 1].SubjectId == Guid.Empty)
                        nextSlot = daySlots[index + 1];
                }

                state.PlaceSubject(slot, chosen.SubjectId);

                if (nextSlot != null)
                    state.PlaceSubject(nextSlot, chosen.SubjectId);
            }
        }

        // ----------------------------------------
        // 5. ADJACENCY CLEANUP (DETERMINISTIC SWAP)
        // ----------------------------------------
        private void FixAdjacencyViolationsWithSwap(TimetableState state)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var slots = state.SlotsForDay(day).ToList();

                for (int i = 1; i < slots.Count; i++)
                {
                    var prev = slots[i - 1];
                    var curr = slots[i];

                    if (curr.SubjectId == Guid.Empty || prev.SubjectId == Guid.Empty)
                        continue;

                    if (state.ViolatesAdjacency(prev, curr))
                    {
                        var swapCandidate = slots.Skip(i + 1)
                            .FirstOrDefault(s => !state.ViolatesAdjacency(prev, s) &&
                                                 !state.ViolatesAdjacency(s, prev) &&
                                                 s.SubjectId != Guid.Empty);

                        if (swapCandidate != null)
                        {
                            var tmp = swapCandidate.SubjectId;
                            state.PlaceSubject(swapCandidate, curr.SubjectId);
                            state.PlaceSubject(curr, tmp);
                        }
                        else
                        {
                            state.RemoveSubject(curr);
                        }
                    }
                }
            }
        }
    }
}
