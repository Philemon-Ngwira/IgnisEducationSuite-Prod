namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public static class TimetableScorer
    {
        public static int Score(
      TimetableState state,
      HashSet<Guid> coreSubjects)
        {
            int score = 0;

            // --- Free slots & holes ---
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var daySlots = state.SlotsForDay(day).ToList();

                for (int i = 0; i < daySlots.Count; i++)
                {
                    if (daySlots[i].SubjectId == Guid.Empty)
                    {
                        score -= 10;

                        bool isHole =
                            i > 0 &&
                            i < daySlots.Count - 1 &&
                            daySlots[i - 1].SubjectId != Guid.Empty &&
                            daySlots[i + 1].SubjectId != Guid.Empty;

                        if (isHole)
                            score -= 10;
                    }
                }
            }

            // --- Subject spread ---
            var bySubject = state.Slots
                .Where(s => s.SubjectId != Guid.Empty)
                .GroupBy(s => s.SubjectId);

            foreach (var group in bySubject)
            {
                int distinctDays = group
                    .Select(s => s.Day)
                    .Distinct()
                    .Count();

                score += distinctDays * 5;
            }

            // --- Morning preference for core subjects ---
            foreach (var slot in state.Slots)
            {
                if (slot.SubjectId != Guid.Empty &&
                    coreSubjects.Contains(slot.SubjectId))
                {
                    if (slot.StartTime < TimeSpan.FromHours(10.5))
                        score += 5;

                    if (slot.StartTime > TimeSpan.FromHours(14))
                        score -= 5;
                }
            }

            return score;
        }
    }
}
