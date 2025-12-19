using EDUSphereSharedProject.Models;

namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class SlotState
    {
        public TimeSlot Slot { get; set; }
        public Guid? SubjectId { get; set; }
        public bool IsLocked { get; set; } // already present

        // New additions
        public bool IsFiller { get; set; } = false; // marks auto-filled single periods
        public Guid? ScheduledActivityId { get; set; } // for Prep / other activities
    }


}
