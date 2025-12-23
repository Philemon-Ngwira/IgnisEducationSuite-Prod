using EDUSphereSharedProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class GeneratedSlotPreview
    {
        public Guid? ClassId { get; set; } // Optional: needed if you have multiple classes
        public Guid? SubjectId { get; set; }
        public Guid? ScheduledActivityId { get; set; }

        public string? SubjectName { get; set; } // Optional, for display
        public string? ActivityName { get; set; } // Optional, for fillers
        public string TeacherName { get; set; } = "";

        public TimeSlot Slot { get; set; } = null!;
        public string DayOfWeek { get; set; } = ""; // Optional but useful for grouping
    }
}
