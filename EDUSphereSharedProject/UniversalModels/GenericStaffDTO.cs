using EDUSphereSharedProject.Models;

namespace EDUSphereSharedProject.UniversalModels
{
    public enum StaffType
    {
        None,
        Clinic,
        Bus,
        General
    }

    public class GenericStaffDTO
    {
        public ClinicStaff ClinicStaff { get; set; } = new();
        public BusStaff BusStaff { get; set; } = new();
        public Staff Staff { get; set; } = new();
        public StaffType ActiveType { get; set; } = StaffType.None;
    }
}
