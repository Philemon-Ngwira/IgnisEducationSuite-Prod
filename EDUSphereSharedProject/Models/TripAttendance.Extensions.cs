using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class TripAttendance
    {
        [NotMapped]
        public string StudentName { get; set; } = "";
        [NotMapped]
        public string AcademicLevel { get; set; } = "";
        [NotMapped]
        public byte[] ProfilePic { get; set; } = Array.Empty<byte>();
        [NotMapped]
        public string TripName { get; set; } = "";
        [NotMapped]
        public TimeSpan? DepartureTime { get; set; }
        [NotMapped]
        public DateTime TripDate { get; set; }
        [NotMapped]
        public TimeSpan? EstimatedArrivalTime { get; set; }
        [NotMapped]
        public Guid? RouteId { get; set; }
        [NotMapped]
        public Guid? BusId { get; set; }
    }
}
