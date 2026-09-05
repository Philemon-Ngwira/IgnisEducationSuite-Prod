namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    /// <summary>Candidate moves the optimizer can try. Always goes through TimetableState so the
    /// cached weekly/daily counts stay in sync with the actual slot contents.</summary>
    public static class TimetableMoves
    {
        public static void Swap(TimetableState state, GenerationSlot a, GenerationSlot b) => state.SwapSlots(a, b);
    }
}
