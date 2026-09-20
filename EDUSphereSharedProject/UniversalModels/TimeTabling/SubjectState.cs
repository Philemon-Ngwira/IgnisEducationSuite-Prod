using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class SubjectState
    {
        public SubjectScheduleConfig Config { get; init; }

        public int RemainingPeriods { get; set; }
        public int RemainingDoubles { get; set; }

        public Dictionary<DayOfWeek, int> DailyCount { get; } = new();
    }
}
