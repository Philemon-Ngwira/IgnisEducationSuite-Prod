using EDUSphereSharedProject.Models;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;

namespace SchedulingTester
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("RUNNING TEST 3...\n");

            // --- 1. Subject IDs ---
            var mathId = Guid.NewGuid();
            var physicsId = Guid.NewGuid();
            var chemistryId = Guid.NewGuid();
            var englishId = Guid.NewGuid();

            // --- 2. Subject Schedules ---
            var schedules = new List<SubjectScheduleConfig>
            {
                new()
                {
                    SubjectId = mathId,
                    SubjectName = "Math",
                    WeeklyPeriods = 5,
                    DoublePeriods = 2 // 2 doubles + 1 single
                },
                new()
                {
                    SubjectId = physicsId,
                    SubjectName = "Physics",
                    WeeklyPeriods = 3,
                    DoublePeriods = 0
                },
                new()
                {
                    SubjectId = chemistryId,
                    SubjectName = "Chemistry",
                    WeeklyPeriods = 3,
                    DoublePeriods = 0
                },
                new()
                {
                    SubjectId = englishId,
                    SubjectName = "English",
                    WeeklyPeriods = 5,
                    DoublePeriods = 0
                }
            };

            // --- 3. Activities (Filler) ---
            var activities = new List<TimeTableActivity>
            {
                new() { ActivityID = Guid.NewGuid(), ActivityName = "Sports" },
                new() { ActivityID = Guid.NewGuid(), ActivityName = "Music" }
            };

            // --- 4. Time Slots (Mon–Fri, 07:10–13:30) ---
            var timeSlots = new List<TimeSlot>();

            for (int day = 1; day <= 5; day++)
            {
                timeSlots.AddRange(new[]
                {
                    NewSlot(day, 7,10,7,50),
                    NewSlot(day, 7,50,8,30),
                    NewSlot(day, 8,30,9,10),
                    NewSlot(day, 9,10,9,50),
                    NewSlot(day, 9,50,10,30), // LAST MORNING SLOT
                    NewSlot(day,10,50,11,30),
                    NewSlot(day,12,50,13,30),
                });
            }

            // --- 5. Adjacency Rules ---
            var adjacencyRules = new List<SubjectAdjacencyConstraints>
            {
                new()
                {
                    SubjectId = mathId,
                    CannotFollowSubjects = new() { physicsId, chemistryId }
                },
                new()
                {
                    SubjectId = physicsId,
                    CannotFollowSubjects = new() { mathId, chemistryId }
                },
                new()
                {
                    SubjectId = chemistryId,
                    CannotFollowSubjects = new() { mathId, physicsId }
                }
            };

            // --- 6. Time Constraints (Morning = before 10:00) ---
            var timeRules = new List<SubjectTimeConstraints>
            {
                new()
                {
                    SubjectId = mathId,
                    MustBeMorning = true,
                    MorningEnd = new TimeSpan(10, 0, 0)
                },
                new()
                {
                    SubjectId = physicsId,
                    MustBeMorning = true,
                    MorningEnd = new TimeSpan(10, 0, 0)
                },
                new()
                {
                    SubjectId = chemistryId,
                    MustBeMorning = true,
                    MorningEnd = new TimeSpan(10, 0, 0)
                }
            };

            // --- 7. Generate ---
            var generator = new TimetableGenerator();
            var result = generator.Generate(
                timeSlots,
                schedules,
                adjacencyRules,
                timeRules,
                activities
            );

            if (result.Success)
            {
                Console.WriteLine("✅ TEST 3 PASSED: Timetable generated successfully!\n");
                generator.PrintTimetable();
            }
            else
            {
                Console.WriteLine("❌ TEST 3 FAILED:\n");
                Console.WriteLine(result.GetErrorMessage());
            }
        }

        static TimeSlot NewSlot(int day, int sh, int sm, int eh, int em)
        {
            return new TimeSlot
            {
                Day = (DayOfWeek)day,
                StartTime = new TimeSpan(sh, sm, 0),
                EndTime = new TimeSpan(eh, em, 0),
                ScheduledActivityId = Guid.Empty
            };
        }
    }
}


