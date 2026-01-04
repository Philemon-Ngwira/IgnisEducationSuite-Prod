using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class TimetableRow
    {
        public string TimeLabel { get; set; } = "";
        public List<GeneratedSlotPreview> Cells { get; set; } = new();
        public PlaceableType Type { get; set; } // Subject or Activity
    }
    public enum PlaceableType
    {
        Subject,
        Activity
    }
}
