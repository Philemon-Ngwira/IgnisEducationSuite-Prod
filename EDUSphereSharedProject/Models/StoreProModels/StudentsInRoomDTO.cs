using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models.StoreProModels
{
    public class StudentsInRoomDTO
    {
        public Guid StudentID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? AcademicLevel { get; set; }
        public Guid? ParentID { get; set; }
        public Guid GenderId { get; set; }
        public string GenderDescription { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string StudentNumber { get; set; } = string.Empty;
        public byte[] ProfilePic { get; set; } = Array.Empty<byte>();
        public string UserID { get; set; } = string.Empty;
        public DateTime? DateOnBoarded { get; set; }
        public string Country { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public Guid? SchoolID { get; set; }
        public string GradeSection { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public Guid AllocationId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
