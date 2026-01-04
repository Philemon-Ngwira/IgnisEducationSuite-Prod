//using EDUSphereSharedProject.Models;
//using EDUSphereSharedProject.UniversalModels.TimeTabling;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
//{
//    public class TimetableGenerator : ITimetableGenerator
//    {
//        private Random _rand = new Random();
//        private Dictionary<(DayOfWeek, Guid), int> dailyCount = new();
//        private bool debug = true; // Set to false to disable debug prints
//        private TimeSpan EarlyMorningCutOFF = TimeSpan.Zero;
//        public List<TimeSlot> Generate(
//            List<TimeSlot> slots,
//            List<SubjectScheduleConfig> subjects,
//            List<SubjectAdjacencyConstraints> adjacencyConstraints,
//            TimeTableActivity? prepActivity = null)
//        {
//            dailyCount = new Dictionary<(DayOfWeek, Guid), int>();
//            var result = new List<TimeSlot>();
//            var prepareslots = new List<TimeSlot>();
//            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
//            {
//                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
//                    continue;

//                for (int i = 0; i < slots.Count; i++)
//                {
//                    prepareslots.Add(new TimeSlot
//                    {
//                        Day = day,
//                        StartTime = slots[i].StartTime,
//                        EndTime = slots[i].EndTime,
//                        SubjectId = Guid.Empty,
//                        SubjectName = "Free",
//                        TimeslotID = slots[i].TimeslotID,
//                    });
//                }
//            }
//            EarlyMorningCutOFF = subjects.Select(x => x.EarlyMorningEnd).First();
//            var remainingPeriods = subjects.ToDictionary(s => s.SubjectId, s => s.WeeklyPeriods);
//            var remainingDoubles = subjects.ToDictionary(s => s.SubjectId, s => s.RequiredDoubles);

//            var earlySubjects = subjects.Where(s => s.EarlyMorningOnly).Select(s => s.SubjectId).ToHashSet();
//            var normalSubjects = subjects.Select(s => s.SubjectId).Where(s => !earlySubjects.Contains(s)).ToList();
//            var learningDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

//            var slotsByDay = prepareslots
//                .GroupBy(s => s.Day)
//                .OrderBy(g => g.Key)
//                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

//            var adjacencyDict = subjects.ToDictionary(
//                s => s.SubjectId,
//                s => adjacencyConstraints.FirstOrDefault(c => c.SubjectId == s.SubjectId) ??
//                     new SubjectAdjacencyConstraints { SubjectId = s.SubjectId }
//            );

//            foreach (var day in slotsByDay.Keys)
//            {
//                Guid previousSubjectId = Guid.Empty;
//                var daySlots = slotsByDay[day];

//                if (debug) Console.WriteLine($"\n--- Generating timetable for {day} ---");

//                for (int i = 0; i < daySlots.Count; i++)
//                {
//                    var slot = daySlots[i];

//                    // ================================
//                    // HARD RULE: Activity owns the slot
//                    // ================================
//                    if (prepActivity != null &&
//                        slot.StartTime >= prepActivity.StartFrom)
//                    {
//                        previousSubjectId = Guid.Empty;

//                        result.Add(new TimeSlot
//                        {
//                            Day = slot.Day,
//                            StartTime = slot.StartTime,
//                            EndTime = slot.EndTime,
//                            SubjectId = Guid.Empty,
//                            SubjectName = prepActivity.ActivityName,
//                            ScheduledActivityId = prepActivity.ActivityID,
//                            TimeslotID = daySlots[i].TimeslotID,
//                        });

//                        if (debug)
//                            Console.WriteLine(
//                                $"Slot {i}: {slot.StartTime}-{slot.EndTime} -> Activity ({prepActivity.ActivityName})");

//                        continue;
//                    }

//                    // ================================
//                    // SUBJECT ELIGIBILITY (unchanged)
//                    // ================================
//                    List<Guid> eligible;
//                    if (slot.StartTime < EarlyMorningCutOFF)
//                        eligible = remainingPeriods.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToList();
//                    else
//                        eligible = normalSubjects.Where(s => remainingPeriods[s] > 0).ToList();

//                    eligible = eligible
//                        .Where(s =>
//                        {
//                            int count = GetDailyCount(day, s);
//                            if (count >= 2) return false;
//                            if (count == 1) return previousSubjectId == s;
//                            return true;
//                        })
//                        .Where(s =>
//                            previousSubjectId == Guid.Empty ||
//                            !adjacencyDict[s].CannotFollowSubjects.Contains(previousSubjectId))
//                        .ToList();

