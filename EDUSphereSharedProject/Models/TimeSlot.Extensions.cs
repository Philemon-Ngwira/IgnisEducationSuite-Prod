using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class TimeSlot
    {
        [NotMapped]
        public DayOfWeek Day { get; set; }
        [NotMapped]
        public Guid? ScheduledActivityId { get; set; }
        [NotMapped]
        public TimeSpan? SchoolMorningEnd { get; set; }     // e.g. 10:30
        [NotMapped]
        public TimeSpan? SchoolEarlyMorningEnd { get; set; } // e.g. 09:50
        [NotMapped]
        public TimeSpan? SchoolAfternoonStart { get; set; } // e.g. 12:50

        [NotMapped]
        public Guid SubjectId { get; set; }
        [NotMapped]
        public string SubjectName { get; set; }
    }
}
