namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator.Helpers
{
    public class TimetableReport
    {
        public List<string> InvariantViolations { get; } = new();
        public Dictionary<string, int> Metrics { get; } = new();

        public bool IsValid => InvariantViolations.Count == 0;
    }
}
