using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingTester.TimeTableGenerator
{
    public class TimetableReport
    {
        public List<string> InvariantViolations { get; } = new();
        public Dictionary<string, int> Metrics { get; } = new();

        public bool IsValid => InvariantViolations.Count == 0;
    }
}
