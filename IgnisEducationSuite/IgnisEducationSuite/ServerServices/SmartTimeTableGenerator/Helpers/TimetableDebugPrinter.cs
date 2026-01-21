using EDUSphereSharedProject.Models;
using System;
using System.Linq;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public static class TimetableDebugPrinter
    {
        public static void Print(string title, TimetableState state)
        {
            Console.WriteLine();
            Console.WriteLine("=================================================");
            Console.WriteLine(title);
            Console.WriteLine("=================================================");

            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                Console.WriteLine($"\n{day}:");

                var slots = state.SlotsForDay(day)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                foreach (var slot in slots)
                {
                    string label;

                    if (slot.IsActivity())
                        label = $"[ACTIVITY: {slot.SubjectName}]";
                    else if (slot.SubjectId == Guid.Empty)
                        label = "[Free]";
                    else
                        label = slot.SubjectName;

                    Console.WriteLine(
                        $"  {slot.StartTime:hh\\:mm}-{slot.EndTime:hh\\:mm} -> {label}");
                }
            }

            Console.WriteLine("\n=================================================\n");
        }
    }
}
