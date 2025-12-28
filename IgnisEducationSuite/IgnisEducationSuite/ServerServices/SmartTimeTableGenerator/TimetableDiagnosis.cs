namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class TimetableDiagnosis
    {
        public Dictionary<Guid, int> MissingPeriods;
        public List<SlotState> FreeSlots;
        public Dictionary<Guid, List<string>> BlockingReasons;
    }
}
