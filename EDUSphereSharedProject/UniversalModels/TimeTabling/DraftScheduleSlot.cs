using EDUSphereSharedProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class DraftScheduleSlot
    {
        public Guid DraftId { get; set; } = Guid.NewGuid();

        public DayofTheWeek Day { get; set; } = default!;
        public TimeSlot TimeSlot { get; set; } = default!;

        public Guid? SubjectId { get; set; }
        public string? SubjectName { get; set; }

        public Guid? ActivityId { get; set; }
        public string? ActivityName { get; set; }

        // Validation / repair state
        public bool IsValid { get; set; } = true;
        public List<string> Warnings { get; set; } = new();
    }
}
