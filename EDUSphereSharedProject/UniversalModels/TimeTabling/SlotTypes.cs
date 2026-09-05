namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    /// <summary>
    /// TimeSlot.SlotType is a free-text DB column (not a real enum/lookup table) — this is the
    /// single shared source of truth for its valid values, so every consumer (Time Slots CRUD,
    /// the generation engine's slot filtering, overrides, the timetable viewer) references the
    /// same literal strings instead of redefining them independently and drifting out of sync.
    /// </summary>
    public static class SlotTypes
    {
        public const string EarlyMorning = "EarlyMorning";
        public const string Morning = "Morning";
        public const string Afternoon = "Afternoon";
        public const string BreakTime = "BreakTime";
        public const string LunchTime = "LunchTime";

        public static readonly string[] All =
        {
            EarlyMorning, Morning, Afternoon, BreakTime, LunchTime,
        };

        /// <summary>Slot types the generation engine treats as schedulable teaching time.</summary>
        public static readonly string[] Teaching =
        {
            EarlyMorning, Morning, Afternoon,
        };
    }
}
