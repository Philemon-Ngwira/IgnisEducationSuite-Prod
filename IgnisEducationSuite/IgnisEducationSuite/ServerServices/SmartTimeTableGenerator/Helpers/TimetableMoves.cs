using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public static class TimetableMoves
    {
        /// <summary>
        /// Determines if two slots can be swapped safely without breaking hard constraints.
        /// </summary>
        public static bool CanSwap(TimeSlot a, TimeSlot b, TimetableState state)
        {
            if (a.SubjectId == Guid.Empty || b.SubjectId == Guid.Empty)
                return true;

            var subA = state.Subjects[a.SubjectId];
            var subB = state.Subjects[b.SubjectId];

            // Early-morning-only: check target slot
            if (subA.EarlyMorningOnly && b.StartTime >= TimeSpan.FromHours(10.5)) return false;
            if (subB.EarlyMorningOnly && a.StartTime >= TimeSpan.FromHours(10.5)) return false;

            // Required doubles: avoid swapping slots that are part of required doubles
            if (state.RequiredDoublesRemaining(a.SubjectId) > 0 || state.RequiredDoublesRemaining(b.SubjectId) > 0)
                return false;

            // Adjacency check
            if (ViolatesAdjacencyAfterSwap(a, b, state)) return false;

            return true;
        }

        public static void Swap(TimeSlot a, TimeSlot b)
        {
            var tempId = a.SubjectId;
            var tempName = a.SubjectName;

            a.SubjectId = b.SubjectId;
            a.SubjectName = b.SubjectName;

            b.SubjectId = tempId;
            b.SubjectName = tempName;
        }

        private static bool ViolatesAdjacencyAfterSwap(TimeSlot a, TimeSlot b, TimetableState state)
        {
            var daySlots = state.SlotsForDay(a.Day).OrderBy(s => s.StartTime).ToList();

            // Check adjacency around a
            int indexA = daySlots.IndexOf(a);
            if (indexA > 0 && state.ViolatesAdjacency(daySlots[indexA - 1], a)) return true;
            if (indexA < daySlots.Count - 1 && state.ViolatesAdjacency(a, daySlots[indexA + 1])) return true;

            // Check adjacency around b
            int indexB = daySlots.IndexOf(b);
            if (indexB > 0 && state.ViolatesAdjacency(daySlots[indexB - 1], b)) return true;
            if (indexB < daySlots.Count - 1 && state.ViolatesAdjacency(b, daySlots[indexB + 1])) return true;

            return false;
        }
    }
}
