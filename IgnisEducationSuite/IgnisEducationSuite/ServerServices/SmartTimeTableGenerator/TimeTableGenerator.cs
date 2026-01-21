using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGenerator : ITimetableGenerator
    {
        private readonly Random _rand = new Random();
        private readonly Dictionary<(DayOfWeek, Guid), int> dailyCount = new();
        private readonly bool debug = true;

        private TimeSpan EarlyMorningCutOFF = TimeSpan.Zero;

        public List<TimeSlot> Generate(
            List<TimeSlot> slots,
            List<SubjectScheduleConfig> subjects,
            List<SubjectAdjacencyConstraints> adjacencyConstraints,
            TimeTableActivity? prepActivity = null)
        {
            dailyCount.Clear();
            var prepareslots = new List<TimeSlot>();

            // 1️⃣ Build empty timetable (Mon–Fri)
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                foreach (var s in slots)
                {
                    prepareslots.Add(new TimeSlot
                    {
                        Day = day,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        TimeslotID = s.TimeslotID,
                        SubjectId = Guid.Empty,
                        SubjectName = "Free",
                        ScheduledActivityId = null,
                        IsLocked = false,
                        SchoolEarlyMorningEnd = s.SchoolEarlyMorningEnd,
                        SchoolAfternoonStart = s.SchoolAfternoonStart,
                        SchoolMorningEnd = s.SchoolMorningEnd,
                        
                    });
                }
            }

            EarlyMorningCutOFF = subjects.First().EarlyMorningEnd;

            var remainingPeriods = subjects.ToDictionary(s => s.SubjectId, s => s.WeeklyPeriods);
            var remainingDoubles = subjects.ToDictionary(s => s.SubjectId, s => s.RequiredDoubles);

            var earlySubjects = subjects
                .Where(s => s.EarlyMorningOnly)
                .Select(s => s.SubjectId)
                .ToHashSet();

            var normalSubjects = subjects
                .Select(s => s.SubjectId)
                .Where(s => !earlySubjects.Contains(s))
                .ToList();

            // 2️⃣ PLACE ACTIVITIES (LOCKED, DAY-AWARE)
            var activitySlots = new HashSet<(DayOfWeek Day, Guid SlotId)>();

            if (prepActivity != null)
            {
                foreach (var slot in prepareslots
                    .Where(s =>
                        s.StartTime >= prepActivity.StartFrom &&
                        prepActivity.Days.Contains(s.Day)))
                {
                    slot.SubjectId = Guid.Empty;
                    slot.SubjectName = prepActivity.ActivityName;
                    slot.ScheduledActivityId = prepActivity.ActivityID;
                    slot.IsLocked = true;

                    activitySlots.Add((slot.Day, slot.TimeslotID));
                }
            }

            var slotsByDay = prepareslots
                .GroupBy(s => s.Day)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(s => s.StartTime).ToList()
                );

            var adjacencyDict = subjects.ToDictionary(
                s => s.SubjectId,
                s => adjacencyConstraints.FirstOrDefault(a => a.SubjectId == s.SubjectId)
                     ?? new SubjectAdjacencyConstraints { SubjectId = s.SubjectId }
            );

            // 3️⃣ GENERATE SUBJECTS (MUTATE SLOTS ONLY)
            foreach (var day in slotsByDay.Keys)
            {
                Guid previousSubjectId = Guid.Empty;
                var daySlots = slotsByDay[day];

                if (debug)
                    Console.WriteLine($"\n--- Generating timetable for {day} ---");

                for (int i = 0; i < daySlots.Count; i++)
                {
                    var slot = daySlots[i];

                    // 🚫 Skip activity slots
                    if (slot.IsLocked || activitySlots.Contains((slot.Day, slot.TimeslotID)))
                    {
                        previousSubjectId = Guid.Empty;
                        continue;
                    }

                    // Eligible subjects
                    List<Guid> eligible =
                        slot.StartTime < EarlyMorningCutOFF
                            ? remainingPeriods.Where(p => p.Value > 0).Select(p => p.Key).ToList()
                            : normalSubjects.Where(s => remainingPeriods[s] > 0).ToList();

                    // Remove subjects already on this day (prevent split doubles)
                    eligible = eligible.Where(s => GetDailyCount(day, s) == 0).ToList();

                    // Adjacency filter
                    eligible = eligible
                        .Where(s =>
                            previousSubjectId == Guid.Empty ||
                            !adjacencyDict[s].CannotFollowSubjects.Contains(previousSubjectId))
                        .ToList();

                    if (!eligible.Any())
                    {
                        previousSubjectId = Guid.Empty;
                        continue;
                    }

                    // Pick random subject
                    var chosen = eligible[_rand.Next(eligible.Count)];
                    var subjectName = subjects.First(s => s.SubjectId == chosen).SubjectName;

                    // Place single
                    slot.SubjectId = chosen;
                    slot.SubjectName = subjectName;
                    remainingPeriods[chosen]--;
                    IncrementDailyCount(day, chosen);

                    // Attempt double
                    if (CanPlaceDouble(daySlots, i, chosen, remainingPeriods, remainingDoubles, activitySlots))
                    {
                        var next = daySlots[i + 1];

                        next.SubjectId = chosen;
                        next.SubjectName = subjectName;
                        remainingPeriods[chosen]--;
                        remainingDoubles[chosen]--;
                        IncrementDailyCount(day, chosen);
                        i++; // skip next slot
                    }

                    previousSubjectId = chosen;
                }

            }



