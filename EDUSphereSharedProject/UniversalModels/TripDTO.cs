using EDUSphereSharedProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class TripDTO
    {
        public Guid tripId {  get; set; }
        public string TripName { get; set; }

        public BusStaff Driver {  get; set; }
        public BusStaff Attendant { get; set; }

    }
}
