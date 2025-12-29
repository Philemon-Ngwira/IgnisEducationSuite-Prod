using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimetableValidator
    {
        public TimetableReport Analyze(
     TimetableState state,
     Dictionary<Guid, SubjectScheduleConfig> subjects,
     Dictionary<Guid, SubjectAdjacencyConstraints> adjacency)
        {
            var report = new TimetableReport();

            CheckDailyLimits(state, report);
            CheckRequiredDoubles(state, subjects, report);
            CheckEarlyMorningRules(state, subjects, report);
            CheckAdjacency(state, adjacency, report);

            CollectMetrics(state, report);

            return report;
        }
        void CheckDailyLimits(TimetableState state, TimetableReport report)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var slots = state.SlotsForDay(day).ToList();

                var grouped = slots
                    .Where(s => s.SubjectId != Guid.Empty)
                    .GroupBy(s => s.SubjectId);

                foreach (var g in grouped)
                {
                    if (g.Count() > 2)
                        report.InvariantViolations.Add(
                            $"{day}: Subject {g.Key} appears {g.Count()} times");

                    if (g.Count() == 2)
                    {
                        var ordered = g.OrderBy(s => s.StartTime).ToList();
                        if (!ordered[0].EndTime.Equals(ordered[1].StartTime))
                            report.InvariantViolations.Add(
                                $"{day}: Subject {g.Key} is split (not a double)");
                    }
                }
            }
        }
        void CheckRequiredDoubles(
    TimetableState state,
    Dictionary<Guid, SubjectScheduleConfig> subjects,
    TimetableReport report)
        {
            foreach (var subject in subjects.Values)
            {
                if (subject.RequiredDoubles <= 0)
                    continue;

                int doublesFound = 0;

                foreach (var day in Enum.GetValues<DayOfWeek>())
                {
                    var slots = state.SlotsForDay(day)
                        .Where(s => s.SubjectId == subject.SubjectId)
                        .OrderBy(s => s.StartTime)
                        .ToList();

                    if (slots.Count == 2 &&
                        slots[0].EndTime == slots[1].StartTime)
                        doublesFound++;
                }

                if (doublesFound < subject.RequiredDoubles)
                    report.InvariantViolations.Add(
                        $"Subject {subject.SubjectName} missing required doubles");
            }
        }
        void CheckEarlyMorningRules(
            TimetableState state,
            Dictionary<Guid, SubjectScheduleConfig> subjects,
            TimetableReport report)
        {
            foreach (var slot in state.Slots)
            {
                if (slot.SubjectId == Guid.Empty)
                    continue;

                var subject = subjects[slot.SubjectId];
                if (subject.EarlyMorningOnly &&
                    slot.StartTime >= TimeSpan.FromHours(10.5))
                {
                    report.InvariantViolations.Add(
                        $"{subject.SubjectName} placed late on {slot.Day}");
                }
            }
        }
        void CheckAdjacency(
    TimetableState state,
    Dictionary<Guid, SubjectAdjacencyConstraints> adjacency,
    TimetableReport report)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var slots = state.SlotsForDay(day)
                    .OrderBy(s => s.StartTime)
                    .ToList();

                for (int i = 1; i < slots.Count; i++)
                {
                    var prev = slots[i - 1];
                    var curr = slots[i];

                    if (prev.SubjectId == Guid.Empty || curr.SubjectId == Guid.Empty)
                        continue;


                    if (!adjacency.ContainsKey(curr.SubjectId))
                        continue; // <-- ignore subjects with no constraints

                    if (adjacency[curr.SubjectId].CannotFollowSubjects.Contains(prev.SubjectId))
                    {
                        report.InvariantViolations.Add(
                            $"{day}: {curr.SubjectId} cannot follow {prev.SubjectId}");
                    }

                }
            }
        }
        void CollectMetrics(TimetableState state, TimetableReport report)
        {
            report.Metrics["FreeSlots"] =
                state.Slots.Count(s => s.SubjectId == Guid.Empty);

            report.Metrics["TotalSlots"] =
                state.Slots.Count();

            report.Metrics["ScheduledSlots"] =
                report.Metrics["TotalSlots"] - report.Metrics["FreeSlots"];
        }

    }
}
