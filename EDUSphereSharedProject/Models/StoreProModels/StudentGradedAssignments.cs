using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public class StudentGradedAssignments
    {
        public Guid StudentAssignmentID { get; set; }
        public Guid? StudentID { get; set; }
        public Guid? AssignmentID { get; set; }
        public DateTime? SubmissionDate { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Grade { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? QuestionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? AssignmentTotalMarks { get; set; }
        public string TeacherFirstName { get; set; } = string.Empty;
        public string TeacherLastName { get; set; } = string.Empty;
    }
}
