using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class TimetableValidationRequest
    {
        public List<GeneratedSlotPreview> Slots { get; set; } = new();

        public Dictionary<Guid, SubjectScheduleConfig> Subjects { get; set; } = new();

        public Dictionary<Guid, SubjectAdjacencyConstraints> Adjacency { get; set; } = new();
    }
}
