using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public class StudentGradedExamsTestsAndQuizzes
    {
        public string TeacherFirstName { get; set; } = string.Empty;
        public string TeacherLastName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TotalMarks { get; set; }
        public Guid StudentExamID { get; set; }
        public Guid? StudentID { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Grade { get; set; }
        public Guid? ExamID { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
