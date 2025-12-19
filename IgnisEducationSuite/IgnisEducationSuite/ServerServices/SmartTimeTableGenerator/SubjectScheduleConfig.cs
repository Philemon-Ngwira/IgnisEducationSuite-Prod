namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class SubjectScheduleConfig
    {
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int WeeklyPeriods { get; set; }       // total number of periods in a week
        public int DoublePeriods { get; set; }
    }
}
