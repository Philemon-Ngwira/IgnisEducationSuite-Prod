using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class TimeTableReportDTO
    {
        public List<string> InvariantViolations { get; set; } = new();
        public Dictionary<string, int> Metrics { get; set; } = new();

        public bool IsValid => InvariantViolations.Count == 0;
    }
}
