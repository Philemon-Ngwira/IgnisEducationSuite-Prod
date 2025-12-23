using System;
using System.Collections.Generic;
using System.Linq;
using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;

namespace SchedulingTester
{
    class Program
    {
        static void Main()
        {
            // --- 1. Define Subjects ---
            var mathId = Guid.NewGuid();
            var physicsId = Guid.NewGuid();
            var chemistryId = Guid.NewGuid();
            var englishId = Guid.NewGuid();
            var historyId = Guid.NewGuid();

            // --- 2. Define Teachers ---
            var mathTeacherId = Guid.NewGuid();
            var physicsTeacherId = Guid.NewGuid();
            var chemistryTeacherId = Guid.NewGuid();
            var englishTeacherId = Guid.NewGuid();
            var historyTeacherId = Guid.NewGuid();

            // --- 3. Define Schedules with Teacher assignments ---
            var schedules = new List<SubjectScheduleConfig>
            {
                new() { SubjectId = mathId, SubjectName = "Math", WeeklyPeriods = 5, DoublePeriods = 2, TeacherId = mathTeacherId },
                new() { SubjectId = physicsId, SubjectName = "Physics", WeeklyPeriods = 0, DoublePeriods = 1, TeacherId = physicsTeacherId },
                new() { SubjectId = chemistryId, SubjectName = "Chemistry", WeeklyPeriods = 0, DoublePeriods = 1, TeacherId = chemistryTeacherId },
                new() { SubjectId = englishId, SubjectName = "English", WeeklyPeriods = 0, DoublePeriods = 1, TeacherId = englishTeacherId },
                new() { SubjectId = historyId, SubjectName = "History", WeeklyPeriods = 0, DoublePeriods = 1, TeacherId = historyTeacherId }
            };

            // --- 4. Define Activities ---
            var activities = new List<TimeTableActivity>
            {
                new() { ActivityID = Guid.NewGuid(), ActivityName = "Sports" },
                new() { ActivityID = Guid.NewGuid(), ActivityName = "Music" },
                new() { ActivityID = Guid.NewGuid(), ActivityName = "Art" }
            };

            // --- 5. Define Time Slots (Mon-Fri, 7am-4pm) ---
            var timeSlots = new List<TimeSlot>();
            for (int day = 1; day <= 5; day++) // Monday=1 .. Friday=5
            {
                for (int hour = 7; hour < 16; hour++)
                {
                    timeSlots.Add(new TimeSlot
                    {
                        Day = (DayOfWeek)day,
                        StartTime = new TimeSpan(hour, 0, 0),
                        EndTime = new TimeSpan(hour + 1, 0, 0),
                        ScheduledActivityId = Guid.Empty
                    });
                }
            }

            // --- 6. Define Teacher Constraints ---
            var teacherConstraints = new List<TeacherScheduleConstraints>
            {
                new()
                {
                    TeacherId = mathTeacherId,
                    MaxDailyPeriods = 2,
                    UnavailableSlots = timeSlots.Where(ts => ts.StartTime.Value.Hours == 10).ToList()
                },
                new()
                {
                    TeacherId = physicsTeacherId,
                    MaxDailyPeriods = 1,
                    UnavailableSlots = timeSlots.Where(ts => ts.StartTime.Value.Hours == 7 || ts.StartTime.Value.Hours == 8).ToList()
                },
                new() { TeacherId = chemistryTeacherId, MaxDailyPeriods = 2 },
                new() { TeacherId = englishTeacherId, MaxDailyPeriods = 2 },
                new() { TeacherId = historyTeacherId, MaxDailyPeriods = 2 }
            };

            // --- 7. Define Adjacency Rules ---
            var adjacencyRules = new List<SubjectAdjacencyConstraints>
            {
                new() { SubjectId = mathId, CannotFollowSubjects = new List<Guid> { physicsId, chemistryId } },
                new() { SubjectId = physicsId, CannotFollowSubjects = new List<Guid> { mathId } },
                new() { SubjectId = chemistryId, CannotFollowSubjects = new List<Guid> { mathId, physicsId } }
            };

            // --- 8. Define Time Rules ---
            var timeRules = new List<SubjectTimeConstraints>
            {
                new() { SubjectId = mathId, MustBeMorning = true },
                new() { SubjectId = physicsId, MustBeMorning = true },
                new() { SubjectId = chemistryId, MustBeMorning = true }
            };

            // --- 9. Generate timetable ---
            var generator = new TimetableGenerator();
            var result = generator.Generate(timeSlots, schedules, adjacencyRules, timeRules, activities, teacherConstraints);

            if (result.Success)
            {
                Console.WriteLine("TEST 3B PASSED: Timetable generated successfully!\n");
                generator.PrintTimetable();
            }
            else
            {
                Console.WriteLine("TEST 3B FAILED:");
                Console.WriteLine(result.GetErrorMessage());
            }
        }
    }
}
