using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ActivitySchedulePreference
    {
        public Guid ActivityId { get; set; }
        public ActivityTimePreference TimePreference { get; set; }
        public bool IsFixed { get; set; } = false; // optional but powerful
    }
    public enum ActivityTimePreference
    {
        Any = 0,
        MorningOnly = 1,        // before 10:00
        MidMorning = 2,         // 10:00+
        AfternoonOnly = 3,
        None = 4 // e.g. after 13:00
    }

}
