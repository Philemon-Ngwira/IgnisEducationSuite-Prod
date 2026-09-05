namespace EDUSphereSharedProject.UniversalModels.TimeTabling
{
    /// <summary>
    /// Persisted as SubjectScheduleConfig.TimePreference (tinyint). Replaces the old
    /// SubjectScheduleConfig.EarlyMorningOnly boolean, which could not express the
    /// morning-only / afternoon-only cases.
    /// </summary>
    public enum SubjectTimePreference : byte
    {
        Any = 0,
        EarlyMorningOnly = 1,
        MorningOnly = 2,
        AfternoonOnly = 3,
    }
}
