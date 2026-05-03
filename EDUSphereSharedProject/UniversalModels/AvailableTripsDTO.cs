using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class AvailableTripsDTO
    {
        public Guid TripId { get; set; }
        public string TripName { get; set; } = "";
        public Guid? Direction { get; set; }
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan? EstimatedArrivalTime { get; set; }
        public Guid RouteId { get; set; }
        public Guid BusId { get; set; }
        public int? MaxCapacity { get; set; }
        public string Notes { get; set; } = "";
        public bool? isRecurring { get; set; }
        public string RecurringDays { get; set; } = "";
        public Guid StudentID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string LevelName { get; set; } = "";
        public bool? isDaySchool { get; set; }

        public DateTime? TripDate { get; set; }

        public Guid? ParentID { get; set; }
        public List<string> AssignedDays { get; set; } = new();
        // 👇 NEW
        public List<Guid> EligibleStudentIds { get; set; } = new();
    }
}
