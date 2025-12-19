namespace IgnisEducationSuite.ServerServices.SmartTimeTableGenerator
{
    public class SubjectAdjacencyConstraints
    {
        public Guid SubjectId { get; set; }
        public List<Guid> CannotFollowSubjects { get; set; } = new();
    }
}
