using System;
using System.Collections.Generic;
using System.Linq;

namespace SchedulingTester.TimeTableGenerator
{
    public class TimetableOptimizer
    {
        private readonly HashSet<Guid> _coreSubjects;

        public TimetableOptimizer(HashSet<Guid> coreSubjects)
        {
            _coreSubjects = coreSubjects;
        }

        public void Optimize(TimetableState state)
        {
            bool improved;
            int iteration = 0;

            do
            {
                improved = false;
                iteration++;

                int baseScore = TimetableScorer.Score(state, _coreSubjects);

                foreach (var day in Enum.GetValues<DayOfWeek>())
                {
                    var daySlots = state.SlotsForDay(day).ToList();

                    for (int i = 0; i < daySlots.Count; i++)
                    {
                        for (int j = i + 1; j < daySlots.Count; j++)
                        {
                            var a = daySlots[i];
                            var b = daySlots[j];

                            if (!TimetableMoves.CanSwap(a, b, state))
                                continue;

                            if (ViolatesStage2Constraints(state, a, b))
                                continue;

                            // perform swap
                            TimetableMoves.Swap(a, b);
                            int newScore = TimetableScorer.Score(state, _coreSubjects);

                            if (newScore > baseScore)
                            {
                                improved = true;
                                baseScore = newScore;
                                goto NEXT_ITERATION;
                            }

                            // revert
                            TimetableMoves.Swap(a, b);
                        }
                    }
                }

            NEXT_ITERATION:;
            }
            while (improved);
        }

        private bool ViolatesStage2Constraints(TimetableState state, TimeSlot a, TimeSlot b)
        {
            // ---- 1. Required doubles must remain atomic ----
            if (state.RequiredDoublesRemaining(a.SubjectId) > 0 && state.CountPlacedDoubles(a.SubjectId) < state.Subjects[a.SubjectId].RequiredDoubles)
                return true;
            if (state.RequiredDoublesRemaining(b.SubjectId) > 0 && state.CountPlacedDoubles(b.SubjectId) < state.Subjects[b.SubjectId].RequiredDoubles)
                return true;

            // ---- 2. Early-morning subjects must stay early ----
            if (state.Subjects[a.SubjectId].EarlyMorningOnly && a.StartTime >= TimeSpan.FromHours(10.5))
                return true;
            if (state.Subjects[b.SubjectId].EarlyMorningOnly && b.StartTime >= TimeSpan.FromHours(10.5))
                return true;

            // ---- 3. Adjacency rules must not be violated ----
            var daySlots = state.SlotsForDay(a.Day).OrderBy(s => s.StartTime).ToList();
            int aIndex = daySlots.IndexOf(a);
            int bIndex = daySlots.IndexOf(b);

            // Check adjacency for neighboring slots after swap
            var aPrev = aIndex > 0 ? daySlots[aIndex - 1] : null;
            var aNext = aIndex < daySlots.Count - 1 ? daySlots[aIndex + 1] : null;
            var bPrev = bIndex > 0 ? daySlots[bIndex - 1] : null;
            var bNext = bIndex < daySlots.Count - 1 ? daySlots[bIndex + 1] : null;

            // simulate swap
            if (aPrev != null && state.ViolatesAdjacency(aPrev, b)) return true;
            if (aNext != null && state.ViolatesAdjacency(b, aNext)) return true;
            if (bPrev != null && state.ViolatesAdjacency(bPrev, a)) return true;
            if (bNext != null && state.ViolatesAdjacency(a, bNext)) return true;

            return false;
        }
    }
}
