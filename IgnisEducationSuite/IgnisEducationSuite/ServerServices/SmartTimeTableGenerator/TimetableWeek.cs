using EDUSphereSharedProject.UniversalModels.TimeTabling;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>
    /// Builds the blank Mon–Fri board from a school's time slot definitions and stamps activity
    /// locks onto it. Runs before TimetableState exists, since the state indexes a board.
    /// </summary>
    public static class TimetableWeek
    {
        /// <summary>
        /// One GenerationSlot per (weekday, teaching slot). Non-teaching slots (BreakTime,
        /// LunchTime) are excluded outright, so no pass can place a subject into break or lunch.
        /// </summary>
        public static List<GenerationSlot> BuildEmpty(List<TimeSlotDto> baseSlots)
        {
            var teachingSlots = baseSlots
                .Where(s => s.SlotType is not null && SlotTypes.Teaching.Contains(s.SlotType))
                .OrderBy(s => s.StartTime)
                .ToList();

            var list = new List<GenerationSlot>();

            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                if (day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;

                foreach (var s in teachingSlots)
                {
                    list.Add(new GenerationSlot
                    {
                        Day = day,
                        StartTime = s.StartTime?.ToTimeSpan(),
                        EndTime = s.EndTime?.ToTimeSpan(),
                        TimeSlotId = s.TimeslotId,
                        SlotType = s.SlotType ?? "",
                        SubjectId = Guid.Empty,
                        SubjectName = "Free",
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// Locks each activity in one of two ways:
        ///  - PreferredTimeSlotId set: locks exactly that one slot, every weekday, or only on
        ///    PreferredDay if that's also set — for activities that occupy a single period
        ///    (Assembly, P.E.), rather than an entire half of the day.
        ///  - PreferredTimeSlotId unset: falls back to the whole-window blanket lock —
        ///    MustBeAfternoon -> [afternoonStart, end of day); MustBeMorning -> [start of day,
        ///    afternoonStart); neither flag set -> the whole day, every weekday (e.g. a blanket
        ///    "Prep" policy). Bounding morning-only activities to end BEFORE the afternoon (rather
        ///    than running to end of day) is what lets a morning activity and an afternoon activity
        ///    coexist in the same run.
        /// The caller is expected to have already validated there's no overlap between the selected
        /// activities (see SchedulingManagementOrchestrator.ValidateActivitySelection).
        /// </summary>
        public static void LockActivities(
            List<GenerationSlot> slots,
            List<TimeTableActivityDto> activities,
            TimeSpan afternoonStart)
        {
            foreach (var activity in activities)
            {
                IEnumerable<GenerationSlot> targetSlots;

                if (activity.PreferredTimeSlotId is not null)
                {
                    targetSlots = slots.Where(s =>
                        s.TimeSlotId == activity.PreferredTimeSlotId.Value &&
                        (activity.PreferredDay is null || s.Day == activity.PreferredDay.Value));
                }
                else
                {
                    var startFrom = activity.MustBeAfternoon ? afternoonStart : TimeSpan.Zero;
                    var endBefore = activity.MustBeMorning && !activity.MustBeAfternoon ? afternoonStart : (TimeSpan?)null;

                    targetSlots = slots.Where(s => s.StartTime >= startFrom && (endBefore is null || s.StartTime < endBefore));
                }

                foreach (var slot in targetSlots)
                {
                    slot.IsLocked = true;
                    slot.SubjectName = activity.ActivityName;
                    slot.ScheduledActivityId = activity.ActivityId;
                    slot.ActivityName = activity.ActivityName;
                }
            }
        }

        /// <summary>Start of the first Afternoon slot, or TimeSpan.MaxValue when the school defines
        /// none. Mirrors TimetableState.AfternoonStart, for callers that need it before a state exists.</summary>
        public static TimeSpan AfternoonStartOf(List<TimeSlotDto> baseSlots) =>
            baseSlots
                .Where(s => s.SlotType == SlotTypes.Afternoon && s.StartTime.HasValue)
                .Select(s => s.StartTime!.Value.ToTimeSpan())
                .DefaultIfEmpty(TimeSpan.MaxValue)
                .Min();
    }
}
