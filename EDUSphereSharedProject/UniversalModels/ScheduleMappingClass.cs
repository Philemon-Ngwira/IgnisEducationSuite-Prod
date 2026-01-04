using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ScheduleMappingClass
    {
        public string Class { get; set; }
        public int GradeLevel { get; set; }
        public string GradeSection { get; set; }
        public string Day { get; set; }

        public string StartTime { get; set; }

        public string EndTime { get; set; }

        public Guid? ScheduledActivityId { get;set; }
    }
}
