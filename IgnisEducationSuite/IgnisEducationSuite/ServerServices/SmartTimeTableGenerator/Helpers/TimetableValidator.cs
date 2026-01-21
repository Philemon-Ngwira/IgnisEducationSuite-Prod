using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;
using Org.BouncyCastle.Utilities;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimetableValidator
    {
        private Dictionary<Guid, SubjectScheduleConfig> subjectsList = new Dictionary<Guid, SubjectScheduleConfig>();

      
        public TimetableReportDto AnalyzeGenerated(
    List<GeneratedSlotPreview> slots,
    Dictionary<Guid, SubjectScheduleConfig> subjects,
    Dictionary<Guid, SubjectAdjacencyConstraints> adjacency)
        {
            var state = TimetableStateFactory.FromGeneratedSlots(slots, subjects, adjacency);
            return Analyze(state, subjects, adjacency);
        }
        private static string SubjectName(
    Guid subjectId,
    Dictionary<Guid, SubjectScheduleConfig> subjects)
        {
            return subjects.TryGetValue(subjectId, out var s)
                ? s.SubjectName
                : $"Unknown Subject ({subjectId})";
        }

        public TimetableReportDto Analyze(
     TimetableState state,
     Dictionary<Guid, SubjectScheduleConfig> subjects,
     Dictionary<Guid, SubjectAdjacencyConstraints> adjacency)
        {
            var report = new TimetableReportDto();
            subjectsList = subjects;
            CheckDailyLimits(state, report);
            CheckRequiredDoubles(state, subjects, report);
            CheckEarlyMorningRules(state, subjects, report);
            CheckAdjacency(state, adjacency, report, subjects);

            CollectMetrics(state, report);

            return report;
        }
        void CheckDailyLimits(TimetableState state, TimetableReportDto report)
        {
            foreach (var day in Enum.GetValues<DayOfWeek>())
            {
                var slots = state.SlotsForDay(day).ToList();

                var grouped = slots
                    .Where(s => s.SubjectId != Guid.Empty && s.IsAcademic())
                    .GroupBy(s => s.SubjectId);

                foreach (var g in grouped)
                {
                    if (g.Count() > 2)
                        report.InvariantViolations.Add(
                           $"{day}: {SubjectName(g.Key, subjectsList)} appears {g.Count()} times"
);

                    if (g.Count() == 2)
                    {
                        var ordered = g.OrderBy(s => s.StartTime).ToList();
                        if (!ordered[0].EndTime.Equals(ordered[1].StartTime))
                            report.InvariantViolations.Add(
                               $"{day}: {SubjectName(g.Key, subjectsList)} is split (not a double)"
);
                    }
                }
            }
        }
        void CheckRequiredDoubles(
     TimetableState state,
     Dictionary<Guid, SubjectScheduleConfig> subjects,
     TimetableReportDto report)
        {
            var validSubjectIds = subjects.Keys.ToHashSet();

            foreach (var subject in subjects.Values)
            {
                if (subject.RequiredDoubles <= 0)
                    continue;

                int doublesFound = 0;

                foreach (var day in Enum.GetValues<DayOfWeek>())
                {
                    var slots = state.SlotsForDay(day)
                        .Where(s =>
                            s.SubjectId == subject.SubjectId &&           // match subject
                           s.IsAcademic())        // 🚫 exclude activities
                        .OrderBy(s => s.StartTime)
                        .ToList();

                    // Count consecutive pairs
                    for (int i = 0; i < slots.Count - 1; i++)
                    {
                        if (slots[i].EndTime == slots[i + 1].StartTime)
                            doublesFound++;
                    }
                }

                if (doublesFound < subject.RequiredDoubles)
                {
                    report.InvariantViolations.Add(
                        $"Subject {subject.SubjectName} missing required doubles " +
                        $"(required: {subject.RequiredDoubles}, found: {doublesFound})");
                }
            }
        }

        void CheckEarlyMorningRules(
            TimetableState state,
            Dictionary<Guid, SubjectScheduleConfig> subjects,
            TimetableReportDto report)
        {
            foreach (var slot in state.Slots)
            {
                if (slot.SubjectId == Guid.Empty)
                    continue;
                if (!slot.IsAcademic())
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
    TimetableReportDto report, Dictionary<Guid, SubjectScheduleConfig> subjects)
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

                    if (!prev.IsAcademic() || !curr.IsAcademic())
                        continue;

                    if (!adjacency.ContainsKey(curr.SubjectId))
                        continue; // <-- ignore subjects with no constraints

                    if (adjacency[curr.SubjectId].CannotFollowSubjects.Contains(prev.SubjectId))
                    {
                        report.InvariantViolations.Add(
                           $"{day}: {SubjectName(curr.SubjectId, subjects)} cannot follow {SubjectName(prev.SubjectId, subjects)}"
);
                    }

                }
            }
        }
        void CollectMetrics(TimetableState state, TimetableReportDto report)
        {
            report.Metrics["FreeSlots"] =
                state.Slots.Count(s => s.SubjectId == Guid.Empty);

            report.Metrics["TotalSlots"] =
                state.Slots.Count();

            report.Metrics["ScheduledSlots"] =
                report.Metrics["TotalSlots"] - report.Metrics["FreeSlots"];
        }

    }

    public static class TimetableStateFactory
    {
        public static TimetableState FromGeneratedSlots(
            List<GeneratedSlotPreview> slots,
            Dictionary<Guid, SubjectScheduleConfig> subjects,
            Dictionary<Guid, SubjectAdjacencyConstraints> adjacency)
        {
            var timeSlots = slots.Select(slot => new TimeSlot
            {
                Day = Enum.Parse<DayOfWeek>(slot.DayOfWeek),
                StartTime = slot.Slot.StartTime,
                EndTime = slot.Slot.EndTime,
                SubjectId = slot.SubjectId ?? Guid.Empty
            }).ToList();

            return new TimetableState(
                timeSlots,
                subjects.Values.ToList(),
                adjacency.Values.ToList()
            );
        }
    }

}
