using EDUSphereSharedProject.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class SlotState
    {
        public TimeSlot Slot { get; set; }
        public Guid? SubjectId { get; set; }
        public Guid ScheduledActivityId { get; set; }
        public bool IsLocked { get; set; } = false;
        public bool IsFiller { get; set; } = false;
        public bool ReservedForActivity { get; set; } = false;
        public DayOfWeek Day { get; init; }
        public string ActivityName { get; set; }
        public string SubjectName { get; set; }
        // Helper: optional reference to previous slot in the same day
        public SlotState? PreviousInDay;
    }


}
