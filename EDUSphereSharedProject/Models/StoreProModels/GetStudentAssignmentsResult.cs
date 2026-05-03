using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public partial class GetStudentAssignmentsResult
    {
        public Guid AssignmentID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public Guid? ClassID { get; set; }
        public Guid? TeacherID { get; set; }
        public int? TotalMarks { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public Guid StudentID { get; set; }
        public int Overdue { get; set; }
    }
}
