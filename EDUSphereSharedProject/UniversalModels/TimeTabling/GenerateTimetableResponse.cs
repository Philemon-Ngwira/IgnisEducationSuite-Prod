using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class GenerateTimetableResponse
    {
        public bool Success { get; set; }
        public string? Error { get; set; }


        public List<GeneratedSlotPreview>? Slots { get; set; }
    }
}
