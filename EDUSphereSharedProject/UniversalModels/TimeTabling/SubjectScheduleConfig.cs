namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    public class SubjectScheduleConfig
    {
        public Guid SubjectId { get; set; }
        public string SubjectName { get; set; } = "";
        public int WeeklyPeriods { get; set; }
        public int RequiredDoubles { get; set; } = 0; // user-specified
        public bool EarlyMorningOnly { get; set; } = false;

        public Guid TeacherId { get; set; }

        public bool IsCoreSubject { get; set; }
        public bool IsActivity { get; set; } // <-- IMPORTANT


        public TimeSpan EarlyMorningEnd { get;set; } // Used instead of hard code
        public TimeSpan MorningEnd { get;set; } // Used instead of hard code
        public TimeSpan AfternoonStart { get;set; } // Used instead of hard code
        public TimeSpan AfternoonEnd { get;set; } // Used instead of hard code

    }
}
