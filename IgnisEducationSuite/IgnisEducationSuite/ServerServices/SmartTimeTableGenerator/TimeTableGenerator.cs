using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGenerator : ITimetableGenerator
    {
        private Dictionary<DayOfWeek, List<SlotState>> _slotsByDay = new();
        private Dictionary<Guid, Dictionary<DayOfWeek, int>> _teacherDailyLoad = new();
        // Add this at the top of the class
        private Dictionary<Guid, TeacherScheduleConstraints> _teacherConstraints = new();

        private Dictionary<Guid, SubjectAdjacencyConstraints> _adjacencyBySubject = new();
        private Dictionary<Guid, SubjectTimeConstraints> _timeRulesBySubject = new();
        private List<SubjectScheduleConfig> _schedules;
        private List<TimeTableActivity> _activities = new();

        public GenerationResult Generate(
    IReadOnlyList<TimeSlot> timeSlots,
    IReadOnlyList<SubjectScheduleConfig> schedules,
    IReadOnlyList<SubjectAdjacencyConstraints> adjacencyRules,
    IReadOnlyList<SubjectTimeConstraints> timeRules,
    IReadOnlyList<TimeTableActivity> activities,
    IReadOnlyList<TeacherScheduleConstraints> teacherConstraints)   // NEW
        {
            _activities = activities.ToList();
            _schedules = schedules.ToList();
            _slotsByDay = new Dictionary<DayOfWeek, List<SlotState>>();
            _adjacencyBySubject = new Dictionary<Guid, SubjectAdjacencyConstraints>();
            _timeRulesBySubject = new Dictionary<Guid, SubjectTimeConstraints>();
            _teacherConstraints = teacherConstraints.ToDictionary(tc => tc.TeacherId);
            _teacherDailyLoad = _teacherConstraints.ToDictionary(
    t => t.Key,
    t => Enum.GetValues<DayOfWeek>().ToDictionary(d => d, d => 0)
);

            BuildSlotState(timeSlots);
            BuildRuleLookups(adjacencyRules, timeRules);

            var tasks = BuildPlacementTasks(schedules);

            var result = PlaceTasks(tasks);
            PrintTimetable();
            return result;
        }


        #region Build Methods

        private void BuildSlotState(IReadOnlyList<TimeSlot> timeSlots)
        {
            // For every day of the week, create a copy of each slot
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (!_slotsByDay.ContainsKey(day))
                    _slotsByDay[day] = new List<SlotState>();

                foreach (var slot in timeSlots)
                {
                    // Make a copy per day
                    _slotsByDay[day].Add(new SlotState
                    {
                        Slot = new TimeSlot
                        {
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            ScheduledActivityId = slot.ScheduledActivityId,
                            Day = day,
                        },
                        SubjectId = null,
                        ScheduledActivityId = slot.ScheduledActivityId ?? Guid.Empty,
                        IsLocked = slot.ScheduledActivityId != null
                    });
                }

                // Sort slots for the day
                _slotsByDay[day] = _slotsByDay[day].OrderBy(s => s.Slot.StartTime).ToList();
            }

            // --- Lock Saturday and Sunday slots AFTER building all slots ---
            foreach (var weekendDay in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
            {
                if (_slotsByDay.TryGetValue(weekendDay, out var weekendSlots))
                {
                    foreach (var slot in weekendSlots)
                    {
                        slot.IsLocked = true;          // Prevent assignment
                        slot.SubjectId = null;         // Ensure no subject
                        slot.ScheduledActivityId = Guid.Empty; // Ensure no activity
                    }
                }
            }
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

        // --- Placement modified to use scoring ---
        private GenerationResult PlaceTasks(List<PlacementTask> tasks)
        {
            var days = _slotsByDay.Keys.ToList();
            var assignedDays = new Dictionary<Guid, HashSet<DayOfWeek>>();
            var dayLoad = days.ToDictionary(d => d, d => 0);

            var fillerQueue = new Queue<TimeTableActivity>(_activities);

            foreach (var task in tasks)
            {
                bool placed = false;
                var failureReasons = new List<string>();

                // Try days with least load first
                var candidateDays = days.OrderBy(d => dayLoad[d]).ToList();

                foreach (var day in candidateDays)
                {
                    if (assignedDays.TryGetValue(task.SubjectId, out var usedDays) && usedDays.Contains(day))
                        continue;

                    var daySlots = _slotsByDay[day];
                    SlotState bestSlot = null;
                    int bestScore = int.MinValue;

                    for (int i = 0; i < daySlots.Count; i++)
                    {
                        var slot = daySlots[i];

                        if (slot.SubjectId != null || slot.ScheduledActivityId != Guid.Empty)
                            continue;

                        // Single-slot
                        if (!task.IsDouble)
                        {
                            if (!TryAssignToSlot(task.SubjectId, slot, out string reason))
                            {
                                failureReasons.Add($"Day {day}, {slot.Slot.StartTime:hh\\:mm}-{slot.Slot.EndTime:hh\\:mm}: {reason}");
                                continue;
                            }

                            int score = EvaluateSlot(slot, task.SubjectId);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSlot = slot;
                            }
                        }
                        else // Double-slot
                        {
                            if (i >= daySlots.Count - 1) continue;

                            var nextSlot = daySlots[i + 1];
                            if (!TryAssignToSlot(task.SubjectId, slot, nextSlot, out string reasonDouble))
                            {
                                failureReasons.Add($"Day {day}, {slot.Slot.StartTime:hh\\:mm}-{nextSlot.Slot.EndTime:hh\\:mm}: {reasonDouble}");
                                continue;
                            }

                            int score = EvaluateSlot(slot, task.SubjectId) + EvaluateSlot(nextSlot, task.SubjectId);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSlot = slot;
                            }
                        }
                    }

                    if (bestSlot != null)
                    {
                        if (task.IsDouble)
                        {
                            var nextSlot = GetNextSlot(bestSlot);
                            AssignSubjectToSlot(task.SubjectId, bestSlot, false);
                            AssignSubjectToSlot(task.SubjectId, nextSlot, false);
                            dayLoad[day] += 2;
                        }
                        else
                        {
                            AssignSubjectToSlot(task.SubjectId, bestSlot, false);
                            dayLoad[day]++;
                        }

                        MarkTaskAssigned(task, day, assignedDays);
                        placed = true;
                        break; // Subject placed successfully
                    }
                }

                if (!placed)
                {
                    return GenerationResult.Failed(
                        $"Could not place subject {GetSubjectName(task.SubjectId)}. Reasons:\n" +
                        string.Join("\n", failureReasons)
                    );
                }
            }

            /// --- Fill remaining slots with filler activities ---
            foreach (var day in _slotsByDay.Keys)
            {
                var freeSlots = _slotsByDay[day]
                    .Where(s => !s.IsLocked && s.SubjectId == null && s.ScheduledActivityId == Guid.Empty)
                    .ToList(); // <-- skip locked slots

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

                        if ((prevSubject != null && ViolatesAdjacency(prevSubject.Value, filler.ActivityID)) ||
                            (nextSubject != null && ViolatesAdjacency(nextSubject.Value, filler.ActivityID)))
                        {
                            fillerQueue.Enqueue(filler);
                            attempts++;
                            continue;
                        }

                        if (_timeRulesBySubject.TryGetValue(filler.ActivityID, out var timeRule))
                        {
                            var startHour = slot.Slot.StartTime?.Hours ?? 0;
                            if ((timeRule.MustBeMorning && startHour >= 12) ||
                                (timeRule.MustBeAfternoon && startHour < 12))
                            {
                                fillerQueue.Enqueue(filler);
                                attempts++;
                                continue;
                            }
                        }

                        AssignFillerToSlot(filler, slot);
                        assigned = true;
                        fillerQueue.Enqueue(filler);
                    }
                }
            }
            var flatSlots = _slotsByDay
     .SelectMany(kvp => kvp.Value.Select(slot => new GeneratedSlotPreview
     {
         DayOfWeek = kvp.Key.ToString(),
         SubjectId = slot.SubjectId,
         ScheduledActivityId = slot.ScheduledActivityId,
         SubjectName = slot.SubjectId.HasValue ? GetSubjectName(slot.SubjectId.Value) : null,
         ActivityName = slot.IsFiller ? _activities.FirstOrDefault(a => a.ActivityID == slot.ScheduledActivityId)?.ActivityName : null,
         TeacherName = "",
         Slot = slot.Slot
     }))
     .ToList();

            return GenerationResult.Ok(flatSlots);
        }

        // --- Helper: Track assigned days ---
        private void MarkTaskAssigned(PlacementTask task, DayOfWeek day, Dictionary<Guid, HashSet<DayOfWeek>> assignedDays)
        {
            if (!assignedDays.ContainsKey(task.SubjectId))
                assignedDays[task.SubjectId] = new HashSet<DayOfWeek>();

            assignedDays[task.SubjectId].Add(day);
        }

        // --- Helper: Single-slot assignment check ---
        // --- Single-slot assignment with teacher constraints ---
        // Single-slot version
        private bool TryAssignToSlot(Guid subjectId, SlotState slot, out string reason)
        {
            reason = "";

            // Respect locked slots (e.g., weekends)
            if (slot.IsLocked)
            {
                reason = "Slot is locked";
                return false;
            }

            var subject = _schedules.First(s => s.SubjectId == subjectId);
            var teacherId = subject.TeacherId;

            // Slot already occupied
            if (slot.SubjectId != null || slot.ScheduledActivityId != Guid.Empty)
            {
                reason = "Slot already occupied";
                return false;
            }

            // Only check teacher constraints if teacher exists and has constraints
            if (teacherId != Guid.Empty && _teacherConstraints.TryGetValue(teacherId, out var constraints))
            {
                // Teacher unavailable
                if (constraints.UnavailableSlots.Any(u => u.Day == slot.Slot.Day && u.StartTime == slot.Slot.StartTime))
                {
                    reason = "Teacher unavailable";
                    return false;
                }

                // Teacher max daily periods
                if (!_teacherDailyLoad.ContainsKey(teacherId))
                    _teacherDailyLoad[teacherId] = Enum.GetValues<DayOfWeek>().ToDictionary(d => d, d => 0);

                if (_teacherDailyLoad[teacherId][slot.Slot.Day] >= constraints.MaxDailyPeriods)
                {
                    reason = "Teacher max daily periods reached";
                    return false;
                }
            }

            // Time rules
            if (_timeRulesBySubject.TryGetValue(subjectId, out var timeRule))
            {
                var start = slot.Slot.StartTime!.Value;
                if (timeRule.MustBeEarlyMorning && timeRule.EarlyMorningEnd.HasValue && start >= timeRule.EarlyMorningEnd.Value)
                {
                    reason = "Must be early morning";
                    return false;
                }
                if (timeRule.MustBeMorning && timeRule.MorningEnd.HasValue && start >= timeRule.MorningEnd.Value)
                {
                    reason = "Must be morning";
                    return false;
                }
                if (timeRule.MustBeAfternoon && start < timeRule.MorningEnd)
                {
                    reason = "Must be afternoon";
                    return false;
                }
            }

            // Adjacency
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

        // Double-slot version
        private bool TryAssignToSlot(Guid subjectId, SlotState first, SlotState second, out string reason)
        {
            reason = "";

            // Respect locked slots for both slots
            if (first.IsLocked || second.IsLocked)
            {
                reason = "One or both slots are locked";
                return false;
            }

            // Both slots must individually pass single-slot checks
            if (!TryAssignToSlot(subjectId, first, out reason)) return false;
            if (!TryAssignToSlot(subjectId, second, out reason)) return false;

            // Check adjacency for outside neighbors
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

            var subject = _schedules.First(s => s.SubjectId == subjectId);
            var teacherId = subject.TeacherId;

            if (!_teacherDailyLoad.ContainsKey(teacherId))
                _teacherDailyLoad[teacherId] = new Dictionary<DayOfWeek, int>();

            if (!_teacherDailyLoad[teacherId].ContainsKey(slot.Slot.Day))
                _teacherDailyLoad[teacherId][slot.Slot.Day] = 0;

            _teacherDailyLoad[teacherId][slot.Slot.Day]++;

            // --- ClassSchedules (unchanged) ---
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
                    string subjectName = slot.SubjectId.HasValue
                        ? GetSubjectName(slot.SubjectId.Value)
                        : (!slot.IsLocked && slot.ScheduledActivityId != Guid.Empty
                            ? $"Activity({slot.ScheduledActivityId})"
                            : "Free"); // <-- Locked or empty slots show Free

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

        private int EvaluateSlot(SlotState slot, Guid subjectId)
        {
            int score = 0;

            // Preferred morning
            if (_timeRulesBySubject.TryGetValue(subjectId, out var timeRule))
            {
                if (timeRule.MustBeMorning && slot.Slot.StartTime.Value.Hours < 12)
                    score += 10;
                if (timeRule.MustBeAfternoon && slot.Slot.StartTime.Value.Hours >= 12)
                    score += 10;
            }

            // Avoid consecutive same subject
            var prev = GetPreviousSlot(slot);
            if (prev?.SubjectId == subjectId) score -= 5;

            // Avoid adjacency violations (already hard rule)
            if (ViolatesAdjacency(subjectId, prev?.SubjectId ?? Guid.Empty)) score -= 20;

            return score;
        }

    }
}
