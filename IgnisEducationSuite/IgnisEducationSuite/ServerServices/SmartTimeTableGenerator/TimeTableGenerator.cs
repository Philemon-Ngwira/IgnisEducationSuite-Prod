using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using System;
using System.Linq;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGenerator : ITimetableGenerator
    {
        private Dictionary<DayOfWeek, List<SlotState>> _slotsByDay = new();
        private Dictionary<Guid, Dictionary<DayOfWeek, int>> _teacherDailyLoad = new();
        private Dictionary<Guid, TeacherScheduleConstraints> _teacherConstraints = new();
        private Dictionary<Guid, SubjectAdjacencyConstraints> _adjacencyBySubject = new();
        private Dictionary<Guid, SubjectTimeConstraints> _timeRulesBySubject = new();
        private List<SubjectScheduleConfig> _schedules;
        private List<TimeTableActivity> _activities = new();
        private List<TimeSlot> _allSlots = new();
        private Dictionary<Guid, SubjectStructureConstraints> _structureBySubject = new();

        public GenerationResult Generate(
            IReadOnlyList<TimeSlot> timeSlots,
            IReadOnlyList<SubjectScheduleConfig> schedules,
            IReadOnlyList<SubjectAdjacencyConstraints> adjacencyRules,
            IReadOnlyList<SubjectTimeConstraints> timeRules,
            IReadOnlyList<TimeTableActivity> activities,
            IReadOnlyList<TeacherScheduleConstraints> teacherConstraints,
            IReadOnlyList<SubjectStructureConstraints> structureConstraints)
        {
            _structureBySubject = structureConstraints
    .ToDictionary(s => s.SubjectId);

            _activities = activities.ToList();
            _schedules = schedules.ToList();
            _allSlots = timeSlots.ToList();
            _teacherConstraints = teacherConstraints.ToDictionary(tc => tc.TeacherId);
            _teacherDailyLoad = _teacherConstraints.ToDictionary(
                t => t.Key,
                t => Enum.GetValues<DayOfWeek>().ToDictionary(d => d, d => 0)
            );

            BuildSlotState(timeSlots);
            ReserveActivitySlots();  // <-- reserve activity periods safely     // <-- reserve activity periods
            BuildRuleLookups(adjacencyRules, timeRules);

            var tasks = BuildPlacementTasks(schedules);
            var result = PlaceTasks(tasks);
            PrintTimetable();
            return result;

        }

        #region Build Methods
        private void BuildSlotState(IReadOnlyList<TimeSlot> timeSlots)
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (!_slotsByDay.ContainsKey(day))
                    _slotsByDay[day] = new List<SlotState>();

                foreach (var slot in timeSlots)
                {
                    _slotsByDay[day].Add(new SlotState
                    {
                        Slot = new TimeSlot
                        {
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            ScheduledActivityId = slot.ScheduledActivityId,
                            Day = day,
                            SchoolMorningEnd = slot.SchoolMorningEnd,
                            SchoolAfternoonStart = slot.SchoolAfternoonStart
                        },
                        SubjectId = null,
                        ScheduledActivityId = slot.ScheduledActivityId ?? Guid.Empty,
                        IsLocked = slot.ScheduledActivityId != null
                    });
                }

                _slotsByDay[day] = _slotsByDay[day].OrderBy(s => s.Slot.StartTime).ToList();
            }

            // Lock weekend slots
            foreach (var weekendDay in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
            {
                if (_slotsByDay.TryGetValue(weekendDay, out var weekendSlots))
                {
                    foreach (var slot in weekendSlots)
                    {
                        slot.IsLocked = true;
                        slot.SubjectId = null;
                        slot.ScheduledActivityId = Guid.Empty;
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
                var structure = _structureBySubject[schedule.SubjectId];

                // 1️⃣ REQUIRED doubles (HARD)
                for (int i = 0; i < structure.RequiredDoublePeriods; i++)
                {
                    tasks.Add(new PlacementTask
                    {
                        SubjectId = schedule.SubjectId,
                        IsDouble = true,
                        IsRequired = true
                    });
                }

                // 2️⃣ OPTIONAL doubles (SOFT)
                int optionalDoubles =
                    schedule.DoublePeriods - structure.RequiredDoublePeriods;

                for (int i = 0; i < Math.Max(0, optionalDoubles); i++)
                {
                    tasks.Add(new PlacementTask
                    {
                        SubjectId = schedule.SubjectId,
                        IsDouble = true,
                        IsRequired = false
                    });
                }

                // 3️⃣ SINGLES
                int singles = Math.Max(
       0,
       schedule.WeeklyPeriods - (schedule.DoublePeriods * 2)
   );


                for (int i = 0; i < singles; i++)
                {
                    tasks.Add(new PlacementTask
                    {
                        SubjectId = schedule.SubjectId,
                        IsDouble = false,
                        IsRequired = false
                    });
                }
            }

            // 🔥 CRITICAL SORT ORDER
            tasks.Sort((a, b) =>
            {
                int cmp = b.IsRequired.CompareTo(a.IsRequired); // required first
                if (cmp != 0) return cmp;

                cmp = b.IsDouble.CompareTo(a.IsDouble); // doubles before singles
                if (cmp != 0) return cmp;

                return 0;
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
            var requiredDoubleDays = new Dictionary<Guid, HashSet<DayOfWeek>>();

            var days = _slotsByDay.Keys
                .Where(d => d != DayOfWeek.Saturday && d != DayOfWeek.Sunday)
                .ToList();

            var assignedDays = new Dictionary<Guid, HashSet<DayOfWeek>>();
            var dayLoad = days.ToDictionary(d => d, d => 0);

            // Dynamically detect core subjects
            var coreSubjects = new HashSet<Guid>(
                _schedules.Where(s => s.IsCoreSubject)
                          .Select(s => s.SubjectId)
            );

            // STEP 1: PLACE ALL SUBJECT TASKS (DOUBLES FIRST)
            foreach (var task in tasks)
            {
                bool placed = false;

                var candidateDays = days.OrderBy(d => dayLoad[d]).ToList();

                foreach (var day in candidateDays)
                {
                    if (task.IsRequired && task.IsDouble)
                    {
                        if (requiredDoubleDays.TryGetValue(task.SubjectId, out var usedDays) &&
                            usedDays.Contains(day))
                        {
                            continue; // ❌ don't stack required doubles on same day
                        }
                    }


                    var daySlots = _slotsByDay[day];
                    SlotState bestSlot = null;
                    int bestScore = int.MinValue;

                    for (int i = 0; i < daySlots.Count; i++)
                    {
                        var slot = daySlots[i];

                        if (slot.IsLocked || slot.SubjectId != null || slot.ScheduledActivityId != Guid.Empty || slot.ReservedForActivity)
                            continue;

                        if (!task.IsDouble)
                        {
                            if (!TryAssignToSlot(task.SubjectId, slot, out _)) continue;

                            int score = EvaluateSlot(
     slot,
     task.SubjectId,
     coreSubjects,
     task.IsRequired && task.IsDouble
 );

                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSlot = slot;
                            }
                        }
                        else
                        {
                            if (i >= daySlots.Count - 1) continue;
                            var next = daySlots[i + 1];

                            if (!TryAssignToSlot(task.SubjectId, slot, next, out _)) continue;

                            int score =
      EvaluateSlot(slot, task.SubjectId, coreSubjects, task.IsRequired) +
      EvaluateSlot(next, task.SubjectId, coreSubjects, task.IsRequired);


                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSlot = slot;
                            }
                        }
                    }
                    if (!task.IsRequired)
                    {
                        if (assignedDays.TryGetValue(task.SubjectId, out var used) &&
                            used.Contains(day))
                        {
                            continue;
                        }
                    }

                    if (bestSlot != null)
                    {
                        if (task.IsDouble)
                        {
                            var next = GetNextSlot(bestSlot);
                            AssignSubjectToSlot(task.SubjectId, bestSlot, false);
                            AssignSubjectToSlot(task.SubjectId, next, false);
                            dayLoad[day] += 2;
                        }
                        else
                        {
                            AssignSubjectToSlot(task.SubjectId, bestSlot, false);
                            dayLoad[day]++;
                        }

                        MarkTaskAssigned(task, day, assignedDays);

                        if (task.IsRequired && task.IsDouble)
                        {
                            if (!requiredDoubleDays.ContainsKey(task.SubjectId))
                                requiredDoubleDays[task.SubjectId] = new HashSet<DayOfWeek>();

                            requiredDoubleDays[task.SubjectId].Add(day);
                        }

                        placed = true;
                        break;

                    }
                }

                if (!placed)
                {
                    if (task.IsRequired && task.IsDouble)
                    {
                        return GenerationResult.Failed(
                            $"Required double could not be placed for {GetSubjectName(task.SubjectId)}");
                    }

                    return GenerationResult.Failed(
                        $"Could not place subject {GetSubjectName(task.SubjectId)}");
                }

            }
            var allSlots = _slotsByDay
     .Where(k => k.Key != DayOfWeek.Saturday && k.Key != DayOfWeek.Sunday)
     .SelectMany(k => k.Value)
     .Where(s => s.SubjectId == null) // only filter unassigned subjects
     .OrderBy(s => s.Slot.Day)
     .ThenBy(s => s.Slot.StartTime)
     .ToList();

            var remainingPeriods = _schedules.ToDictionary(
                s => s.SubjectId,
                s => s.WeeklyPeriods -
                     _slotsByDay.SelectMany(d => d.Value)
                                .Count(sl => sl.SubjectId == s.SubjectId)
            );


            // STEP 2: BACKFILL MISSING PERIODS
            foreach (var day in days)
            {
                foreach (var slot in _slotsByDay[day].OrderBy(s => s.Slot.StartTime))
                {
                  

                    if (slot.SubjectId != null || slot.ReservedForActivity || slot.IsLocked)
                        continue;

                    foreach (var subjectId in remainingPeriods.Where(p => p.Value > 0).Select(p => p.Key))
                    {
                        if (!TryAssignToSlot(subjectId, slot, out var reason))
                        {
                            Console.WriteLine($"Slot {slot.Slot.StartTime} skipped for {GetSubjectName(subjectId)}: {reason}");
                            continue;
                        }

                        AssignSubjectToSlot(subjectId, slot, isFiller: true);
                        remainingPeriods[subjectId]--;
                        break; // move to next slot
                    }
                }
            }



            // STEP 3: PLACE ACTIVITIES
            var activityQueue = new Queue<TimeTableActivity>(_activities);

            foreach (var day in _slotsByDay.Keys.Where(d => d != DayOfWeek.Saturday && d != DayOfWeek.Sunday))
            {
                var freeSlots = _slotsByDay[day]
                    .Where(s => !s.IsLocked && s.SubjectId == null && s.ScheduledActivityId == Guid.Empty)
                    .ToList();

                foreach (var slot in freeSlots)
                {
                    var attempts = 0;
                    var maxAttempts = activityQueue.Count;
                    bool assigned = false;

                    while (!assigned && attempts < maxAttempts)
                    {
                        var activity = activityQueue.Dequeue();

                        if ((activity.MustBeMorning && !slot.IsMorning()) ||
                            (activity.MustBeAfternoon && !slot.IsAfternoon()))
                        {
                            activityQueue.Enqueue(activity);
                            attempts++;
                            continue;
                        }

                        AssignFillerToSlot(activity, slot);
                        assigned = true;
                        activityQueue.Enqueue(activity); // requeue
                    }
                }
            }

            // STEP 4: FLATTEN RESULT
            var flatSlots = _slotsByDay
                .SelectMany(kvp => kvp.Value.Select(slot => new GeneratedSlotPreview
                {
                    DayOfWeek = kvp.Key.ToString(),
                    SubjectId = slot.SubjectId,
                    ScheduledActivityId = slot.ScheduledActivityId,
                    SubjectName = slot.SubjectId.HasValue ? GetSubjectName(slot.SubjectId.Value) : null,
                    ActivityName = slot.IsFiller
                        ? _activities.FirstOrDefault(a => a.ActivityID == slot.ScheduledActivityId)?.ActivityName
                        : null,
                    TeacherName = "",
                    Slot = slot.Slot
                }))
                .ToList();
            // ✅ HARD VALIDATION (GOES HERE)
            foreach (var constraint in _structureBySubject.Values)
            {
                int placed = _slotsByDay
                    .SelectMany(d => d.Value)
                    .Count(s => s.SubjectId == constraint.SubjectId);

                if (placed != constraint.WeeklyPeriods)
                {
                    return GenerationResult.Failed(
                        $"{GetSubjectName(constraint.SubjectId)} has {placed}, expected {constraint.WeeklyPeriods}");
                }
            }

            return GenerationResult.Ok(flatSlots);
        }
     

        #endregion

        #region Slot Assignment Helpers
        private void MarkTaskAssigned(PlacementTask task, DayOfWeek day, Dictionary<Guid, HashSet<DayOfWeek>> assignedDays)
        {
            if (!assignedDays.ContainsKey(task.SubjectId)) assignedDays[task.SubjectId] = new HashSet<DayOfWeek>();
            assignedDays[task.SubjectId].Add(day);
        }

        private bool TryAssignToSlot(Guid subjectId, SlotState slot, out string reason)
        {
            reason = "";
            var subjectName = GetSubjectName(subjectId);
            Console.WriteLine(
     $"[TRY] {GetSubjectName(subjectId)} " +
     $"@ {slot.Slot.Day} {slot.Slot.StartTime:hh\\:mm}"
 );

            if (slot.IsLocked)
            {
                reason = "Slot locked";
                Console.WriteLine($" -> Skipped: {reason}");
                return false;
            }
            if (slot.SubjectId != null)
            {
                reason = "Already assigned";
                Console.WriteLine($" -> Skipped: {reason}");
                return false;
            }
            if (slot.ScheduledActivityId != Guid.Empty)
            {
                reason = "Activity scheduled";
                Console.WriteLine($" -> Skipped: {reason}");
                return false;
            }
            // ✅ THIS IS THE RIGHT PLACE
            if (slot.ReservedForActivity)
            {
                reason = "Reserved for activity";
                return false;
            }
            var subject = _schedules.First(s => s.SubjectId == subjectId);
            var teacherId = subject.TeacherId;

            if (teacherId != Guid.Empty && _teacherConstraints.TryGetValue(teacherId, out var constraints))
            {
                if (constraints.UnavailableSlots.Any(u => u.Day == slot.Slot.Day && u.StartTime == slot.Slot.StartTime))
                {
                    reason = "Teacher unavailable";
                    Console.WriteLine($" -> Skipped: {reason}");
                    return false;
                }
            }

            var prevSubject = GetPreviousSlot(slot)?.SubjectId;
            var nextSubject = GetNextSlot(slot)?.SubjectId;

            if (_timeRulesBySubject.TryGetValue(subjectId, out var timeRule))
            {
                var start = slot.Slot.StartTime!.Value;

                if (!SlotMatchesTimeRule(
    slot.Slot.StartTime!.Value,
    slot.Slot.EndTime!.Value,
    timeRule.MustBeEarlyMorning,
    timeRule.EarlyMorningEnd,
    timeRule.MustBeMorning,
    slot.Slot.SchoolMorningEnd,
    timeRule.MustBeAfternoon,
    slot.Slot.SchoolAfternoonStart)
)
                {
                    reason = "Violates time rule";
                    Console.WriteLine($" -> Skipped: {reason}");
                    return false;
                }
            }

            if ((prevSubject != null && ViolatesAdjacency(subjectId, prevSubject.Value)) ||
                (nextSubject != null && ViolatesAdjacency(subjectId, nextSubject.Value)))
            {
                reason = "Adjacency violation";
                Console.WriteLine($" -> Skipped: {reason}");
                return false;
            }

            Console.WriteLine($" -> Slot valid for {subjectName}");
            return true;
        }

        private bool TryAssignToSlot(Guid subjectId, SlotState first, SlotState second, out string reason)
        {
            reason = "";
            if (!TryAssignToSlot(subjectId, first, out reason)) return false;
            if (!TryAssignToSlot(subjectId, second, out reason)) return false;
            if (GetNextSlot(first) != second) { reason = "Slots not consecutive"; return false; }
            return true;
        }

        private void AssignSubjectToSlot(Guid subjectId, SlotState slot, bool isFiller)
        {
            slot.SubjectId = subjectId;
            slot.IsFiller = isFiller;
        }

        private void AssignFillerToSlot(TimeTableActivity activity, SlotState slot)
        {
            slot.ScheduledActivityId = activity.ActivityID;
            slot.IsFiller = true;
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
            return index >= 0 && index < daySlots.Count - 1 ? daySlots[index + 1] : null;
        }

        private bool ViolatesAdjacency(Guid subjectId, Guid adjacentSubjectId)
        {
            if (_structureBySubject.TryGetValue(subjectId, out var s1) &&
                s1.CannotFollowSubjects.Contains(adjacentSubjectId))
                return true;

            if (_structureBySubject.TryGetValue(adjacentSubjectId, out var s2) &&
                s2.CannotFollowSubjects.Contains(subjectId))
                return true;

            return false;
        }

        #endregion
        private void PrintTimetable()
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
                            : "Free");

                    string fillerMark = slot.IsFiller ? "(Filler)" : "";
                    Console.WriteLine($"{slot.Slot.StartTime:hh\\:mm} - {slot.Slot.EndTime:hh\\:mm} : {subjectName} {fillerMark}");
                }
            }
        }

        private string GetSubjectName(Guid subjectId)
        {
            return _schedules.FirstOrDefault(s => s.SubjectId == subjectId)?.SubjectName ?? "Unknown";
        }

        #region Scoring
        // Update EvaluateSlot to consider coreSubjects
        private int EvaluateSlot(
     SlotState slot,
     Guid subjectId,
     HashSet<Guid> coreSubjects,
     bool isRequiredDouble = false)
        {
            int score = 0;

            if (isRequiredDouble)
            {
                score += 25; // 🔥 strong priority
            }


            if (_timeRulesBySubject.TryGetValue(subjectId, out var timeRule))
            {
                var start = slot.Slot.StartTime!.Value;

                if (timeRule.MustBeEarlyMorning && timeRule.EarlyMorningEnd.HasValue)
                    score += start < timeRule.EarlyMorningEnd.Value ? 20 : -50;

                if (timeRule.MustBeMorning && slot.Slot.SchoolMorningEnd != default)
                    score += start < slot.Slot.SchoolMorningEnd ? 10 : -30;

                if (timeRule.MustBeAfternoon && slot.Slot.SchoolAfternoonStart != default)
                    score += start >= slot.Slot.SchoolAfternoonStart ? 10 : -30;
            }
            else
            {
                score += 5;
            }

            var prev = GetPreviousSlot(slot);
            var next = GetNextSlot(slot);

            // Penalize adjacency only for non-core subjects
            if (prev?.SubjectId != null && !coreSubjects.Contains(prev.SubjectId.Value) && ViolatesAdjacency(subjectId, prev.SubjectId.Value))
                score -= 20;
            if (next?.SubjectId != null && !coreSubjects.Contains(next.SubjectId.Value) && ViolatesAdjacency(subjectId, next.SubjectId.Value))
                score -= 20;

            // Penalize consecutive same subject (unless double)
            if (prev?.SubjectId == subjectId) score -= 5;

            return score;
        }
        #endregion
        private void ReserveActivitySlots()
        {
            foreach (var day in _slotsByDay.Keys)
            {
                foreach (var slot in _slotsByDay[day])
                {
                    if (_activities.Any(a =>
                            (a.MustBeAfternoon && slot.IsAfternoon()) ||
                            (a.MustBeMorning && slot.IsMorning())))
                    {
                        slot.ReservedForActivity = true;
                    }
                }
            }
        }

        private bool SlotMatchesTimeRule(
         TimeSpan start,
         TimeSpan end,
         bool mustBeEarlyMorning,
         TimeSpan? earlyMorningEnd,
         bool mustBeMorning,
         TimeSpan? schoolMorningEnd,
         bool mustBeAfternoon,
         TimeSpan? schoolAfternoonStart)
        {
            // Early morning: slot must END before earlyMorningEnd
            if (mustBeEarlyMorning && earlyMorningEnd.HasValue && end > earlyMorningEnd.Value)
                return false;

            // Morning: slot must END at or before schoolMorningEnd
            if (mustBeMorning && schoolMorningEnd.HasValue && end > schoolMorningEnd.Value)
                return false;

            // Afternoon: slot must START at or after schoolAfternoonStart
            if (mustBeAfternoon && schoolAfternoonStart.HasValue && start < schoolAfternoonStart.Value)
                return false;

            return true;

        }

    }


}
