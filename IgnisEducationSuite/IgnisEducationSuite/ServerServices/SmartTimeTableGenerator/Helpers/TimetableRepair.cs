using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimeTableRepair
    {
        private const int MaxRepairPasses = 2; // Number of iterative repair attempts

        public void Repair(TimetableState state, TimeTableActivity? prepActivity = null)
        {
            // 1️⃣ Enforce activities
            EnforceActivityOwnership(state, prepActivity);

            for (int pass = 0; pass <= MaxRepairPasses; pass++)
            {
                // 2️⃣ Place required doubles first
                PlaceRequiredDoubles(state);

                // 3️⃣ Fill remaining singles safely
                FillRemainingSingles(state);

                // 4️⃣ Validate hard constraints
                var validator = new TimetableValidator();
                var report = validator.Analyze(state, state.Subjects, new Dictionary<Guid, SubjectAdjacencyConstraints>());

                if (!report.InvariantViolations.Any())
                    break; // Stop early if perfect
            }
        }

        // ------------------------------------------------
        // 1️⃣ Activity Enforcement
        // ------------------------------------------------
        private void EnforceActivityOwnership(TimetableState state, TimeTableActivity? prepActivity)
        {
            foreach (var slot in state.Slots)
            {
                if (slot.ScheduledActivityId != null)
                {
                    slot.SubjectId = Guid.Empty;
                    slot.IsLocked = true;
                }
            }

            // Optional: if prepActivity provided, ensure its slots are locked
            if (prepActivity != null)
            {
                foreach (var slot in state.Slots.Where(s => s.StartTime >= prepActivity.StartFrom))
                {
                    slot.SubjectId = Guid.Empty;
                    slot.SubjectName = prepActivity.ActivityName;
                    slot.ScheduledActivityId = prepActivity.ActivityID;
                    slot.IsLocked = true;
                }
            }
        }

        // ------------------------------------------------
        // 2️⃣ Required Doubles Placement
        // ------------------------------------------------
        private void PlaceRequiredDoubles(TimetableState state)
        {
            foreach (var subject in state.Subjects.Values)
            {
                int remainingDoubles = state.RequiredDoublesRemaining(subject.SubjectId);

                if (remainingDoubles <= 0)
                    continue;

                // Try to place doubles earliest-first
                foreach (var day in Enum.GetValues<DayOfWeek>())
                {
                    if (state.DailyCount(day, subject.SubjectId) != 0)
                        continue; // Skip days where subject already appears

                    var daySlots = state.SlotsForDay(day).ToList();

                    for (int i = 0; i < daySlots.Count - 1; i++)
                    {
                        var a = daySlots[i];
                        var b = daySlots[i + 1];

                        if (!IsSlotValidForDouble(a, subject, state) ||
                            !IsSlotValidForDouble(b, subject, state))
                            continue;

                        if (a.EndTime != b.StartTime)
                            continue; // must be consecutive

                        // Place double
                        PlaceSubject(a, subject.SubjectId, state);
                        PlaceSubject(b, subject.SubjectId, state);

                        remainingDoubles--;
                        if (remainingDoubles == 0) break;
                    }

                    if (remainingDoubles == 0) break;
                }
            }
        }

        private bool IsSlotValidForDouble(TimeSlot slot, SubjectScheduleConfig subject, TimetableState state)
        {
            if (slot.IsLocked || slot.SubjectId != Guid.Empty || slot.ScheduledActivityId != null)
                return false;

            if (subject.EarlyMorningOnly && slot.StartTime >= TimeSpan.FromHours(10.5))
                return false;

            return true;
        }

        private void PlaceSubject(TimeSlot slot, Guid subjectId, TimetableState state)
        {
            slot.SubjectId = subjectId;
            slot.SubjectName = state.Subjects[subjectId].SubjectName;
            state.RebuildIndexes(); // update daily/weekly counts
        }

        // ------------------------------------------------
        // 3️⃣ Fill Remaining Singles
        // ------------------------------------------------
        private void FillRemainingSingles(TimetableState state)
        {
            foreach (var subject in state.Subjects.Values)
            {
                int remaining = state.WeeklyRemaining(subject.SubjectId);

                if (remaining <= 0)
                    continue;

                foreach (var day in Enum.GetValues<DayOfWeek>())
                {
                    if (state.DailyCount(day, subject.SubjectId) >= 2)
                        continue; // respect daily max

                    var daySlots = state.SlotsForDay(day)
                        .Where(s => s.SubjectId == Guid.Empty && s.ScheduledActivityId == null && !s.IsLocked)
                        .OrderBy(s => s.StartTime)
                        .ToList();

                    foreach (var slot in daySlots)
                    {
                        if (remaining <= 0) break;
                        if (subject.EarlyMorningOnly && slot.StartTime >= TimeSpan.FromHours(10.5)) continue;

                        // Check adjacency
                        var prev = state.SlotsForDay(day).Where(s => s.EndTime == slot.StartTime).FirstOrDefault();
                        if (prev != null && state.ViolatesAdjacency(prev, slot)) continue;

                        PlaceSubject(slot, subject.SubjectId, state);
                        remaining--;
                    }
                }
            }
        }
    }
}
