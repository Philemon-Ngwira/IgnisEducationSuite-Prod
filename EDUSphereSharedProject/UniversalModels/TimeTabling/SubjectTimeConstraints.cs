namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class SubjectTimeConstraints
    {
        public Guid SubjectId { get; set; }

        public bool MustBeEarlyMorning { get; set; }
        public bool MustBeMorning { get; set; }
        public bool MustBeAfternoon { get; set; }

        // NEW — evaluated against slot.StartTime
        public TimeSpan? MorningEnd { get; set; }       // e.g. 10:30
        public TimeSpan? EarlyMorningEnd { get; set; }  // e.g. 09:50
    }


}
