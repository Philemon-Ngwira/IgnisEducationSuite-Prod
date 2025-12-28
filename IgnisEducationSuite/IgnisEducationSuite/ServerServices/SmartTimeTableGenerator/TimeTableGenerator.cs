using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGenerator : ITimetableGenerator
    {
        private Dictionary<DayOfWeek, List<TimeSlot>> _slotsByDay;
        private Dictionary<Guid, TeacherScheduleConstraints> _teacherConstraints;
        private Dictionary<Guid, SubjectTimeConstraints> _timeRules;
        private Dictionary<Guid, SubjectAdjacencyConstraints> _adjacencyRules;
        private Dictionary<Guid, SubjectStructureConstraints> _structure;
        private List<SubjectScheduleConfig> _schedules;
        private List<TimeTableActivity> _activities;

        public GenerationResult Generate(
            IReadOnlyList<TimeSlot> timeSlotTemplates,
            IReadOnlyList<SubjectScheduleConfig> schedules,
            IReadOnlyList<SubjectAdjacencyConstraints> adjacencyRules,
            IReadOnlyList<SubjectTimeConstraints> timeRules,
            IReadOnlyList<TimeTableActivity> activities,
            IReadOnlyList<TeacherScheduleConstraints> teacherConstraints,
            IReadOnlyList<SubjectStructureConstraints> structureConstraints)
        {
            _schedules = schedules.ToList();
            _teacherConstraints = teacherConstraints.ToDictionary(tc => tc.TeacherId);
            _structure = structureConstraints.ToDictionary(s => s.SubjectId);
            _timeRules = timeRules.ToDictionary(t => t.SubjectId);
            _adjacencyRules = adjacencyRules.ToDictionary(a => a.SubjectId);
            _activities = activities.ToList();

            // --- EXPAND TEMPLATE SLOTS TO ALL LEARNING DAYS ---
            var learningDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            _slotsByDay = learningDays.ToDictionary(
                day => day,
                day => timeSlotTemplates.Select(ts => new TimeSlot
                {
                    Day = day,
                    StartTime = ts.StartTime,
                    EndTime = ts.EndTime,
                    SlotType = ts.SlotType,
                    SchoolMorningEnd = ts.SchoolMorningEnd,
                    SchoolAfternoonStart = ts.SchoolAfternoonStart,
                    ScheduledActivityId = Guid.Empty
                }).ToList()
            );

            ReserveActivitySlots();

            // --- BUILD TASKS BASED ON SUBJECT STRUCTURE ---
            var tasks = BuildTasks(_schedules);
            var state = new TimetableState(_slotsByDay, _teacherConstraints, _structure, _timeRules, _adjacencyRules, _schedules);

            // --- ORDER TASKS: hardest first (early morning + doubles + few candidates) ---
            tasks = tasks.OrderByDescending(t => t.IsDouble)
                         .ThenByDescending(t => _timeRules.ContainsKey(t.SubjectId) && _timeRules[t.SubjectId].MustBeMorning ? 1 : 0)
                         .ToList();

            bool success = BacktrackAssign(tasks, state, 0);

            if (!success)
                return GenerationResult.Failed("Unable to generate a complete timetable. Check constraints and available slots.");

            var generatedSlots = state.GetGeneratedSlots();

            // --- PRINT TIMETABLE ---
            Console.WriteLine("Generated Timetable:");
            var slotsByDay = generatedSlots.GroupBy(s => s.DayOfWeek)
                                           .OrderBy(g => g.Key);
            foreach (var dayGroup in slotsByDay)
            {
                Console.WriteLine($"\n{dayGroup.Key}:");
                foreach (var slot in dayGroup.OrderBy(s => s.Slot.StartTime))
                {
                    var subjectName = _schedules.FirstOrDefault(s => s.SubjectId == slot.SubjectId)?.SubjectName ?? "Free/Activity";
                    Console.WriteLine($"  {slot.Slot.StartTime:hh\\:mm} - {slot.Slot.EndTime:hh\\:mm}: {subjectName}");
                }
            }

            return GenerationResult.Ok(generatedSlots);
        }

        private void ReserveActivitySlots()
        {
            foreach (var activity in _activities)
            {
                foreach (var daySlots in _slotsByDay)
                {
                    foreach (var slot in daySlots.Value)
                    {
                        if ((activity.MustBeMorning && slot.StartTime < slot.SchoolMorningEnd) ||
                            (activity.MustBeAfternoon && slot.StartTime >= slot.SchoolAfternoonStart))
                        {
                            slot.ScheduledActivityId = activity.ActivityID;
                        }
                    }
                }
            }
        }

        private List<PlacementTask> BuildTasks(List<SubjectScheduleConfig> schedules)
        {
            var tasks = new List<PlacementTask>();
            foreach (var schedule in schedules)
            {
                if (!_structure.TryGetValue(schedule.SubjectId, out var structure))
                    continue;

                // Required doubles first
                for (int i = 0; i < structure.RequiredDoubleCount; i++)
                    tasks.Add(new PlacementTask(schedule.SubjectId, true, true));

                // Optional doubles
                int optionalDoubles = Math.Max(0, schedule.DoublePeriods - structure.RequiredDoubleCount);
                for (int i = 0; i < optionalDoubles; i++)
                    tasks.Add(new PlacementTask(schedule.SubjectId, true, false));

                // Remaining singles
                int singles = Math.Max(0, schedule.WeeklyPeriods - schedule.DoublePeriods * 2);
                for (int i = 0; i < singles; i++)
                    tasks.Add(new PlacementTask(schedule.SubjectId, false, false));
            }
            return tasks;
        }

        private bool BacktrackAssign(List<PlacementTask> tasks, TimetableState state, int index)
        {
            if (index >= tasks.Count) return true;

            var task = tasks[index];
            var candidates = state.GetAvailableSlots(task)
                                  .Select(s => new { Slot = s, Score = state.EvaluateSlot(task, s) })
                                  .OrderByDescending(c => c.Score)
                                  .Select(c => c.Slot)
                                  .ToList();

            if (!candidates.Any())
            {
                Console.WriteLine($"Task {task.SubjectId} has no candidates at index {index}! RemainingPeriods: {state.RemainingPeriods(task.SubjectId)}");
                return false;
            }

            foreach (var slot in candidates)
            {
                if (!state.CanPlaceTaskHere(task, slot)) continue;

                state.PlaceTask(task, slot);

                if (!state.HasEnoughRemainingSlots(task))
                {
                    state.RemoveTask(task, slot);
                    continue;
                }

                if (BacktrackAssign(tasks, state, index + 1))
                    return true;

                state.RemoveTask(task, slot);
            }

            return false;
        }
    }

    public class TimetableState
    {
        private readonly Dictionary<DayOfWeek, List<TimeSlot>> _slots;
        private readonly Dictionary<Guid, TeacherScheduleConstraints> _teacherConstraints;
        private readonly Dictionary<Guid, SubjectStructureConstraints> _structure;
        private readonly Dictionary<Guid, SubjectTimeConstraints> _timeRules;
        private readonly Dictionary<Guid, SubjectAdjacencyConstraints> _adjacencyRules;
        private readonly Dictionary<Guid, SubjectScheduleConfig> _schedulesLookup;
        private readonly Dictionary<TimeSlot, Guid> _assignment;
        private readonly Dictionary<Guid, int> _remainingPeriods;
        private readonly Dictionary<Guid, Dictionary<DayOfWeek, int>> _dailySubjectCount;

        public TimetableState(Dictionary<DayOfWeek, List<TimeSlot>> slots,
                              Dictionary<Guid, TeacherScheduleConstraints> teacherConstraints,
                              Dictionary<Guid, SubjectStructureConstraints> structure,
                              Dictionary<Guid, SubjectTimeConstraints> timeRules,
                              Dictionary<Guid, SubjectAdjacencyConstraints> adjacencyRules,
                              List<SubjectScheduleConfig> schedules)
        {
            _dailySubjectCount = schedules.ToDictionary(
    s => s.SubjectId,
    s => Enum.GetValues(typeof(DayOfWeek))
             .Cast<DayOfWeek>()
             .ToDictionary(d => d, d => 0)
);

            _slots = slots;
            _teacherConstraints = teacherConstraints;
            _structure = structure;
            _timeRules = timeRules;
            _adjacencyRules = adjacencyRules;
            _schedulesLookup = schedules.ToDictionary(s => s.SubjectId);
            _assignment = new Dictionary<TimeSlot, Guid>(); 
            _remainingPeriods = schedules.ToDictionary(
    s => s.SubjectId,
    s => s.WeeklyPeriods
);

        }

        public int RemainingPeriods(Guid subjectId) => _remainingPeriods.TryGetValue(subjectId, out var r) ? r : 0;

        public List<TimeSlot> GetAvailableSlots(PlacementTask task)
        {
            var available = new List<TimeSlot>();
            foreach (var day in _slots.Keys)
            {
                var slots = _slots[day];
                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot.ScheduledActivityId != Guid.Empty) continue;

                    TimeSlot? nextSlot = (task.IsDouble && i < slots.Count - 1) ? slots[i + 1] : null;
                    if (nextSlot != null && nextSlot.ScheduledActivityId != Guid.Empty) continue;

                    if (IsSlotLegal(task, slot, nextSlot))
                        available.Add(slot);
                }
            }
            return available;
        }

        public int EvaluateSlot(PlacementTask task, TimeSlot slot)
        {
            int score = 0;
            if (task.IsRequired) score += 30;
            if (_schedulesLookup[task.SubjectId].IsCoreSubject) score += 10;

            if (_timeRules.TryGetValue(task.SubjectId, out var timeRule))
            {
                if (timeRule.MustBeMorning && slot.StartTime < timeRule.MorningEnd) score += 20;
                if (timeRule.MustBeMorning && slot.StartTime >= timeRule.MorningEnd) score -= 50;
            }

            return score;
        }

        public void PlaceTask(PlacementTask task, TimeSlot slot)
        {
            _assignment[slot] = task.SubjectId;
            _remainingPeriods[task.SubjectId]--;
            if (task.IsDouble)
            {
                int idx = _slots[slot.Day].IndexOf(slot);
                var nextSlot = _slots[slot.Day][idx + 1];
                _assignment[nextSlot] = task.SubjectId;
                _remainingPeriods[task.SubjectId]--;
            }
            _dailySubjectCount[task.SubjectId][slot.Day]++;

            Console.WriteLine($"Placing {task.SubjectId} at {slot.Day} {slot.StartTime}");
        }

        public void RemoveTask(PlacementTask task, TimeSlot slot)
        {
            _assignment.Remove(slot);
            _remainingPeriods[task.SubjectId]++;
            if (task.IsDouble)
            {
                int idx = _slots[slot.Day].IndexOf(slot);
                var nextSlot = _slots[slot.Day][idx + 1];
                _assignment.Remove(nextSlot);
                _remainingPeriods[task.SubjectId]++;
            }
            _dailySubjectCount[task.SubjectId][slot.Day]--;

            Console.WriteLine($"Removed {task.SubjectId} from {slot.Day} {slot.StartTime}");
        }

        public bool CanPlaceTaskHere(PlacementTask task, TimeSlot slot)
        {
            if (_dailySubjectCount[task.SubjectId][slot.Day] >= _schedulesLookup[task.SubjectId].MaxPerDay)
                return false;


            if (task.IsDouble && _dailySubjectCount[task.SubjectId][slot.Day] >= _schedulesLookup[task.SubjectId].MaxPerDay)
                return false;


            return true;
        }

        public bool HasEnoughRemainingSlots(PlacementTask task)
        {
            int remaining = RemainingPeriods(task.SubjectId);
            int candidateCount = GetAvailableSlots(task).Count;
            return candidateCount >= remaining;
        }

        private bool IsSlotLegal(PlacementTask task, TimeSlot slot, TimeSlot? next)
        {
            var subjectId = task.SubjectId;

            if (_assignment.ContainsKey(slot)) return false;
            if (task.IsDouble && next == null) return false;
            if (task.IsDouble && next != null && _assignment.ContainsKey(next)) return false;

            // Teacher constraints
            if (_schedulesLookup.TryGetValue(subjectId, out var schedule) &&
                schedule.TeacherId != Guid.Empty &&
                _teacherConstraints.TryGetValue(schedule.TeacherId, out var teacherConstraint))
            {
                if (teacherConstraint.UnavailableSlots.Contains(slot)) return false;

                var assignedToday = _assignment.Count(kvp => kvp.Key.Day == slot.Day &&
                                                             _schedulesLookup[kvp.Value].TeacherId == schedule.TeacherId);
                if (assignedToday >= teacherConstraint.MaxDailyPeriods) return false;
            }

            // Time rules
            if (_timeRules.TryGetValue(subjectId, out var timeRule))
            {
                if (timeRule.MustBeMorning && slot.StartTime >= timeRule.MorningEnd) return false;
                if (timeRule.MustBeAfternoon && slot.StartTime < timeRule.AfternoonStart) return false;
            }

            // Adjacency
            if (_adjacencyRules.TryGetValue(subjectId, out var adjacencyRule))
            {
                var daySlots = _slots[slot.Day];
                int idx = daySlots.IndexOf(slot);
                if (idx > 0)
                {
                    var prevSlot = daySlots[idx - 1];
                    if (_assignment.TryGetValue(prevSlot, out var prevSubjectId) &&
                        adjacencyRule.CannotFollowSubjects.Contains(prevSubjectId))
                        return false;
                }
            }

            return true;
        }

        public List<GeneratedSlotPreview> GetGeneratedSlots()
        {
            var result = new List<GeneratedSlotPreview>();
            foreach (var kvp in _slots)
            {
                var day = kvp.Key;
                foreach (var slot in kvp.Value)
                {
                    _assignment.TryGetValue(slot, out var sid);
                    result.Add(new GeneratedSlotPreview
                    {
                        DayOfWeek = day.ToString(),
                        SubjectId = sid,
                        Slot = slot
                    });
                }
            }
            return result;
        }
    }
}
