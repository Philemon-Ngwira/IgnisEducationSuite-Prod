namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Hill-climbing local search over a fully-repaired timetable: tries pairwise slot swaps within
    /// each day, keeps a swap only if it improves TimetableScorer.Score and doesn't undo a hard
    /// constraint already satisfied (teacher availability, required doubles, time preference,
    /// daily max, adjacency), otherwise reverts. Runs after repair + the builder's fill pass, since
    /// it assumes a mostly/fully-filled board to rearrange rather than fill.
    ///
    /// Teacher commitments are released before each trial swap and re-reserved afterwards, so a
    /// swap is evaluated against the conflicts that would genuinely remain — and so the checker
    /// still reflects reality once optimization finishes. Without this the optimizer can move a
    /// subject onto a slot where its teacher is already committed to another section, quietly
    /// undoing the guarantee the generation pass established.
    /// </summary>
    public class TimetableOptimizer
    {
        private const int MaxIterations = 200;

        private readonly TeacherConflictChecker _teacherChecker;

        public TimetableOptimizer(TeacherConflictChecker teacherChecker)
        {
            _teacherChecker = teacherChecker;
        }

        public void Optimize(TimetableState state)
        {
            var currentScore = TimetableScorer.Score(state);
            var improved = true;
            var iterations = 0;

            while (improved && iterations < MaxIterations)
            {
                improved = false;
                iterations++;

                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

                    var daySlots = state.SlotsForDay(day).ToList();

                    for (var i = 0; i < daySlots.Count; i++)
                    {
                        for (var j = i + 1; j < daySlots.Count; j++)
                        {
                            var a = daySlots[i];
                            var b = daySlots[j];

                            if (a.IsLocked || b.IsLocked || a.IsActivity || b.IsActivity) continue;
                            if (a.IsDoublePeriod || b.IsDoublePeriod) continue; // never break up a double
                            if (a.SubjectId == b.SubjectId) continue;           // nothing to gain

                            ReleaseTeachers(state, a, b);
                            TimetableMoves.Swap(state, a, b);

                            if (!TeachersAvailable(state, a, b) || ViolatesHardConstraints(state, a, b, day))
                            {
                                TimetableMoves.Swap(state, a, b); // revert
                                ReserveTeachers(state, a, b);
                                continue;
                            }

                            var newScore = TimetableScorer.Score(state);
                            if (newScore > currentScore)
                            {
                                ReserveTeachers(state, a, b);
                                currentScore = newScore;
                                improved = true;
                            }
                            else
                            {
                                TimetableMoves.Swap(state, a, b); // revert
                                ReserveTeachers(state, a, b);
                            }
                        }
                    }
                }
            }
        }

        // ---------- Teacher commitment bookkeeping ----------

        private Guid TeacherOf(TimetableState state, GenerationSlot slot) =>
            slot.SubjectId != Guid.Empty && state.Subjects.TryGetValue(slot.SubjectId, out var subject)
                ? subject.TeacherId ?? Guid.Empty
                : Guid.Empty;

        private void ReleaseTeachers(TimetableState state, params GenerationSlot[] slots)
        {
            foreach (var slot in slots)
            {
                if (slot.StartTime is not { } start) continue;
                _teacherChecker.MarkFree(TeacherOf(state, slot), slot.Day, start);
            }
        }

        private void ReserveTeachers(TimetableState state, params GenerationSlot[] slots)
        {
            foreach (var slot in slots)
            {
                if (slot.StartTime is not { } start) continue;
                _teacherChecker.MarkBusy(TeacherOf(state, slot), slot.Day, start);
            }
        }

        private bool TeachersAvailable(TimetableState state, params GenerationSlot[] slots)
        {
            foreach (var slot in slots)
            {
                if (slot.StartTime is not { } start) continue;

                var teacherId = TeacherOf(state, slot);
                if (teacherId == Guid.Empty) continue;

                if (_teacherChecker.IsTeacherBusy(teacherId, slot.Day, start)) return false;
            }

            return true;
        }

        // ---------- Hard constraints ----------

        private static bool ViolatesHardConstraints(TimetableState state, GenerationSlot a, GenerationSlot b, DayOfWeek day)
        {
            foreach (var slot in new[] { a, b })
            {
                if (slot.SubjectId == Guid.Empty) continue;
                if (!state.Subjects.TryGetValue(slot.SubjectId, out var subject)) continue;

                if (!state.SatisfiesTimePreference(subject, slot)) return true;
                if (state.ExceedsDailyMax(day, slot.SubjectId)) return true;
            }

            var daySlots = state.SlotsForDay(day).ToList();
            foreach (var slot in new[] { a, b })
            {
                var prev = daySlots.FirstOrDefault(s => s.EndTime == slot.StartTime);
                if (prev is not null && state.ViolatesAdjacency(prev, slot)) return true;

                var next = daySlots.FirstOrDefault(s => s.StartTime == slot.EndTime);
                if (next is not null && state.ViolatesAdjacency(slot, next)) return true;
            }

            return false;
        }
    }
}
