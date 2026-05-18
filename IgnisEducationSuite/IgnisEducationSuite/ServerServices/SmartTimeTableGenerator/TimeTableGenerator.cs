using EDUSphereSharedProject.Models;
using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableGenerator : ITimetableGenerator
    {
        private readonly Random _rand = new();
        private readonly Dictionary<(DayOfWeek, Guid), int> dailyCount = new();
        private TimeSpan EarlyMorningCutOFF = TimeSpan.Zero;


        public async Task<List<TimeSlot>> Generate(
            List<TimeSlot> slots,
            List<SubjectScheduleConfig> subjects,
            List<SubjectAdjacencyConstraints> adjacencyConstraints,
            TimeTableActivity? prepActivity = null,
            TeacherConflictChecker? teacherChecker = null)
        {
            await Task.Yield(); // Ensure async context for potential future DB calls in teacherChecker
            dailyCount.Clear();
            var prepareslots = BuildEmptyWeek(slots);
            EarlyMorningCutOFF = subjects.First().EarlyMorningEnd;

            var remainingPeriods = subjects.ToDictionary(s => s.SubjectId, s => s.WeeklyPeriods);
            var remainingDoubles = subjects.ToDictionary(s => s.SubjectId, s => s.RequiredDoubles);

            var earlySubjects = subjects.Where(s => s.EarlyMorningOnly).ToList();
            var normalSubjects = subjects.Where(s => !s.EarlyMorningOnly).ToList();

            var adjacencyDict = subjects.ToDictionary(
                s => s.SubjectId,
                s => adjacencyConstraints.FirstOrDefault(a => a.SubjectId == s.SubjectId)
                     ?? new SubjectAdjacencyConstraints { SubjectId = s.SubjectId }
            );

            // 1️⃣ LOCK ACTIVITIES
            LockActivities(prepareslots, prepActivity);

            var slotsByDay = prepareslots
                .GroupBy(s => s.Day)
                .ToDictionary(g => g.Key, g => g.OrderBy(s => s.StartTime).ToList());

            // 2️⃣ EARLY MORNING DOUBLES
            PlaceDoubles(slotsByDay, earlySubjects, remainingPeriods, remainingDoubles, adjacencyDict, morningOnly: true, teacherChecker);

            // 3️⃣ EARLY MORNING SINGLES
            PlaceSingles(slotsByDay, earlySubjects, remainingPeriods, adjacencyDict, morningOnly: true, teacherChecker);

            // 4️⃣ NORMAL DOUBLES
            PlaceDoubles(slotsByDay, normalSubjects, remainingPeriods, remainingDoubles, adjacencyDict, morningOnly: false, teacherChecker);

            // 5️⃣ NORMAL SINGLES
            PlaceSingles(slotsByDay, normalSubjects, remainingPeriods, adjacencyDict, morningOnly: false, teacherChecker);

#if DEBUG
            foreach (var subject in subjects)
            {
                var placed = prepareslots.Count(s => s.SubjectId == subject.SubjectId);
                if (placed > subject.WeeklyPeriods)
                    throw new InvalidOperationException(
                        $"{subject.SubjectName} overfilled: {placed}/{subject.WeeklyPeriods}");
            }
#endif

            return prepareslots;
        }

        // ------------------------------------------------
        // DOUBLES
        // ------------------------------------------------
        private void PlaceDoubles(
            Dictionary<DayOfWeek, List<TimeSlot>> slotsByDay,
            List<SubjectScheduleConfig> subjects,
            Dictionary<Guid, int> remainingPeriods,
            Dictionary<Guid, int> remainingDoubles,
            Dictionary<Guid, SubjectAdjacencyConstraints> adjacencyDict,
            bool morningOnly,
            TeacherConflictChecker teacherChecker)
        {
            foreach (var subject in subjects)
            {
                while (remainingDoubles[subject.SubjectId] > 0 &&
                       remainingPeriods[subject.SubjectId] >= 2)
                {
                    bool placed = false;

                    foreach (var day in slotsByDay.Keys)
                    {
                        if (GetDailyCount(day, subject.SubjectId) != 0)
                            continue;

                        var daySlots = slotsByDay[day];

                        for (int i = 0; i < daySlots.Count - 1; i++)
                        {
                            var a = daySlots[i];
                            var b = daySlots[i + 1];

                            if (!IsFree(a) || !IsFree(b)) continue;
                            if (a.EndTime != b.StartTime) continue;
                            if (morningOnly && a.StartTime >= EarlyMorningCutOFF) continue;

                            // ✅ Teacher conflict check for both slots of the double
                            if (teacherChecker.IsTeacherBusy(subject.TeacherId, day, a.StartTime.Value)) continue;
                            if (teacherChecker.IsTeacherBusy(subject.TeacherId, day, b.StartTime.Value)) continue;

                            if (remainingPeriods[subject.SubjectId] < 2) break;

                            Place(a, subject, remainingPeriods, day, teacherChecker);
                            Place(b, subject, remainingPeriods, day, teacherChecker);
                            remainingDoubles[subject.SubjectId]--;

                            placed = true;
                            break;
                        }

                        if (placed) break;
                    }

                    if (!placed) break;
                }
            }
        }

        // ------------------------------------------------
        // SINGLES
        // ------------------------------------------------
        private void PlaceSingles(
            Dictionary<DayOfWeek, List<TimeSlot>> slotsByDay,
            List<SubjectScheduleConfig> subjects,
            Dictionary<Guid, int> remainingPeriods,
            Dictionary<Guid, SubjectAdjacencyConstraints> adjacencyDict,
            bool morningOnly,
            TeacherConflictChecker teacherChecker)
        {
            foreach (var subject in subjects)
            {
                foreach (var day in slotsByDay.Keys)
                {
                    if (remainingPeriods[subject.SubjectId] <= 0) break;
                    if (GetDailyCount(day, subject.SubjectId) >= 2) continue;

                    foreach (var slot in slotsByDay[day])
                    {
                        if (remainingPeriods[subject.SubjectId] <= 0) break;
                        if (!IsFree(slot)) continue;
                        if (morningOnly && slot.StartTime >= EarlyMorningCutOFF) continue;

                        // ✅ Teacher conflict check
                        if (teacherChecker.IsTeacherBusy(subject.TeacherId, day, slot.StartTime.Value)) continue;

                        Place(slot, subject, remainingPeriods, day, teacherChecker);
                        break;
                    }
                }
            }
        }

        // ------------------------------------------------
        // HELPERS
        // ------------------------------------------------
        private static bool IsFree(TimeSlot slot) =>
            !slot.IsLocked && slot.SubjectId == Guid.Empty && slot.ScheduledActivityId == null;

        private void Place(
            TimeSlot slot,
            SubjectScheduleConfig subject,
            Dictionary<Guid, int> remainingPeriods,
            DayOfWeek day,
            TeacherConflictChecker teacherChecker)
        {
            if (remainingPeriods[subject.SubjectId] <= 0) return;

            // ✅ Final teacher conflict guard before committing
            if (teacherChecker.IsTeacherBusy(subject.TeacherId, day, slot.StartTime.Value))
                return;

            slot.SubjectId = subject.SubjectId;
            slot.SubjectName = subject.SubjectName;
            remainingPeriods[subject.SubjectId]--;
            IncrementDailyCount(day, subject.SubjectId);

            // ✅ Mark teacher as busy so no other subject/grade can clash
            teacherChecker.MarkBusy(subject.TeacherId, day, slot.StartTime.Value);
        }

        private List<TimeSlot> BuildEmptyWeek(List<TimeSlot> slots)
        {
            var list = new List<TimeSlot>();
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
                foreach (var s in slots)
                {
                    list.Add(new TimeSlot
                    {
                        Day = day,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        TimeslotID = s.TimeslotID,
                        SubjectId = Guid.Empty,
                        SubjectName = "Free"
                    });
                }
            }
            return list;
        }

        private void LockActivities(List<TimeSlot> slots, TimeTableActivity? activity)
        {
            if (activity == null) return;

            foreach (var slot in slots.Where(s =>
                s.StartTime >= activity.StartFrom &&
                activity.Days.Contains(s.Day)))
            {
                slot.IsLocked = true;
                slot.SubjectName = activity.ActivityName;
                slot.ScheduledActivityId = activity.ActivityID;
            }
        }

        private int GetDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount.TryGetValue((day, subjectId), out var c) ? c : 0;

        private void IncrementDailyCount(DayOfWeek day, Guid subjectId) =>
            dailyCount[(day, subjectId)] = GetDailyCount(day, subjectId) + 1;
    }
}