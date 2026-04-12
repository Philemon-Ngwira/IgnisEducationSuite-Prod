using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class StudentWithClassesDTO
    {
        public Guid StudentID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public int AcademicLevel { get; set; }
        public string LevelName { get; set; } = "";
        public string StudentNumber { get; set; } = "";
        public string Gender { get; set; } = "";
        public string GradeSection { get; set; } = "";
        public Guid? AcademicLevelID { get; set; }
        public Guid? LevelSectionID { get; set; }

        public List<StudentClassDTO> Classes { get; set; } = new();

        public bool isInteractable { get; set; } = true;

        public class StudentClassDTO
        {
            public Guid ClassID { get; set; }
            public string ClassName { get; set; } = "";
            public Guid? TeacherID { get; set; }
        }

    }
}
