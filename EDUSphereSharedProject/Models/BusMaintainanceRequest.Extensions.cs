using System.ComponentModel.DataAnnotations.Schema;

namespace EDUSphereSharedProject.Models
{
    public partial class BusMaintenanceRequest
    {
        [NotMapped]
        public string RegistrationNumber { get; set; } = "";
        [NotMapped]
        public string ReportingStaffFirstName { get; set; } = "";
        [NotMapped]
        public string ReportingStaffLastName { get; set; } = "";
        [NotMapped]
        public string EmployeeID { get; set; } = "";
    }
}
