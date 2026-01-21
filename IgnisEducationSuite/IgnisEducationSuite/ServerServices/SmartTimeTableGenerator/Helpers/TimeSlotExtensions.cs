using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public static class TimeSlotExtensions
    {
        public static bool IsActivity(this TimeSlot slot) =>
            slot.SubjectId == Guid.Empty &&
            slot.ScheduledActivityId != null &&
            slot.IsLocked;

        public static bool IsAcademic(this TimeSlot slot) =>
            slot.SubjectId != Guid.Empty;
    }

}
