using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Hard-constraint repair pass: enforces activity locks, places any required doubles the
    /// initial greedy generation missed, fills remaining singles, re-validates.
    /// </summary>
    public class TimeTableRepair
    {
        private const int MaxRepairPasses = 2;
        private readonly TeacherConflictChecker _teacherChecker;

        public TimeTableRepair(TeacherConflictChecker teacherChecker)
        {
            _teacherChecker = teacherChecker;
        }

        public void Repair(TimetableState state)
        {
            EnforceActivityOwnership(state);

            for (var pass = 0; pass < MaxRepairPasses; pass++)
            {
                PlaceRequiredDoubles(state);
                FillRemainingSingles(state);

                var report = new TimetableValidator().Analyze(state);
                if (report.InvariantViolations.Count == 0) break;
            }
        }

        private static void EnforceActivityOwnership(TimetableState state)
        {
            foreach (var slot in state.Slots.Where(s => s.ScheduledActivityId is not null))
            {
                slot.SubjectId = Guid.Empty;
                slot.SubjectName = string.Empty;
                slot.IsLocked = true;
            }

            state.RebuildIndexes();
        }

        private void PlaceRequiredDoubles(TimetableState state)
        {
            foreach (var subject in state.Subjects.Values)
            {
                while (state.RequiredDoublesRemaining(subject.ClassId) > 0 && state.WeeklyRemaining(subject.ClassId) >= 2)
                {
                    var placed = false;

                    foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                    {
                        if (state.DailyCount(day, subject.ClassId) != 0) continue;

                        var daySlots = state.SlotsForDay(day).ToList();

                        for (var i = 0; i < daySlots.Count - 1; i++)
                        {
                            var a = daySlots[i];
                            var b = daySlots[i + 1];

                            if (!IsSlotValidForDouble(state, a, subject, day)) continue;
                            if (!IsSlotValidForDouble(state, b, subject, day)) continue;
                            if (a.EndTime != b.StartTime) continue;
                            if (state.WeeklyRemaining(subject.ClassId) < 2) break;

                            PlaceSubject(a, subject.ClassId, state);
                            PlaceSubject(b, subject.ClassId, state);
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

        private bool IsSlotValidForDouble(TimetableState state, GenerationSlot slot, SubjectScheduleConfigDto subject, DayOfWeek day)
        {
            if (slot.IsLocked || slot.SubjectId != Guid.Empty || slot.ScheduledActivityId is not null) return false;
            if (!state.SatisfiesTimePreference(subject, slot)) return false;

            return !_teacherChecker.IsTeacherBusy(subject.TeacherId ?? Guid.Empty, day, slot.StartTime!.Value);
        }

        private void FillRemainingSingles(TimetableState state)
        {
            foreach (var subject in state.Subjects.Values)
            {
                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    if (state.WeeklyRemaining(subject.ClassId) <= 0) break;
                    if (state.DailyCount(day, subject.ClassId) >= 2) continue;

                    var freeSlots = state.SlotsForDay(day).Where(s => s.IsFree).ToList();

                    foreach (var slot in freeSlots)
                    {
                        if (state.WeeklyRemaining(subject.ClassId) <= 0) break;
                        if (!state.SatisfiesTimePreference(subject, slot)) continue;

                        if (_teacherChecker.IsTeacherBusy(subject.TeacherId ?? Guid.Empty, day, slot.StartTime!.Value))
                            continue;

                        var prev = state.SlotsForDay(day).FirstOrDefault(s => s.EndTime == slot.StartTime);
                        if (prev is not null && state.ViolatesAdjacency(prev, slot)) continue;

                        PlaceSubject(slot, subject.ClassId, state);
                    }
                }
            }
        }

        private void PlaceSubject(GenerationSlot slot, Guid subjectId, TimetableState state)
        {
            if (state.WeeklyRemaining(subjectId) <= 0) return;
            if (!state.Subjects.TryGetValue(subjectId, out var subject)) return;

            var teacherId = subject.TeacherId ?? Guid.Empty;
            if (_teacherChecker.IsTeacherBusy(teacherId, slot.Day, slot.StartTime!.Value)) return;

            state.PlaceSubject(slot, subjectId);
            _teacherChecker.MarkBusy(teacherId, slot.Day, slot.StartTime.Value);
        }
    }
}
