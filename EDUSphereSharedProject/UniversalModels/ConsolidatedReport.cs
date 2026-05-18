using System.ComponentModel.DataAnnotations;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ConsolidatedReport
    {
        public Guid StudentID { get; set; }
        [StringLength(101)]
        public string StudentName { get; set; } = "";
        [StringLength(20)]
        public string Term { get; set; } = "";
        public DateTime? IssuedDate { get; set; }
        [StringLength(50)]
        public string Grade { get; set; } = "";
        [StringLength(50)]
        public string GradeSection { get; set; } = "";
        public int? Points { get; set; } = 0;
        public int? PointsInBestSix { get; set; } = 0;
        public int? PositionInClass { get; set; } = 0;
        public bool IsGCE { get; set; } = false;
        [StringLength(3)]
        public string PointsDisplay { get; set; } = "";
        [StringLength(3)]
        public string PositionDisplay { get; set; } = "";
        [StringLength(4000)]
        public string SubjectList { get; set; } = "";
    }
}
