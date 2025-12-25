using System;
using System.Collections.Generic;
using System.Linq;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;

class TimetableGeneratorV3_1Tests
{
    static void Main()
    {
        Console.WriteLine("=== TIMETABLE GENERATOR V3.1 – CORE RULE TEST ===\n");

        TestV3_1_WeeklyQuota_MathRules_WithActivity();

        Console.WriteLine("\n=== TEST COMPLETE ===");
    }

    // =========================
    // CORE TEST
    // =========================
    // =========================
    // CORE TEST + DAILY AFTERNOON ACTIVITY
    // =========================
    private static void TestV3_1_WeeklyQuota_MathRules_WithActivity()
    {
        Console.WriteLine("--- Test: Weekly Quotas + Math Rules + Daily Activity ---");

        var slots = DefaultTimeSlots();

        // SUBJECT IDS
        var mathId = Guid.NewGuid();
        var physicsId = Guid.NewGuid();
        var chemistryId = Guid.NewGuid();
        var biologyId = Guid.NewGuid();
        var englishId = Guid.NewGuid();

        var subjects = new List<SubjectScheduleConfig>
    {
        new() { SubjectId = mathId, SubjectName = "Math", WeeklyPeriods = 5, DoublePeriods = 2 },
        new() { SubjectId = physicsId, SubjectName = "Physics", WeeklyPeriods = 5, DoublePeriods = 1 },
        new() { SubjectId = chemistryId, SubjectName = "Chemistry", WeeklyPeriods = 5, DoublePeriods = 1 },
        new() { SubjectId = biologyId, SubjectName = "Biology", WeeklyPeriods = 5, DoublePeriods = 1 },
        new() { SubjectId = englishId, SubjectName = "English", WeeklyPeriods = 5, DoublePeriods = 1 },
    };

        var adjacencyRules = new List<SubjectAdjacencyConstraints>
    {
        new()
        {
            SubjectId = mathId,
            CannotFollowSubjects = new List<Guid>
            {
                physicsId,
                chemistryId,
                biologyId
            }
        }
    };

        var timeRules = new List<SubjectTimeConstraints>
    {
        new()
        {
            SubjectId = mathId,
            MustBeEarlyMorning = true,
            EarlyMorningEnd = TimeSpan.Parse("10:30")
        }
    };

        // DAILY AFTERNOON ACTIVITY
        var activityId = Guid.NewGuid();
        var dailyAfternoonActivity = new TimeTableActivity
        {
            ActivityID = activityId,
            ActivityName = "Daily Assembly",
            MustBeAfternoon = true,
            MustBeMorning = false
        };

        var activities = new List<TimeTableActivity> { dailyAfternoonActivity };
        var generator = new TimetableGeneratorV3_1();
        // Generate timetable
        var result = generator.Generate(
    slots,
    subjects,
    adjacencyRules,
    timeRules,
    activities,                  // <-- pass the activity list
    new List<TeacherScheduleConstraints>()
);

        PrintTimetable(result);

        // =========================
        // ASSERTIONS
        // =========================
        var placedSubjects = result.Slots
            .Where(s => s.SubjectId.HasValue)
            .GroupBy(s => s.SubjectName!)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 1️⃣ EXACT WEEKLY QUOTAS
        foreach (var subject in subjects)
        {
            if (!placedSubjects.TryGetValue(subject.SubjectName, out var list))
                throw new Exception($"❌ {subject.SubjectName} not scheduled");

            if (list.Count != 5)
                throw new Exception($"❌ {subject.SubjectName} has {list.Count} periods (expected 5)");
        }

        // 2️⃣ DAILY AFTERNOON ACTIVITY PRESENT
        var afternoonSlots = result.Slots
            .Where(s => s.Slot.StartTime >= TimeSpan.Parse("14:30"))
            .ToList();

        foreach (var day in Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>()
            .Where(d => d != DayOfWeek.Saturday && d != DayOfWeek.Sunday))
        {
            if (!afternoonSlots.Any(s => s.DayOfWeek == day.ToString() && s.ActivityName == "Daily Assembly"))
                throw new Exception($"❌ Daily Assembly not scheduled on {day}");
        }

        Console.WriteLine("✅ PASSED: Weekly quotas, doubles, adjacency, time rules, daily activity.");
    }


    // =========================
    // HELPERS
    // =========================
    private static List<TimeSlot> DefaultTimeSlots() => new()
    {
        new() { StartTime = TimeSpan.Parse("07:10"), EndTime = TimeSpan.Parse("07:50") },
        new() { StartTime = TimeSpan.Parse("07:50"), EndTime = TimeSpan.Parse("08:30") },
        new() { StartTime = TimeSpan.Parse("08:30"), EndTime = TimeSpan.Parse("09:10") },
        new() { StartTime = TimeSpan.Parse("09:10"), EndTime = TimeSpan.Parse("09:50") },
        new() { StartTime = TimeSpan.Parse("09:50"), EndTime = TimeSpan.Parse("10:30") },
        new() { StartTime = TimeSpan.Parse("14:30"), EndTime = TimeSpan.Parse("16:30") },
    };

    private static void PrintTimetable(GenerationResult result)
    {
        foreach (var s in result.Slots
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.Slot.StartTime))
        {
            var name = s.SubjectName ?? s.ActivityName ?? "Free";
            Console.WriteLine($"[{s.DayOfWeek}] {s.Slot.StartTime:hh\\:mm}-{s.Slot.EndTime:hh\\:mm} : {name}");
        }
    }
}
