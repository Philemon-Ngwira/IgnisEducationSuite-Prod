
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using Humanizer;
using SchedulingTester.TimeTableGenerator;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

class RealisticTimetableTest
{
    static void Main()
    {
        Console.WriteLine("=== REALISTIC TIMETABLE GENERATOR TEST ===\n");

        TestFullWeekEarlyMath();

        Console.WriteLine("\n=== TEST COMPLETE ===");
    }

    private static void TestFullWeekEarlyMath()
    {
        Console.WriteLine("--- Test: Full week, early-morning Math, repair-driven pipeline ---");

        // 13 slots/day from 07:10 to 15:10
        var slotTimes = new[]
        {
        "07:10","07:50","08:30","09:10","09:50","10:30",
        "11:10","11:50","12:30","13:10","13:50","14:30","15:10"
    };

        var slots = new List<TimeSlot>();
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            for (int i = 0; i < slotTimes.Length - 1; i++)
            {
                slots.Add(new TimeSlot
                {
                    Day = day,
                    StartTime = TimeSpan.Parse(slotTimes[i]),
                    EndTime = TimeSpan.Parse(slotTimes[i + 1]),
                    SubjectId = Guid.Empty,
                    SubjectName = "Free"
                });
            }
        }

        // Subjects
        var subjects = new List<SchedulingTester.TimeTableGenerator.SubjectScheduleConfig>
    {
        new() { SubjectId = Guid.NewGuid(), SubjectName = "Math", WeeklyPeriods = 5, EarlyMorningOnly = true, RequiredDoubles = 2 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "Physics", WeeklyPeriods = 5 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "Chemistry", WeeklyPeriods = 5 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "Biology", WeeklyPeriods = 5 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "English", WeeklyPeriods = 5 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "History", WeeklyPeriods = 5 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "Geography", WeeklyPeriods = 5 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "Computer Studies", WeeklyPeriods = 5 },
        new() { SubjectId = Guid.NewGuid(), SubjectName = "Civic Education", WeeklyPeriods = 5 }
    };

        var adjacencyConstraints = new List<SubjectAdjacencyConstraints>
    {
        new()
        {
            SubjectId = subjects.First(s => s.SubjectName == "Physics").SubjectId,
            CannotFollowSubjects = new() { subjects.First(s => s.SubjectName == "Math").SubjectId }
        },
        new()
        {
            SubjectId = subjects.First(s => s.SubjectName == "Chemistry").SubjectId,
            CannotFollowSubjects = new() { subjects.First(s => s.SubjectName == "Physics").SubjectId }
        }
    };

        var prepActivity = new Activity
        {
            Name = "PREP",
            StartFrom = TimeSpan.Parse("13:50")
        };

        // --------------------------------------------------
        // 1. GENERATE (DIRTY)
        // --------------------------------------------------
        var generator = new RealisticTimetableGenerator();
        var generatedSlots = generator.Generate(
            slots,
            subjects,
            adjacencyConstraints,
            prepActivity
        );

        // --------------------------------------------------
        // 2. BUILD STATE FROM GENERATED SLOTS
        // --------------------------------------------------
        var state = new TimetableState(
            generatedSlots,
            subjects,
            adjacencyConstraints
        );

        // --------------------------------------------------
        // 3. VALIDATE (EXPECT FAILURES)
        // --------------------------------------------------
        var initialReport  = new TimetableValidator().Analyze(
    state,
    subjects.ToDictionary(s => s.SubjectId),
    adjacencyConstraints.ToDictionary(a => a.SubjectId)
);

        Console.WriteLine("Initial violations:");
        foreach (var v in initialReport.InvariantViolations)
            Console.WriteLine(" - " + v);

        //        // --------------------------------------------------
        //        // 4. REPAIR PIPELINE
        //        // --------------------------------------------------


        //        var repair = new TimeTableRepair();
        //        repair.Repair(state, prepActivity);

        //        // --------------------------------------------------
        //        // 5. VALIDATE AGAIN
        //        // --------------------------------------------------
        //        var finalReport = new TimetableValidator().Analyze(
        //    state,
        //    subjects.ToDictionary(s => s.SubjectId),
        //    adjacencyConstraints.ToDictionary(a => a.SubjectId)
        //);
        //        Console.WriteLine("\nFinal violations:");
        //        foreach (var v in finalReport.InvariantViolations)
        //            Console.WriteLine(" - " + v);

        // --------------------------------------------------
        // 6. PRINT FINAL TIMETABLE
        // --------------------------------------------------
        Console.WriteLine("\nFinal timetable:");
        foreach (var s in state.Slots
            .OrderBy(s => s.Day)
            .ThenBy(s => s.StartTime))
        {
            Console.WriteLine(
                $"[{s.Day}] {s.StartTime:hh\\:mm}-{s.EndTime:hh\\:mm} : {s.SubjectName} :{s.SubjectId}"
            );
        }
    }

   
}