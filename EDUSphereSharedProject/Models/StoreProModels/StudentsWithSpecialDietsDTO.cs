using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public class StudentsWithSpecialDietsDTO
    {
        public Guid DietId { get; set; }
        public Guid StudentId { get; set; }
        public string DietType { get; set; }
        public string Description { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Gender { get; set; }
        public byte[] ProfilePic { get; set; }
        public string StudentNumber { get; set; }
        public string AcademicLevel { get; set; }
    }
}
