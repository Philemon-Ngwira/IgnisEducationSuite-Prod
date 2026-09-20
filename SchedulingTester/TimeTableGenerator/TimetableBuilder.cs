using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingTester.TimeTableGenerator
{
    public class TimetableBuilder
    {
        private readonly HashSet<Guid> _coreSubjects;

        public TimetableBuilder(HashSet<Guid> coreSubjects)
        {
            _coreSubjects = coreSubjects;
        }

        /// <summary>
        /// Runs a full deterministic pipeline: repair hard constraints, then optimize soft preferences.
        /// </summary>
        public void Build(TimetableState state, Activity? prepActivity = null)
        {
            // -------- STAGE 2: REPAIR (HARD CONSTRAINTS) --------
            var repair = new TimeTableRepair();
            repair.Repair(state, prepActivity);

            // -------- STAGE 3: OPTIMIZATION (SOFT CONSTRAINTS) --------
            Optimize(state);
        }

        /// <summary>
        /// Stage 3 soft-constraint optimization.
        /// Swaps slots deterministically if the score improves.
        /// Only swaps safe slots (not required doubles, not early-morning-only violations, not adjacency violations).
        /// </summary>
        private void Optimize(TimetableState state)
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

                            // Only consider swapping free or safe slots
                            if (!TimetableMoves.CanSwap(a, b, state))
                                continue;

                            // Apply swap
                            TimetableMoves.Swap(a, b);
                            int newScore = TimetableScorer.Score(state, _coreSubjects);

                            if (newScore > baseScore)
                            {
                                // Keep swap
                                improved = true;
                                baseScore = newScore;
                                goto NEXT_ITERATION;
                            }

                            // Revert swap
                            TimetableMoves.Swap(a, b);
                        }
                    }
                }

            NEXT_ITERATION:
                ;
            } while (improved);
        }
    }

  
}
