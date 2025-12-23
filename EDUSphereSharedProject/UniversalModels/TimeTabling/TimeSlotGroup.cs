using EDUSphereSharedProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class TimeSlotGroup
    {
        public TimeSlot TimeSlot { get; set; } = default!;
        public List<ClassSchedule> Slots { get; set; } = new();
    }
}
