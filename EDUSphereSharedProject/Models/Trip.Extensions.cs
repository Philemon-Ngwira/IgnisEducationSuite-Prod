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
        public List<string> TripDays { get; set; } = new List<string>();

        [NotMapped]
        public BusStaff Driver { get; set; } = new();
        [NotMapped]
        public BusStaff Attendant { get; set; } = new();
        [NotMapped]
        public List<TripBooking> bookings { get; set; } = new List<TripBooking>();
        [NotMapped]
        public List<TripAttendance> attendances { get; set; } = new List<TripAttendance>();
        [NotMapped]
        public BusRoute tripRoute { get; set; } = new();
        [NotMapped]
        public Bus Bus { get; set; } = new();
    }
}
