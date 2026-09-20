using EDUSphereSharedProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class GenerateTimetableRequest
    {
        public int Grade {  get; set; }
        public string GradeName { get; set; } 
        public List<TimeSlot> TimeSlots { get; set; } = new();
        public List<SubjectScheduleConfig> Schedules { get; set; } = new();
        public List<SubjectAdjacencyConstraints> AdjacencyRules { get; set; } = new();
        public List<SubjectTimeConstraints> TimeRules { get; set; } = new();
        public List<TimeTableActivity> Activities { get; set; } = new();
        public List<SubjectStructureConstraints> structureConstraints { get; set; } = new();

        public List<Class> classes { get; set; } = new();

    }
}
