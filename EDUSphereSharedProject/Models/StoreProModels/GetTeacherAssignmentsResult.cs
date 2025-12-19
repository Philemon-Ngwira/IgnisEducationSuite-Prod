using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public partial class GetTeacherAssignmentsResult
    {
        public Guid StudentAssignmentID { get; set; }
        public Guid? StudentID { get; set; }
        public Guid? AssignmentID { get; set; }
        public DateTime? SubmissionDate { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Grade { get; set; }
        public string Status { get; set; }
        public Guid? QuestionID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? TotalMarks { get; set; }
        public string ClassName { get; set; }
        public int? GradeLevel { get; set; }
        public int Overdue { get; set; }
        [NotMapped]
        public string LevelName { get; set; }
    }
}
