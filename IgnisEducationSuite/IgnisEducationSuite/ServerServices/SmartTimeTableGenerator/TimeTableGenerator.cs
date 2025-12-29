using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGenerator : ITimetableGenerator
    {
        private Random _rand = new Random();
        private Dictionary<(DayOfWeek, Guid), int> dailyCount = new();
        private bool debug = true; // Set to false to disable debug prints
        private TimeSpan EarlyMorningCutOFF = TimeSpan.Zero;
        public List<TimeSlot> Generate(
            List<TimeSlot> slots,
            List<SubjectScheduleConfig> subjects,
            List<SubjectAdjacencyConstraints> adjacencyConstraints,
            TimeTableActivity? prepActivity = null)
        {
            dailyCount = new Dictionary<(DayOfWeek, Guid), int>();
            var result = new List<TimeSlot>();
            var prepareslots = new List<TimeSlot>();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                for (int i = 0; i < slots.Count - 1; i++)
                {
                    prepareslots.Add(new TimeSlot
                    {
                        Day = day,
                        StartTime = slots[i].StartTime,
                        EndTime = slots[i].EndTime,
                        SubjectId = Guid.Empty,
                        SubjectName = "Free"
                    });
                }
            }
            EarlyMorningCutOFF = subjects.Select(x => x.EarlyMorningEnd).First();
            var remainingPeriods = subjects.ToDictionary(s => s.SubjectId, s => s.WeeklyPeriods);
            var remainingDoubles = subjects.ToDictionary(s => s.SubjectId, s => s.RequiredDoubles);

            var earlySubjects = subjects.Where(s => s.EarlyMorningOnly).Select(s => s.SubjectId).ToHashSet();
            var normalSubjects = subjects.Select(s => s.SubjectId).Where(s => !earlySubjects.Contains(s)).ToList();
            var learningDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

            var slotsByDay = prepareslots
                .GroupBy(s => s.Day)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

            var adjacencyDict = subjects.ToDictionary(
                s => s.SubjectId,
                s => adjacencyConstraints.FirstOrDefault(c => c.SubjectId == s.SubjectId) ??
                     new SubjectAdjacencyConstraints { SubjectId = s.SubjectId }
            );

            foreach (var day in slotsByDay.Keys)
            {
                Guid previousSubjectId = Guid.Empty;
                var daySlots = slotsByDay[day];

                if (debug) Console.WriteLine($"\n--- Generating timetable for {day} ---");

                for (int i = 0; i < daySlots.Count; i++)
                {
                    var slot = daySlots[i];

                    // Prep activity
                    if (prepActivity != null && slot.StartTime >= prepActivity.StartFrom)
                    {
                        previousSubjectId = Guid.Empty;
                        result.Add(new TimeSlot
                        {
                            Day = slot.Day,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            SubjectId = Guid.Empty,
                            SubjectName = prepActivity.ActivityName,
                        });

                        if (debug) Console.WriteLine($"Slot {i}: {slot.StartTime}-{slot.EndTime} -> Prep Activity ({prepActivity.ActivityName})");

                        continue;
                    }

                    // Determine eligible subjects
                    List<Guid> eligible;
                    if (slot.StartTime < EarlyMorningCutOFF)
                        eligible = remainingPeriods.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToList();
                    else
                        eligible = normalSubjects.Where(s => remainingPeriods[s] > 0).ToList();

                    // Enforce daily max
                    eligible = eligible
                        .Where(s =>
                        {
                            int count = GetDailyCount(day, s);
                            if (count >= 2) return false;
                            if (count == 1) return previousSubjectId == s;
                            return true;
                        }).ToList();

                    // Apply adjacency
                    eligible = eligible
                        .Where(s => previousSubjectId == Guid.Empty ||
                                    !adjacencyDict[s].CannotFollowSubjects.Contains(previousSubjectId))
                        .ToList();

                    // If none, fallback
                    if (!eligible.Any())
                    {
                        eligible = remainingPeriods
                            .Where(kvp =>
                                kvp.Value > 0 &&
                                GetDailyCount(day, kvp.Key) < 2 &&
                                (!earlySubjects.Contains(kvp.Key) || slot.StartTime < TimeSpan.FromHours(10.5)) &&
                                (previousSubjectId == Guid.Empty ||
                                 !adjacencyDict[kvp.Key].CannotFollowSubjects.Contains(previousSubjectId)))
                            .Select(kvp => kvp.Key)
                            .ToList();
                    }

                    if (!eligible.Any())
                    {
                        // Free slot
                        result.Add(new TimeSlot
                        {
                            Day = slot.Day,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            SubjectId = Guid.Empty,
                            SubjectName = "Free"
                        });
                        previousSubjectId = Guid.Empty;

                        if (debug) Console.WriteLine($"Slot {i}: {slot.StartTime}-{slot.EndTime} -> Free (no eligible subjects)");
                        continue;
                    }

                    // Pick subject
                    var chosen = eligible[_rand.Next(eligible.Count)];
                    var subjectName = subjects.First(s => s.SubjectId == chosen).SubjectName;
                    int todayCount = GetDailyCount(day, chosen);

                    bool placeDouble = false;
                    if (todayCount == 0 && i < daySlots.Count - 1)
                    {
                        if (remainingDoubles[chosen] > 0) placeDouble = true;
                        else if (remainingPeriods[chosen] >= 2 && _rand.NextDouble() < 0.25) placeDouble = true;
                    }

                    // Place first slot
                    result.Add(new TimeSlot
                    {
                        Day = slot.Day,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        SubjectId = chosen,
                        SubjectName = subjectName
                    });
                    remainingPeriods[chosen]--;
                    IncrementDailyCount(day, chosen);

                    if (debug)
                    {
                        Console.WriteLine($"Slot {i}: {slot.StartTime}-{slot.EndTime} -> {subjectName} (Remaining: {remainingPeriods[chosen]}, DailyCount: {GetDailyCount(day, chosen)})");
                    }

                    // Place second slot if double
                    if (placeDouble)
                    {
                        var nextSlot = daySlots[i + 1];
                        result.Add(new TimeSlot
                        {
                            Day = nextSlot.Day,
                            StartTime = nextSlot.StartTime,
                            EndTime = nextSlot.EndTime,
                            SubjectId = chosen,
                            SubjectName = subjectName
                        });
                        remainingPeriods[chosen]--;
                        IncrementDailyCount(day, chosen);

                        if (remainingDoubles[chosen] > 0) remainingDoubles[chosen]--;
                        i++; // skip next slot

                        if (debug)
                            Console.WriteLine($"Slot {i}: {nextSlot.StartTime}-{nextSlot.EndTime} -> {subjectName} (Double) (Remaining: {remainingPeriods[chosen]}, DailyCount: {GetDailyCount(day, chosen)})");
                    }

                    previousSubjectId = chosen;
                }
            }

            if (debug) Console.WriteLine("\n--- Timetable generation complete ---\n");
            return result;
        }

        #region Helpers
        int GetDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount.TryGetValue((day, subjectId), out var count) ? count : 0;

        void IncrementDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount[(day, subjectId)] = GetDailyCount(day, subjectId) + 1;
        #endregion
    }
}