//                    if (!eligible.Any())
//                    {
//                        result.Add(new TimeSlot
//                        {
//                            Day = slot.Day,
//                            StartTime = slot.StartTime,
//                            EndTime = slot.EndTime,
//                            SubjectId = Guid.Empty,
//                            SubjectName = "Free",
//                            TimeslotID = daySlots[i].TimeslotID,
//                        });

//                        previousSubjectId = Guid.Empty;

//                        if (debug)
//                            Console.WriteLine($"Slot {i}: {slot.StartTime}-{slot.EndTime} -> Free");

//                        continue;
//                    }

//                    var chosen = eligible[_rand.Next(eligible.Count)];
//                    var subjectName = subjects.First(s => s.SubjectId == chosen).SubjectName;
//                    int todayCount = GetDailyCount(day, chosen);

//                    bool placeDouble = false;
//                    if (todayCount == 0 && i < daySlots.Count - 1)
//                    {
//                        if (remainingDoubles[chosen] > 0) placeDouble = true;
//                        else if (remainingPeriods[chosen] >= 2 && _rand.NextDouble() < 0.25) placeDouble = true;
//                    }

//                    result.Add(new TimeSlot
//                    {
//                        Day = slot.Day,
//                        StartTime = slot.StartTime,
//                        EndTime = slot.EndTime,
//                        SubjectId = chosen,
//                        SubjectName = subjectName,
//                        TimeslotID = daySlots[i].TimeslotID,
//                    });

//                    remainingPeriods[chosen]--;
//                    IncrementDailyCount(day, chosen);

//                    if (placeDouble)
//                    {
//                        var nextSlot = daySlots[i + 1];

//                        result.Add(new TimeSlot
//                        {
//                            Day = nextSlot.Day,
//                            StartTime = nextSlot.StartTime,
//                            EndTime = nextSlot.EndTime,
//                            SubjectId = chosen,
//                            SubjectName = subjectName,
//                            TimeslotID = nextSlot.TimeslotID, // ✅ FIX
//                        });

//                        remainingPeriods[chosen]--;
//                        IncrementDailyCount(day, chosen);

//                        if (remainingDoubles[chosen] > 0)
//                            remainingDoubles[chosen]--;

//                        i++;
//                    }


//                    previousSubjectId = chosen;
//                }

//            }

//            if (debug) Console.WriteLine("\n--- Timetable generation complete ---\n");
//            return result;
//        }

//        #region Helpers
//        int GetDailyCount(DayOfWeek day, Guid subjectId) =>
//            dailyCount.TryGetValue((day, subjectId), out var count) ? count : 0;

