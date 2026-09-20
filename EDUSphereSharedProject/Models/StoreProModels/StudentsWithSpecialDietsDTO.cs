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
        public string DietType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public byte[] ProfilePic { get; set; } = Array.Empty<byte>();
        public string StudentNumber { get; set; } = string.Empty;
        public string AcademicLevel { get; set; } = string.Empty;
    }
}
