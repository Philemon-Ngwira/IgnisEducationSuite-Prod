using EDUSphereSharedProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class GeneratedScheduleSlot
    {
        // The day of the week for this slot
        public DayOfWeek Day { get; set; }

        // List of slots for this day
        public List<SlotInfo> Slots { get; set; } = new();

        public class SlotInfo
        {
            // The time slot info
            public TimeSlot Slot { get; set; } = new();

            // Assigned subject (if any)
            public Guid? SubjectId { get; set; }

            // Scheduled activity (if a filler/activity is assigned)
            public Guid? ScheduledActivityId { get; set; }
        }
    }
}