//        void IncrementDailyCount(DayOfWeek day, Guid subjectId) =>
//            dailyCount[(day, subjectId)] = GetDailyCount(day, subjectId) + 1;
//        #endregion
//    }
//}
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
        private bool debug = true;
        private TimeSpan EarlyMorningCutOFF = TimeSpan.Zero;

        public List<TimeSlot> Generate(
            List<TimeSlot> slots,
            List<SubjectScheduleConfig> subjects,
            List<SubjectAdjacencyConstraints> adjacencyConstraints,
            TimeTableActivity? prepActivity = null)
        {
            dailyCount.Clear();
            var result = new List<TimeSlot>();
            var prepareslots = new List<TimeSlot>();

            // --- Prepare empty slots for all weekdays ---
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

                foreach (var s in slots)
                {
                    prepareslots.Add(new TimeSlot
                    {
                        Day = day,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        SubjectId = Guid.Empty,
                        SubjectName = "Free",
                        TimeslotID = s.TimeslotID,
                        ScheduledActivityId = null
                    });
                }
            }

            EarlyMorningCutOFF = subjects.Select(x => x.EarlyMorningEnd).First();

            var remainingPeriods = subjects.ToDictionary(s => s.SubjectId, s => s.WeeklyPeriods);
            var remainingDoubles = subjects.ToDictionary(s => s.SubjectId, s => s.RequiredDoubles);

            var earlySubjects = subjects.Where(s => s.EarlyMorningOnly).Select(s => s.SubjectId).ToHashSet();
            var normalSubjects = subjects.Select(s => s.SubjectId).Where(s => !earlySubjects.Contains(s)).ToList();

            // --- Precompute activity slots ---
            HashSet<Guid> activitySlotIDs = new();
            if (prepActivity != null)
            {
                foreach (var s in prepareslots.Where(sl => sl.StartTime >= prepActivity.StartFrom))
                {
                    s.ScheduledActivityId = prepActivity.ActivityID;
                    s.SubjectName = prepActivity.ActivityName;
                    activitySlotIDs.Add(s.TimeslotID);
                }
            }

            var slotsByDay = prepareslots
                .GroupBy(s => s.Day)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

            var adjacencyDict = subjects.ToDictionary(
                s => s.SubjectId,
                s => adjacencyConstraints.FirstOrDefault(c => c.SubjectId == s.SubjectId)
                     ?? new SubjectAdjacencyConstraints { SubjectId = s.SubjectId }
            );

            // --- Generate timetable ---
            foreach (var day in slotsByDay.Keys)
            {
                Guid previousSubjectId = Guid.Empty;
                var daySlots = slotsByDay[day];

                if (debug) Console.WriteLine($"\n--- Generating timetable for {day} ---");

                for (int i = 0; i < daySlots.Count; i++)
                {
                    var slot = daySlots[i];

                    // --- Skip activity slots ---
                    if (activitySlotIDs.Contains(slot.TimeslotID))
                    {
                        previousSubjectId = Guid.Empty;
                        result.Add(slot);
                        continue;
                    }

                    // --- Determine eligible subjects ---
                    List<Guid> eligible = slot.StartTime < EarlyMorningCutOFF
                        ? remainingPeriods.Where(kvp => kvp.Value > 0).Select(kvp => kvp.Key).ToList()
                        : normalSubjects.Where(s => remainingPeriods[s] > 0).ToList();

                    eligible = eligible
                        .Where(s =>
                        {
                            int count = GetDailyCount(day, s);
                            if (count >= 2) return false;
                            if (count == 1) return previousSubjectId == s;
                            return true;
                        })
                        .Where(s =>
                            previousSubjectId == Guid.Empty ||
                            !adjacencyDict[s].CannotFollowSubjects.Contains(previousSubjectId))
                        .ToList();

                    // --- Leave free if no eligible subject ---
                    if (!eligible.Any())
                    {
                        result.Add(new TimeSlot
                        {
                            Day = slot.Day,
                            StartTime = slot.StartTime,
                            EndTime = slot.EndTime,
                            SubjectId = Guid.Empty,
                            SubjectName = "Free",
                            TimeslotID = slot.TimeslotID
                        });
                        previousSubjectId = Guid.Empty;
                        continue;
                    }

                    // --- Place chosen subject ---
                    var chosen = eligible[_rand.Next(eligible.Count)];
                    var subjectName = subjects.First(s => s.SubjectId == chosen).SubjectName;
                    int todayCount = GetDailyCount(day, chosen);

                    bool placeDouble = false;
                    if (todayCount == 0 && i < daySlots.Count - 1 &&
                        !activitySlotIDs.Contains(daySlots[i + 1].TimeslotID))
                    {
                        if (remainingDoubles[chosen] > 0) placeDouble = true;
                        else if (remainingPeriods[chosen] >= 2 && _rand.NextDouble() < 0.25) placeDouble = true;
                    }

                    result.Add(new TimeSlot
                    {
                        Day = slot.Day,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        SubjectId = chosen,
                        SubjectName = subjectName,
                        TimeslotID = slot.TimeslotID
                    });
                    remainingPeriods[chosen]--;
                    IncrementDailyCount(day, chosen);

                    // --- Place double safely ---
                    if (placeDouble)
                    {
                        var nextSlot = daySlots[i + 1];
                        result.Add(new TimeSlot
                        {
                            Day = nextSlot.Day,
                            StartTime = nextSlot.StartTime,
                            EndTime = nextSlot.EndTime,
                            SubjectId = chosen,
                            SubjectName = subjectName,
                            TimeslotID = nextSlot.TimeslotID
                        });

                        remainingPeriods[chosen]--;
                        IncrementDailyCount(day, chosen);
                        if (remainingDoubles[chosen] > 0) remainingDoubles[chosen]--;
                        i++; // skip next slot
                    }

                    previousSubjectId = chosen;
                }
            }

#if DEBUG
            // --- Validate activity slots are untouched ---
            var violated = result.Where(s => activitySlotIDs.Contains(s.TimeslotID) && s.SubjectId != Guid.Empty).ToList();
            if (violated.Any()) throw new Exception("Generator violated activity ownership");
#endif

            if (debug) Console.WriteLine("\n--- Timetable generation complete ---\n");
            return result;
        }

        #region Helpers
        private int GetDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount.TryGetValue((day, subjectId), out var count) ? count : 0;

        private void IncrementDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount[(day, subjectId)] = GetDailyCount(day, subjectId) + 1;
        #endregion
    }
}

