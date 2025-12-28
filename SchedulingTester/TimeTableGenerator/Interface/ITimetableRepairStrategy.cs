using IgnisEducationSuite.ServerServices.SmartTimeTableGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchedulingTester.TimeTableGenerator.Interface
{
    interface ITimetableRepairStrategy
    {
        bool CanApply(TimetableState state);
        bool TryApply(TimetableState state);
    }

}
