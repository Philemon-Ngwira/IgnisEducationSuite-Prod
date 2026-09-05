using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Wraps one section's generation-in-progress slot list plus its subject configs and
    /// adjacency rules, with cached weekly/daily placement counts.
    ///
    /// Also owns the school's derived time-of-day boundaries. Every pass (generation, repair,
    /// fill, optimization, validation) reads the cutoffs from here rather than hardcoding a clock
    /// time, so they cannot disagree about when "early morning" ends for a given school.
    /// </summary>
    public class TimetableState
    {
        public List<GenerationSlot> Slots { get; }
        public Dictionary<Guid, SubjectScheduleConfigDto> Subjects { get; }

        /// <summary>subjectId -> set of subjectIds that must not immediately precede it.</summary>
        public Dictionary<Guid, HashSet<Guid>> Adjacency { get; }

        /// <summary>End of the last EarlyMorning slot. Zero when the school defines no such slots,
        /// which makes EarlyMorningOnly unsatisfiable — the validator reports that rather than
        /// silently falling back to an arbitrary clock time.</summary>
        public TimeSpan EarlyMorningCutoff { get; }

        /// <summary>Start of the first Afternoon slot, or TimeSpan.MaxValue when the school defines
        /// none (making every teaching slot "morning").</summary>
        public TimeSpan AfternoonStart { get; }

        private Dictionary<Guid, int> _weeklyCount = new();
        private Dictionary<(DayOfWeek, Guid), int> _dailyCount = new();

        public TimetableState(
            List<GenerationSlot> slots,
            List<SubjectScheduleConfigDto> subjects,
            List<SubjectAdjacencyRuleDto> adjacencyRules)
        {
            Slots = slots;
            Subjects = subjects.ToDictionary(s => s.ClassId);

            EarlyMorningCutoff = slots
                .Where(s => s.SlotType == SlotTypes.EarlyMorning && s.EndTime.HasValue)
                .Select(s => s.EndTime!.Value)
                .DefaultIfEmpty(TimeSpan.Zero)
                .Max();

            AfternoonStart = slots
                .Where(s => s.SlotType == SlotTypes.Afternoon && s.StartTime.HasValue)
                .Select(s => s.StartTime!.Value)
                .DefaultIfEmpty(TimeSpan.MaxValue)
                .Min();

            Adjacency = new Dictionary<Guid, HashSet<Guid>>();
            foreach (var rule in adjacencyRules)
            {
                if (!Adjacency.TryGetValue(rule.ClassId, out var set))
                {
                    set = new HashSet<Guid>();
                    Adjacency[rule.ClassId] = set;
                }
                set.Add(rule.CannotFollowClassId);
            }

            RebuildIndexes();
        }

        // ---------- Time preference ----------

        /// <summary>
        /// The single authority on whether a subject may occupy a slot's time of day. Every pass
        /// routes through this: previously only EarlyMorningOnly was ever checked (and only by the
        /// initial generator), so MorningOnly and AfternoonOnly were silently ignored everywhere.
        /// </summary>
        public bool SatisfiesTimePreference(SubjectScheduleConfigDto subject, GenerationSlot slot)
        {
            if (slot.StartTime is not { } start) return true;

            return subject.TimePreference switch
            {
                SubjectTimePreference.EarlyMorningOnly => start < EarlyMorningCutoff,
                SubjectTimePreference.MorningOnly => start < AfternoonStart,
                SubjectTimePreference.AfternoonOnly => start >= AfternoonStart,
                _ => true,
            };
        }

        public bool SatisfiesTimePreference(Guid subjectId, GenerationSlot slot) =>
            !Subjects.TryGetValue(subjectId, out var subject) || SatisfiesTimePreference(subject, slot);

        // ---------- Indexing ----------

        /// <summary>Full rescan — only called at construction and after activity ownership is
        /// enforced. Per-placement updates use PlaceSubject/ClearSlot instead.</summary>
        public void RebuildIndexes()
        {
            _weeklyCount = new Dictionary<Guid, int>();
            _dailyCount = new Dictionary<(DayOfWeek, Guid), int>();

            foreach (var slot in Slots.Where(s => s.IsAcademic))
            {
                _weeklyCount.TryAdd(slot.SubjectId, 0);
                _weeklyCount[slot.SubjectId]++;

                var key = (slot.Day, slot.SubjectId);
                _dailyCount.TryAdd(key, 0);
                _dailyCount[key]++;
            }
        }

        /// <summary>
        /// Places a subject into a slot and incrementally updates the count indexes — avoids the
        /// full-rescan-per-placement cost RebuildIndexes() would have if called on every placement.
        /// </summary>
        public void PlaceSubject(GenerationSlot slot, Guid subjectId)
        {
            slot.SubjectId = subjectId;
            slot.SubjectName = Subjects.TryGetValue(subjectId, out var subject) ? subject.SubjectName : null;

            _weeklyCount.TryAdd(subjectId, 0);
            _weeklyCount[subjectId]++;

            var key = (slot.Day, subjectId);
            _dailyCount.TryAdd(key, 0);
            _dailyCount[key]++;
        }

        /// <summary>Clears a slot's subject and decrements the count indexes to match — the
        /// counterpart to PlaceSubject, used by the optimizer's swap moves so counts never
        /// drift out of sync with the actual slot contents.</summary>
        public void ClearSlot(GenerationSlot slot)
        {
            if (slot.SubjectId == Guid.Empty) return;

            var subjectId = slot.SubjectId;
            if (_weeklyCount.TryGetValue(subjectId, out var weekly) && weekly > 0)
                _weeklyCount[subjectId] = weekly - 1;

            var key = (slot.Day, subjectId);
            if (_dailyCount.TryGetValue(key, out var daily) && daily > 0)
                _dailyCount[key] = daily - 1;

            slot.SubjectId = Guid.Empty;
            slot.SubjectName = "Free";
        }

        /// <summary>Swaps the subject assignment of two slots, keeping the count indexes correct
        /// throughout (each slot may be empty, so this handles all four combinations).</summary>
        public void SwapSlots(GenerationSlot a, GenerationSlot b)
        {
            var aSubjectId = a.SubjectId;
            var bSubjectId = b.SubjectId;
            var aWasDouble = a.IsDoublePeriod;
            var bWasDouble = b.IsDoublePeriod;

            ClearSlot(a);
            ClearSlot(b);

            if (bSubjectId != Guid.Empty) PlaceSubject(a, bSubjectId);
            if (aSubjectId != Guid.Empty) PlaceSubject(b, aSubjectId);

            a.IsDoublePeriod = bWasDouble;
            b.IsDoublePeriod = aWasDouble;
        }

        // ---------- Queries ----------

        public int WeeklyCount(Guid subjectId) => _weeklyCount.TryGetValue(subjectId, out var c) ? c : 0;

        public int DailyCount(DayOfWeek day, Guid subjectId) => _dailyCount.TryGetValue((day, subjectId), out var c) ? c : 0;

        public int WeeklyRemaining(Guid subjectId) =>
            Subjects.TryGetValue(subjectId, out var s) ? s.WeeklyPeriods - WeeklyCount(subjectId) : 0;

        public int RequiredDoublesRemaining(Guid subjectId)
        {
            if (!Subjects.TryGetValue(subjectId, out var s)) return 0;
            return Math.Max(0, s.RequiredDoubles - CountPlacedDoubles(subjectId));
        }

        public bool ExceedsDailyMax(DayOfWeek day, Guid subjectId) => DailyCount(day, subjectId) > 2;

        public IEnumerable<GenerationSlot> SlotsForDay(DayOfWeek day) =>
            Slots.Where(s => s.Day == day).OrderBy(s => s.StartTime);

        public IEnumerable<GenerationSlot> FreeSlots() => Slots.Where(s => s.IsFree);

        public int CountPlacedDoubles(Guid subjectId)
        {
            var count = 0;

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                var daySlots = SlotsForDay(day).Where(s => s.SubjectId == subjectId).ToList();

                for (var i = 0; i < daySlots.Count - 1; i++)
                {
                    if (daySlots[i + 1].StartTime == daySlots[i].EndTime)
                        count++;
                }
            }

            return count;
        }

        public bool ViolatesAdjacency(GenerationSlot prev, GenerationSlot next)
        {
            if (prev.SubjectId == Guid.Empty || next.SubjectId == Guid.Empty) return false;
            if (!Adjacency.TryGetValue(next.SubjectId, out var cannotFollow)) return false;
            return cannotFollow.Contains(prev.SubjectId);
        }
    }
}
