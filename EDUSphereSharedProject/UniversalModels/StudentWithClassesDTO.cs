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
        public List<string> ClassNames { get; set; } = new();

        public bool isInteractable = true;
    }
}
