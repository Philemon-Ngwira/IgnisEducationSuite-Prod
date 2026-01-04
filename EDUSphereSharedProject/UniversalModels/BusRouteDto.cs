using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class BusRouteDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<BusStopDto> BusStops { get; set; } = new();
    }
    public class BusStopDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Order { get; set; }
    }
}
