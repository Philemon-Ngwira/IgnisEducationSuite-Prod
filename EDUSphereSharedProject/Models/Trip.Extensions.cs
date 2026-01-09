using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class Trip
    {
        [NotMapped]
        public List<string> TripDays { get; set; }

        [NotMapped]
        public BusStaff Driver { get; set; }
        [NotMapped]
        public BusStaff Attendant { get; set; }
        [NotMapped]
        public List<TripBooking> bookings { get; set; }
        [NotMapped]
        public List<TripAttendance> attendances { get; set; }
        [NotMapped]
        public BusRoute tripRoute { get; set; }
        [NotMapped]
        public Bus Bus { get; set; }
    }
}
