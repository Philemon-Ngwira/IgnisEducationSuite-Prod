namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class PlacementTask
    {
        public Guid SubjectId { get; }
        public bool IsDouble { get; }
        public bool IsRequired { get; }

        public PlacementTask(Guid subjectId, bool isDouble, bool isRequired)
        {
            SubjectId = subjectId;
            IsDouble = isDouble;
            IsRequired = isRequired;
        }
    }

}
