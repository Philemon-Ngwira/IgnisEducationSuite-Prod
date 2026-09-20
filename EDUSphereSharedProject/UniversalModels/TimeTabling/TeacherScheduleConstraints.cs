using EDUSphereSharedProject.Models;

namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class TeacherScheduleConstraints
    {
        public Guid TeacherId { get; set; }
        public List<TimeSlot> UnavailableSlots { get; set; } = new();
        public int MaxDailyPeriods { get; set; } = 5;
    }
}
