using System;
using System.Collections.Generic;
using System.Linq;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGeneratorV2
    {
        private Dictionary<DayOfWeek, List<SlotState>> _slotsByDay = new();
        private Dictionary<Guid, TeacherScheduleConstraints> _teacherConstraints = new();
        private List<SubjectScheduleConfig> _schedules = new();
        private List<TimeTableActivity> _activities = new();
        private Dictionary<Guid, SubjectTimeConstraints> _timeRulesBySubject = new();
        private Dictionary<Guid, SubjectAdjacencyConstraints> _adjacencyBySubject = new();

        public GenerationResult Generate(
            IReadOnlyList<TimeSlot> timeSlots,
            IReadOnlyList<SubjectScheduleConfig> schedules,
            IReadOnlyList<SubjectAdjacencyConstraints> adjacencyRules,
            IReadOnlyList<SubjectTimeConstraints> timeRules,
            IReadOnlyList<TimeTableActivity> activities,
            IReadOnlyList<TeacherScheduleConstraints> teacherConstraints)
        {
            // --- Init ---
            _schedules = schedules.ToList();
            _activities = activities.ToList();
            _teacherConstraints = teacherConstraints.ToDictionary(tc => tc.TeacherId);
            _adjacencyBySubject = adjacencyRules.ToDictionary(r => r.SubjectId);
            _timeRulesBySubject = timeRules.ToDictionary(r => r.SubjectId);

            BuildSlots(timeSlots);
            ReserveActivitySlots();

            var weekdays = _slotsByDay.Keys.Where(d => d != DayOfWeek.Saturday && d != DayOfWeek.Sunday).ToList();

            // --- Place subjects ---
            PlaceSubjects(weekdays);

            // --- Place activities like PREP ---
            PlaceActivities(weekdays);

            // --- Backfill remaining gaps ---
            BackfillGaps(weekdays);

            // --- Flatten result for output ---
            var flatSlots = _slotsByDay
                .Where(kvp => kvp.Key != DayOfWeek.Saturday && kvp.Key != DayOfWeek.Sunday)
                .SelectMany(kvp => kvp.Value.Select(slot => new GeneratedSlotPreview
                {
                    DayOfWeek = kvp.Key.ToString(),
                    SubjectId = slot.SubjectId,
                    ScheduledActivityId = slot.ScheduledActivityId,
                    SubjectName = slot.SubjectId.HasValue ? GetSubjectName(slot.SubjectId.Value) : null,
                    ActivityName = slot.ScheduledActivityId != Guid.Empty ? _activities.FirstOrDefault(a => a.ActivityID == slot.ScheduledActivityId)?.ActivityName : null,
                    TeacherName = "",
                    Slot = slot.Slot
                }))
                .OrderBy(s => s.Slot.StartTime)
                .ToList();

            return GenerationResult.Ok(flatSlots);
        }

        #region Slot & Activity Setup
        private void BuildSlots(IReadOnlyList<TimeSlot> timeSlots)
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                _slotsByDay[day] = timeSlots.Select(slot => new SlotState
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
                    IsLocked = slot.ScheduledActivityId != null,
                    ReservedForActivity = false
                }).ToList();
            }

            // Lock weekends
            foreach (var weekendDay in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
            {
                foreach (var slot in _slotsByDay[weekendDay])
                {
                    slot.IsLocked = true;
                }
            }
        }

        private void ReserveActivitySlots()
        {
            // Reserve slots for morning/afternoon activities
            foreach (var day in _slotsByDay.Keys.Where(d => d != DayOfWeek.Saturday && d != DayOfWeek.Sunday))
            {
                foreach (var slot in _slotsByDay[day])
                {
                    if (_activities.Any(a =>
                        (a.MustBeMorning && slot.IsMorning()) ||
                        (a.MustBeAfternoon && slot.IsAfternoon())))
                    {
                        slot.ReservedForActivity = true;
                    }
                }
            }
        }
        #endregion

        #region Subject Placement
        private void PlaceSubjects(List<DayOfWeek> weekdays)
        {
            // Sort: core subjects first, then doubles
            var tasks = _schedules
                .SelectMany(s =>
                {
                    var list = new List<PlacementTask>();
                    for (int i = 0; i < s.DoublePeriods; i++)
                        list.Add(new PlacementTask { SubjectId = s.SubjectId, IsDouble = true });
                    for (int i = 0; i < s.WeeklyPeriods - (s.DoublePeriods * 2); i++)
                        list.Add(new PlacementTask { SubjectId = s.SubjectId, IsDouble = false });
                    return list;
                })
                .OrderByDescending(t => _schedules.First(s => s.SubjectId == t.SubjectId).IsCoreSubject)
                .ThenByDescending(t => t.IsDouble)
                .ToList();

            var assignedDays = new Dictionary<Guid, HashSet<DayOfWeek>>();

            foreach (var task in tasks)
            {
                bool placed = false;

                foreach (var day in weekdays.OrderBy(d => _slotsByDay[d].Count(s => s.SubjectId != null)))
                {
                    if (assignedDays.TryGetValue(task.SubjectId, out var usedDays) && usedDays.Contains(day))
                        continue;

                    var bestSlot = FindBestSlotForTask(task, day);
                    if (bestSlot != null)
                    {
                        AssignTaskToSlot(task, bestSlot);
                        if (!assignedDays.ContainsKey(task.SubjectId)) assignedDays[task.SubjectId] = new HashSet<DayOfWeek>();
                        assignedDays[task.SubjectId].Add(day);
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                    return; // Could not place, skip
            }
        }

        private SlotState? FindBestSlotForTask(PlacementTask task, DayOfWeek day)
        {
            var slots = _slotsByDay[day];

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsLocked || slot.SubjectId != null || slot.ReservedForActivity) continue;

                if (!task.IsDouble)
                {
                    if (CanAssignSubjectToSlot(task.SubjectId, slot))
                        return slot;
                }
                else
                {
                    if (i < slots.Count - 1 && CanAssignSubjectToSlot(task.SubjectId, slot) && CanAssignSubjectToSlot(task.SubjectId, slots[i + 1]))
                        return slot;
                }
            }

            return null;
        }

        private void AssignTaskToSlot(PlacementTask task, SlotState slot)
        {
            AssignSubjectToSlot(task.SubjectId, slot, isFiller: false);

            if (task.IsDouble)
            {
                var next = GetNextSlot(slot);
                if (next != null) AssignSubjectToSlot(task.SubjectId, next, isFiller: false);
            }
        }

        private bool CanAssignSubjectToSlot(Guid subjectId, SlotState slot)
        {
            if (slot.IsLocked || slot.SubjectId != null || slot.ReservedForActivity)
                return false;

            if (_teacherConstraints.TryGetValue(_schedules.First(s => s.SubjectId == subjectId).TeacherId, out var tc))
            {
                if (tc.UnavailableSlots.Any(u => u.Day == slot.Slot.Day && u.StartTime == slot.Slot.StartTime))
                    return false;
            }

            if (_timeRulesBySubject.TryGetValue(subjectId, out var rule))
            {
                if (!SlotMatchesTimeRule(slot.Slot.StartTime.Value, slot.Slot.EndTime.Value, rule))
                    return false;
            }

            return true;
        }

       private bool SlotMatchesTimeRule(TimeSpan start, TimeSpan end, SubjectTimeConstraints rule)
{
    if (rule.MustBeEarlyMorning && rule.EarlyMorningEnd.HasValue && end > rule.EarlyMorningEnd.Value)
        return false;

    if (rule.MustBeMorning && rule.MorningEnd.HasValue && end > rule.MorningEnd.Value)
        return false;

    if (rule.MustBeAfternoon && rule.AfternoonStart.HasValue && start < rule.AfternoonStart.Value)
        return false;

    return true;
}

        #endregion

        #region Activity Placement
        private void PlaceActivities(List<DayOfWeek> weekdays)
        {
            foreach (var day in weekdays)
            {
                var freeSlots = _slotsByDay[day]
                    .Where(s => !s.IsLocked && s.SubjectId == null && s.ReservedForActivity)
                    .ToList();

                foreach (var slot in freeSlots)
                {
                    foreach (var activity in _activities)
                    {
                        if ((activity.MustBeMorning && slot.IsMorning()) || (activity.MustBeAfternoon && slot.IsAfternoon()))
                        {
                            AssignFillerToSlot(activity, slot);
                            break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Backfill
        private void BackfillGaps(List<DayOfWeek> weekdays)
        {
            foreach (var day in weekdays)
            {
                foreach (var slot in _slotsByDay[day])
                {
                    if (slot.SubjectId != null || slot.IsLocked || slot.ReservedForActivity) continue;

                    // Pick subject with least assigned periods
                    var subject = _schedules
                        .OrderBy(s => _slotsByDay.SelectMany(d => d.Value).Count(sl => sl.SubjectId == s.SubjectId))
                        .FirstOrDefault();

                    if (subject != null)
                        AssignSubjectToSlot(subject.SubjectId, slot, isFiller: true);
                }
            }
        }
        #endregion

        #region Helpers
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

        private SlotState? GetNextSlot(SlotState slot)
        {
            var list = _slotsByDay[slot.Slot.Day];
            var index = list.IndexOf(slot);
            return index >= 0 && index < list.Count - 1 ? list[index + 1] : null;
        }

        private string GetSubjectName(Guid subjectId)
        {
            return _schedules.FirstOrDefault(s => s.SubjectId == subjectId)?.SubjectName ?? "Unknown";
        }
        #endregion
    }

    
   
}
