using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGenerator : ITimetableGenerator
    {
        private Dictionary<DayOfWeek, List<SlotState>> _slotsByDay = new();
        private Dictionary<Guid, SubjectAdjacencyConstraints> _adjacencyBySubject = new();
        private Dictionary<Guid, SubjectTimeConstraints> _timeRulesBySubject = new();
        private List<SubjectScheduleConfig> _schedules;
        private List<TimeTableActivity> _activities = new();

        public GenerationResult Generate(
            IReadOnlyList<TimeSlot> timeSlots,
            IReadOnlyList<SubjectScheduleConfig> schedules,
            IReadOnlyList<SubjectAdjacencyConstraints> adjacencyRules,
            IReadOnlyList<SubjectTimeConstraints> timeRules,
            IReadOnlyList<TimeTableActivity> activities)
        {
            _activities = activities.ToList();
            _schedules = schedules.ToList();
            _slotsByDay = new Dictionary<DayOfWeek, List<SlotState>>();
            _adjacencyBySubject = new Dictionary<Guid, SubjectAdjacencyConstraints>();
            _timeRulesBySubject = new Dictionary<Guid, SubjectTimeConstraints>();

            BuildSlotState(timeSlots);
            BuildRuleLookups(adjacencyRules, timeRules);

            var tasks = BuildPlacementTasks(schedules);

            return PlaceTasks(tasks);
        }

        #region Build Methods

        private void BuildSlotState(IReadOnlyList<TimeSlot> timeSlots)
        {
            foreach (var slot in timeSlots)
            {
                if (!_slotsByDay.ContainsKey(slot.Day))
                    _slotsByDay[slot.Day] = new List<SlotState>();

                _slotsByDay[slot.Day].Add(new SlotState
                {
                    Slot = slot,
                    SubjectId = null,
                    ScheduledActivityId = slot.ScheduledActivityId,
                    IsLocked = slot.ScheduledActivityId != null
                });
            }

            foreach (var day in _slotsByDay.Keys)
                _slotsByDay[day] = _slotsByDay[day].OrderBy(s => s.Slot.StartTime).ToList();
        }

        private void BuildRuleLookups(
            IReadOnlyList<SubjectAdjacencyConstraints> adjacencyRules,
            IReadOnlyList<SubjectTimeConstraints> timeRules)
        {
            foreach (var rule in adjacencyRules)
                _adjacencyBySubject[rule.SubjectId] = rule;

            foreach (var rule in timeRules)
                _timeRulesBySubject[rule.SubjectId] = rule;
        }

        private List<PlacementTask> BuildPlacementTasks(IReadOnlyList<SubjectScheduleConfig> schedules)
        {
            var tasks = new List<PlacementTask>();
            foreach (var schedule in schedules)
            {
                for (int i = 0; i < schedule.DoublePeriods; i++)
                    tasks.Add(new PlacementTask { SubjectId = schedule.SubjectId, IsDouble = true });

                int singleCount = schedule.WeeklyPeriods - (schedule.DoublePeriods * 2);
                for (int i = 0; i < singleCount; i++)
                    tasks.Add(new PlacementTask { SubjectId = schedule.SubjectId, IsDouble = false });
            }

            tasks.Sort((a, b) =>
            {
                int cmp = b.IsDouble.CompareTo(a.IsDouble);
                if (cmp != 0) return cmp;

                cmp = HasTimeConstraint(b.SubjectId).CompareTo(HasTimeConstraint(a.SubjectId));
                if (cmp != 0) return cmp;

                cmp = HasAdjacencyConstraint(b.SubjectId).CompareTo(HasAdjacencyConstraint(a.SubjectId));
                return cmp;
            });

            return tasks;
        }

        private bool HasTimeConstraint(Guid subjectId) => _timeRulesBySubject.ContainsKey(subjectId);
        private bool HasAdjacencyConstraint(Guid subjectId) =>
            _adjacencyBySubject.ContainsKey(subjectId) &&
            _adjacencyBySubject[subjectId].CannotFollowSubjects.Count > 0;

        #endregion

        #region Placement Engine

        private GenerationResult PlaceTasks(List<PlacementTask> tasks)
        {
            var days = _slotsByDay.Keys.ToList();
            var assignedDays = new Dictionary<Guid, HashSet<DayOfWeek>>(); // Track days a subject is already placed
            var dayLoad = days.ToDictionary(d => d, d => 0); // Track how many slots are used per day

            // --- Initialize Filler Queue ---
            var fillerQueue = new Queue<TimeTableActivity>(_activities);

            // --- 1. Place main subjects ---
            foreach (var task in tasks)
            {
                bool placed = false;
                var failureReasons = new List<string>();

                var candidateDays = days.OrderBy(d => dayLoad[d]).ToList();
                foreach (var day in candidateDays)
                {
                    if (assignedDays.TryGetValue(task.SubjectId, out var usedDays) && usedDays.Contains(day))
                        continue;

                    var daySlots = _slotsByDay[day];
                    for (int i = 0; i < daySlots.Count; i++)
                    {
                        var slot = daySlots[i];
                        if (slot.SubjectId != null || slot.ScheduledActivityId != Guid.Empty)
                            continue;

                        if (task.IsDouble)
                        {
                            if (i >= daySlots.Count - 1) continue; // no space for double
                            var nextSlot = daySlots[i + 1];

                            if (TryAssignToSlot(task.SubjectId, slot, nextSlot, out string reason))
                            {
                                AssignSubjectToSlot(task.SubjectId, slot, false);
                                AssignSubjectToSlot(task.SubjectId, nextSlot, false);
                                dayLoad[day] += 2;
                                MarkTaskAssigned(task, day, assignedDays);
                                placed = true;
                                break;
                            }
                            else
                            {
                                failureReasons.Add($"{day} {slot.Slot.StartTime:hh\\:mm}-{nextSlot.Slot.EndTime:hh\\:mm}: {reason}");
                            }
                        }
                        else
                        {
                            if (TryAssignToSlot(task.SubjectId, slot, out string reason))
                            {
                                AssignSubjectToSlot(task.SubjectId, slot, false);
                                dayLoad[day]++;
                                MarkTaskAssigned(task, day, assignedDays);
                                placed = true;
                                break;
                            }
                            else
                            {
                                failureReasons.Add($"{day} {slot.Slot.StartTime:hh\\:mm}-{slot.Slot.EndTime:hh\\:mm}: {reason}");
                            }
                        }
                    }

                    if (placed) break;
                }

                if (!placed)
                    return GenerationResult.Failed(
                        $"Could not place subject {GetSubjectName(task.SubjectId)}. Tried:\n" +
                        string.Join("\n", failureReasons));
            }

            // --- 2. Fill remaining slots with adjacency-aware fillers ---
            foreach (var day in _slotsByDay.Keys)
            {
                var freeSlots = _slotsByDay[day].Where(s => s.SubjectId == null && s.ScheduledActivityId == Guid.Empty).ToList();

                foreach (var slot in freeSlots)
                {
                    bool assigned = false;
                    int attempts = 0;

                    while (!assigned && attempts < fillerQueue.Count)
                    {
                        var filler = fillerQueue.Dequeue();

                        var prevSlot = GetPreviousSlot(slot);
                        var nextSlot = GetNextSlot(slot);

                        var prevSubject = prevSlot?.SubjectId;
                        var nextSubject = nextSlot?.SubjectId;

                        // Skip filler if adjacent to forbidden subjects
                        if ((prevSubject != null && ViolatesAdjacency(prevSubject.Value, filler.ActivityID)) ||
                            (nextSubject != null && ViolatesAdjacency(nextSubject.Value, filler.ActivityID)))
                        {
                            fillerQueue.Enqueue(filler);
                            attempts++;
                            continue;
                        }

                        // Optional: check time rules for fillers if defined
                        if (_timeRulesBySubject.TryGetValue(filler.ActivityID, out var timeRule))
                        {
                            if (timeRule.MustBeMorning && slot.Slot.StartTime.Value.Hours >= 12)
                            {
                                fillerQueue.Enqueue(filler);
                                attempts++;
                                continue;
                            }
                            if (timeRule.MustBeAfternoon && slot.Slot.StartTime.Value.Hours < 12)
                            {
                                fillerQueue.Enqueue(filler);
                                attempts++;
                                continue;
                            }
                        }

                        // Assign the filler
                        AssignFillerToSlot(filler, slot);
                        assigned = true;

                        // Rotate filler queue
                        fillerQueue.Enqueue(filler);
                    }
                }
            }

            return GenerationResult.Ok();
        }

        // --- Helper: Track assigned days ---
        private void MarkTaskAssigned(PlacementTask task, DayOfWeek day, Dictionary<Guid, HashSet<DayOfWeek>> assignedDays)
        {
            if (!assignedDays.ContainsKey(task.SubjectId))
                assignedDays[task.SubjectId] = new HashSet<DayOfWeek>();

            assignedDays[task.SubjectId].Add(day);
        }

        // --- Helper: Single-slot assignment check ---
        private bool TryAssignToSlot(Guid subjectId, SlotState slot, out string reason)
        {
            reason = "";

            if (slot.SubjectId != null || slot.ScheduledActivityId != Guid.Empty)
            {
                reason = "Slot already occupied";
                return false;
            }

            if (_timeRulesBySubject.TryGetValue(subjectId, out var timeRule))
            {
                var start = slot.Slot.StartTime!.Value;

                if (timeRule.MustBeEarlyMorning &&
                    timeRule.EarlyMorningEnd.HasValue &&
                    start >= timeRule.EarlyMorningEnd.Value)
                {
                    reason = "Must be early morning";
                    return false;
                }

                if (timeRule.MustBeMorning &&
                    timeRule.MorningEnd.HasValue &&
                    start >= timeRule.MorningEnd.Value)
                {
                    reason = "Must be morning";
                    return false;
                }

                if (timeRule.MustBeAfternoon &&
                    start < timeRule.MorningEnd)
                {
                    reason = "Must be afternoon";
                    return false;
                }
            }


            var prevSubject = GetPreviousSlot(slot)?.SubjectId;
            var nextSubject = GetNextSlot(slot)?.SubjectId;

            if ((prevSubject != null && ViolatesAdjacency(subjectId, prevSubject.Value)) ||
                (nextSubject != null && ViolatesAdjacency(subjectId, nextSubject.Value)))
            {
                reason = "Violates adjacency";
                return false;
            }

            return true;
        }

        // --- Helper: Double-slot assignment check ---
        private bool TryAssignToSlot(Guid subjectId, SlotState first, SlotState second, out string reason)
        {
            reason = "";

            if (first.SubjectId != null || second.SubjectId != null)
            {
                reason = "One of the double slots is occupied";
                return false;
            }

            // Time rules must pass for BOTH slots
            if (!TryAssignToSlot(subjectId, first, out reason))
                return false;

            if (_timeRulesBySubject.TryGetValue(subjectId, out var rule))
            {
                var start2 = second.Slot.StartTime!.Value;

                if (rule.MustBeEarlyMorning &&
                    rule.EarlyMorningEnd.HasValue &&
                    start2 >= rule.EarlyMorningEnd.Value)
                {
                    reason = "Second slot violates early morning rule";
                    return false;
                }

                if (rule.MustBeMorning &&
                    rule.MorningEnd.HasValue &&
                    start2 >= rule.MorningEnd.Value)
                {
                    reason = "Second slot violates morning rule";
                    return false;
                }
            }

            // Check adjacency OUTSIDE the double only
            var prev = GetPreviousSlot(first)?.SubjectId;
            var next = GetNextSlot(second)?.SubjectId;

            if ((prev != null && ViolatesAdjacency(subjectId, prev.Value)) ||
                (next != null && ViolatesAdjacency(subjectId, next.Value)))
            {
                reason = "Double violates adjacency with neighbors";
                return false;
            }

            return true;
        }


        // --- Helper: Assign filler activity to slot ---
        private void AssignFillerToSlot(TimeTableActivity activity, SlotState slot)
        {
            slot.ScheduledActivityId = activity.ActivityID;
            slot.IsFiller = true;
        }

        private void AssignSubjectToSlot(Guid subjectId, SlotState slot, bool isFiller)
        {
            slot.SubjectId = subjectId;
            slot.IsFiller = isFiller;

            // Add to ClassSchedules if not already
            slot.Slot.ClassSchedules.Clear();
            slot.Slot.ClassSchedules.Add(new ClassSchedule
            {
                ClassID = subjectId,
                IsActive = true,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(6)
            });
        }

        private bool ViolatesAdjacency(Guid subjectA, Guid subjectB)
        {
            if (_adjacencyBySubject.TryGetValue(subjectA, out var ruleA) &&
                ruleA.CannotFollowSubjects.Contains(subjectB)) return true;
            if (_adjacencyBySubject.TryGetValue(subjectB, out var ruleB) &&
                ruleB.CannotFollowSubjects.Contains(subjectA)) return true;
            return false;
        }

        private SlotState? GetPreviousSlot(SlotState slot)
        {
            var daySlots = _slotsByDay[slot.Slot.Day];
            var index = daySlots.IndexOf(slot);
            return index > 0 ? daySlots[index - 1] : null;
        }

        private SlotState? GetNextSlot(SlotState slot)
        {
            var daySlots = _slotsByDay[slot.Slot.Day];
            var index = daySlots.IndexOf(slot);
            return index >= 0 && index < daySlots.Count - 1
                ? daySlots[index + 1]
                : null;
        }


        public void PrintTimetable()
        {
            foreach (var day in _slotsByDay.Keys)
            {
                Console.WriteLine($"\n--- {day} ---");
                foreach (var slot in _slotsByDay[day])
                {
                    string subjectName = slot.SubjectId.HasValue ? GetSubjectName(slot.SubjectId.Value) :
                                         slot.ScheduledActivityId.HasValue ? $"Activity({slot.ScheduledActivityId})" : "Free";
                    string fillerMark = slot.IsFiller ? "(Filler)" : "";
                    Console.WriteLine($"{slot.Slot.StartTime:hh\\:mm} - {slot.Slot.EndTime:hh\\:mm} : {subjectName} {fillerMark}");
                }
            }
        }

        private string GetSubjectName(Guid subjectId)
        {
            return _schedules.FirstOrDefault(s => s.SubjectId == subjectId)?.SubjectName ?? "Unknown";
        }
        #endregion
    }
}