#if DEBUG
            // Ensure activity slots remain untouched
            var violated = prepareslots
                .Where(s => s.IsActivity() && s.SubjectId != Guid.Empty)
                .ToList();

            if (violated.Any())
                throw new Exception("Generator violated activity ownership");
#endif

            // After the for loop over days/slots, before returning
            if (debug)
            {
                Console.WriteLine("\n--- Generated Timetable (Before Repair) ---\n");
                foreach (var day in Enum.GetValues(typeof(DayOfWeek)))
                {
                    if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

                    Console.WriteLine($"{day}:");
                    var daySlots = prepareslots
                        .Where(s => s.Day == (DayOfWeek)day)
                        .OrderBy(s => s.StartTime)
                        .ToList();

                    foreach (var slot in daySlots)
                    {
                        string info = slot.IsActivity()
                            ? $"[ACTIVITY: {slot.SubjectName}]"
                            : slot.SubjectId == Guid.Empty
                                ? "[Free]"
                                : slot.SubjectName;

                        Console.WriteLine($"  {slot.StartTime:hh\\:mm}-{slot.EndTime:hh\\:mm} -> {info}");
                    }
                    Console.WriteLine();
                }
            }

            return prepareslots;
        }

        // -------------------------
        // Helpers
        // -------------------------
        private bool CanPlaceDouble(
            List<TimeSlot> daySlots,
            int index,
            Guid subjectId,
            Dictionary<Guid, int> remainingPeriods,
            Dictionary<Guid, int> remainingDoubles,
            HashSet<(DayOfWeek, Guid)> activitySlots)
        {
            if (remainingPeriods[subjectId] < 2)
                return false;

            if (remainingDoubles[subjectId] <= 0)
                return false;

            if (index >= daySlots.Count - 1)
                return false;

            var current = daySlots[index];
            var next = daySlots[index + 1];

            // Must be consecutive, empty, and not activity
            if (current.EndTime != next.StartTime)
                return false;

            if (activitySlots.Contains((current.Day, current.TimeslotID)) ||
                activitySlots.Contains((next.Day, next.TimeslotID)))
                return false;

            if (current.SubjectId != Guid.Empty || next.SubjectId != Guid.Empty)
                return false;

            return true;
        }

        private int GetDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount.TryGetValue((day, subjectId), out var c) ? c : 0;

        private void IncrementDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount[(day, subjectId)] = GetDailyCount(day, subjectId) + 1;
    }
}
