namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class SubjectScheduleConfig
    {
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public Guid TeacherId { get; set; }      // NEW: Assign teacher
        public int WeeklyPeriods { get; set; }       // total number of periods in a week
        public int DoublePeriods { get; set; }
    }
}
