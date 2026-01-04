using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class TimetableValidationResult
    {
        public bool IsValid => !Violations.Any();
        public List<TimetableViolation> Violations { get; set; } = new();
    }
    public class TimetableViolation
    {
        public string Code { get; set; } = "";
        public string Message { get; set; } = "";

        public string DayOfWeek { get; set; } = "";
        public TimeSpan StartTime { get; set; }

        public Guid? SubjectId { get; set; }
    }

}
