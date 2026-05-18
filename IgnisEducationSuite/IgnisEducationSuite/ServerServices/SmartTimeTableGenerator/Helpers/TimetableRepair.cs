using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimeTableRepair
    {
        private const int MaxRepairPasses = 2;
        private readonly TeacherConflictChecker _teacherChecker;

        public TimeTableRepair(TeacherConflictChecker teacherChecker)
        {
            _teacherChecker = teacherChecker;
        }

        public void Repair(TimetableState state, TimeTableActivity? prepActivity = null)
        {
            // 1️⃣ Enforce fixed activities & locks
            EnforceActivityOwnership(state, prepActivity);

            for (int pass = 0; pass < MaxRepairPasses; pass++)
            {
                // 2️⃣ Required doubles first (hard structure)
                PlaceRequiredDoubles(state);

                // 3️⃣ Fill remaining singles (quota-safe)
                FillRemainingSingles(state);

                // 4️⃣ Validate hard constraints
                var validator = new TimetableValidator();
                var report = validator.Analyze(
                    state,
                    state.Subjects,
                    new Dictionary<Guid, SubjectAdjacencyConstraints>()
                );

                if (!report.InvariantViolations.Any())
                    break;
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
                    slot.SubjectName = string.Empty;
                    slot.IsLocked = true;
                }
            }

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

            state.RebuildIndexes();
        }

        // ------------------------------------------------
        // 2️⃣ Required Doubles Placement
        // ------------------------------------------------
        private void PlaceRequiredDoubles(TimetableState state)
        {
            foreach (var subject in state.Subjects.Values)
            {
                while (state.RequiredDoublesRemaining(subject.SubjectId) > 0 &&
                       state.WeeklyRemaining(subject.SubjectId) >= 2)
                {
                    bool placed = false;

                    foreach (var day in Enum.GetValues<DayOfWeek>())
                    {
                        if (state.DailyCount(day, subject.SubjectId) != 0)
                            continue;

                        var daySlots = state.SlotsForDay(day).OrderBy(s => s.StartTime).ToList();

                        for (int i = 0; i < daySlots.Count - 1; i++)
                        {
                            var a = daySlots[i];
                            var b = daySlots[i + 1];

                            if (!IsSlotValidForDouble(a, subject, day)) continue;
                            if (!IsSlotValidForDouble(b, subject, day)) continue;
                            if (a.EndTime != b.StartTime) continue;

                            if (state.WeeklyRemaining(subject.SubjectId) < 2) break;

                            PlaceSubject(a, subject.SubjectId, state);
                            PlaceSubject(b, subject.SubjectId, state);

                            placed = true;
                            break;
                        }

                        if (placed) break;
                    }

                    if (!placed) break;
                }
            }
        }

        private bool IsSlotValidForDouble(TimeSlot slot, SubjectScheduleConfig subject, DayOfWeek day)
        {
            if (slot.IsLocked ||
                slot.SubjectId != Guid.Empty ||
                slot.ScheduledActivityId != null)
                return false;

            if (subject.EarlyMorningOnly &&
                slot.StartTime >= TimeSpan.FromHours(10.5))
                return false;

            // ✅ Teacher conflict check
            if (_teacherChecker.IsTeacherBusy(subject.TeacherId, day, slot.StartTime.Value))
                return false;

            return true;
        }

        // ------------------------------------------------
        // 3️⃣ Fill Remaining Singles
        // ------------------------------------------------
        private void FillRemainingSingles(TimetableState state)
        {
            foreach (var subject in state.Subjects.Values)
            {
                foreach (var day in Enum.GetValues<DayOfWeek>())
                {
                    if (state.WeeklyRemaining(subject.SubjectId) <= 0)
                        break;

                    if (state.DailyCount(day, subject.SubjectId) >= 2)
                        continue;

                    var freeSlots = state.SlotsForDay(day)
                        .Where(s =>
                            s.SubjectId == Guid.Empty &&
                            s.ScheduledActivityId == null &&
                            !s.IsLocked)
                        .OrderBy(s => s.StartTime)
                        .ToList();

                    foreach (var slot in freeSlots)
                    {
                        if (state.WeeklyRemaining(subject.SubjectId) <= 0)
                            break;

                        if (subject.EarlyMorningOnly &&
                            slot.StartTime >= TimeSpan.FromHours(10.5))
                            continue;

                        // ✅ Teacher conflict check
                        if (_teacherChecker.IsTeacherBusy(subject.TeacherId, day, slot.StartTime.Value))
                            continue;

                        var prev = state.SlotsForDay(day)
                            .FirstOrDefault(s => s.EndTime == slot.StartTime);

                        if (prev != null && state.ViolatesAdjacency(prev, slot))
                            continue;

                        PlaceSubject(slot, subject.SubjectId, state);
                    }
                }
            }
        }

        // ------------------------------------------------
        // HARD GUARD PLACEMENT
        // ------------------------------------------------
        private void PlaceSubject(TimeSlot slot, Guid subjectId, TimetableState state)
        {
            if (state.WeeklyRemaining(subjectId) <= 0) return;

            var subject = state.Subjects[subjectId];

            // ✅ Final teacher conflict guard before committing
            if (_teacherChecker.IsTeacherBusy(subject.TeacherId, slot.Day, slot.StartTime.Value))
                return;

            slot.SubjectId = subjectId;
            slot.SubjectName = subject.SubjectName;

            // ✅ Mark teacher busy after placement
            _teacherChecker.MarkBusy(subject.TeacherId, slot.Day, slot.StartTime.Value);

            state.RebuildIndexes();
        }
    }
}