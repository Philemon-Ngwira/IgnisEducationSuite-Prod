using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using System.Collections.Generic;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public partial class TimetableState
    {

        public List<TimeSlot> Slots { get; }
        public Dictionary<Guid, SubjectScheduleConfig> Subjects { get; }
        public Dictionary<Guid, SubjectAdjacencyConstraints> Adjacency { get; }

        // Derived state (cached)
        private Dictionary<Guid, int> _weeklyCount;
        private Dictionary<(DayOfWeek, Guid), int> _dailyCount;

        public TimetableState(
            List<TimeSlot> slots,
            List<SubjectScheduleConfig> subjects,
            List<SubjectAdjacencyConstraints> adjacency)
        {
            Slots = slots;
            Subjects = subjects.ToDictionary(s => s.SubjectId);
            Adjacency = adjacency.ToDictionary(a => a.SubjectId);

            RebuildIndexes();
        }

        // ---------- INDEXING ----------

        public void RebuildIndexes()
        {
            _weeklyCount = new();
            _dailyCount = new();

            foreach (var slot in Slots.Where(s => s.SubjectId != Guid.Empty))
            {
                _weeklyCount.TryAdd(slot.SubjectId, 0);
                _weeklyCount[slot.SubjectId]++;

                var key = (slot.Day, slot.SubjectId);
                _dailyCount.TryAdd(key, 0);
                _dailyCount[key]++;
            }
        }

        // ---------- QUERIES ----------

        public int WeeklyCount(Guid subjectId)
            => _weeklyCount.TryGetValue(subjectId, out var c) ? c : 0;

        public int DailyCount(DayOfWeek day, Guid subjectId)
            => _dailyCount.TryGetValue((day, subjectId), out var c) ? c : 0;

        public int WeeklyRemaining(Guid subjectId)
            => Subjects[subjectId].WeeklyPeriods - WeeklyCount(subjectId);

        public int RequiredDoublesRemaining(Guid subjectId)
        {
            var required = Subjects[subjectId].RequiredDoubles;
            var placed = CountPlacedDoubles(subjectId);
            return Math.Max(0, required - placed);
        }

        public bool ExceedsDailyMax(DayOfWeek day, Guid subjectId)
            => DailyCount(day, subjectId) > 2;

        public IEnumerable<TimeSlot> SlotsForDay(DayOfWeek day)
            => Slots.Where(s => s.Day == day).OrderBy(s => s.StartTime);

        public IEnumerable<TimeSlot> FreeSlots()
            => Slots.Where(s => s.SubjectId == Guid.Empty);

        // ---------- DOUBLE DETECTION ----------

        public int CountPlacedDoubles(Guid subjectId)
        {
            int count = 0;

            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var daySlots = SlotsForDay(day)
                    .Where(s => s.SubjectId == subjectId)
                    .ToList();

                for (int i = 0; i < daySlots.Count - 1; i++)
                {
                    if (daySlots[i + 1].StartTime == daySlots[i].EndTime)
                        count++;
                }
            }

            return count;
        }

        // ---------- VALIDATION HELPERS ----------

        public bool ViolatesAdjacency(TimeSlot prev, TimeSlot next)
        {
            if (prev.SubjectId == Guid.Empty || next.SubjectId == Guid.Empty)
                return false;

            if (!Adjacency.ContainsKey(next.SubjectId))
                return false;

            return Adjacency[next.SubjectId]
                .CannotFollowSubjects
                .Contains(prev.SubjectId);
        }
    }
}
