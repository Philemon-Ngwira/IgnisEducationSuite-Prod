namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Tracks which (day, startTime) slots each teacher is already committed to, so the generator
    /// never double-books a teacher across subjects/sections. Preload from the DB once before
    /// generation starts (already-saved commitments), then MarkBusy as new placements are made
    /// during this run.
    /// </summary>
    public class TeacherConflictChecker
    {
        private readonly Dictionary<Guid, HashSet<(DayOfWeek, TimeSpan)>> _cache = new();

        public void Preload(Guid teacherId, HashSet<(DayOfWeek Day, TimeSpan StartTime)> busySlots)
        {
            if (teacherId == Guid.Empty) return;
            _cache[teacherId] = busySlots;
        }

        public bool IsTeacherBusy(Guid teacherId, DayOfWeek day, TimeSpan startTime)
        {
            if (teacherId == Guid.Empty) return false;
            return _cache.TryGetValue(teacherId, out var busy) && busy.Contains((day, startTime));
        }

        public void MarkBusy(Guid teacherId, DayOfWeek day, TimeSpan startTime)
        {
            if (teacherId == Guid.Empty) return;

            if (!_cache.TryGetValue(teacherId, out var busy))
            {
                busy = new HashSet<(DayOfWeek, TimeSpan)>();
                _cache[teacherId] = busy;
            }

            busy.Add((day, startTime));
        }

        /// <summary>
        /// Releases a commitment. Needed by the interactive editor: clearing or reassigning a cell
        /// must free the teacher's slot, otherwise the checker keeps reporting a conflict against a
        /// placement that no longer exists.
        /// </summary>
        public void MarkFree(Guid teacherId, DayOfWeek day, TimeSpan startTime)
        {
            if (teacherId == Guid.Empty) return;
            if (_cache.TryGetValue(teacherId, out var busy)) busy.Remove((day, startTime));
        }
    }
}
