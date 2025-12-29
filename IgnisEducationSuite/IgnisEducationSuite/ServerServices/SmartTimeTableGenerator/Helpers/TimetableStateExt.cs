using EDUSphereSharedProject.Models;
using System.Collections.Generic;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public partial class TimetableState
    {
        // ---------- MUTATORS FOR REPAIR PASS ----------

        public void PlaceSubject(TimeSlot slot, Guid subjectId)
        {
            slot.SubjectId = subjectId;
            if (subjectId == Guid.Empty)
            {
                slot.SubjectName = "Free"; // or prepActivity.Name
            }
            else
            {
                slot.SubjectName = Subjects[subjectId].SubjectName;
            }

            // update weekly and daily counts
            _weeklyCount.TryAdd(subjectId, 0);
            _weeklyCount[subjectId]++;

            var key = (slot.Day, subjectId);
            _dailyCount.TryAdd(key, 0);
            _dailyCount[key]++;
        }

        public void RemoveSubject(TimeSlot slot)
        {
            if (slot.SubjectId == Guid.Empty) return;

            var subjectId = slot.SubjectId;

            _weeklyCount[subjectId]--;
            var key = (slot.Day, subjectId);
            _dailyCount[key]--;

            slot.SubjectId = Guid.Empty;
            slot.SubjectName = "Free";
        }

        // ---------- CHECKS ----------

        public bool CanPlaceSubject(DayOfWeek day, Guid subjectId, TimeSpan? nextSlotStart = null)
        {
            // Daily max
            if (DailyCount(day, subjectId) >= 2)
                return false;

            // Optional adjacency check if nextSlotStart is given
            if (nextSlotStart.HasValue)
            {
                var daySlots = SlotsForDay(day).OrderBy(s => s.StartTime).ToList();
                for (int i = 0; i < daySlots.Count - 1; i++)
                {
                    var current = daySlots[i];
                    var next = daySlots[i + 1];

                    if (current.EndTime != nextSlotStart) continue;

                    if (ViolatesAdjacency(current, new TimeSlot { SubjectId = subjectId }))
                        return false;
                }
            }

            // Weekly remaining
            return WeeklyRemaining(subjectId) > 0;
        }
    }
}
