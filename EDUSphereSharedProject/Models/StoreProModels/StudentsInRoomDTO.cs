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
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? AcademicLevel { get; set; }
        public Guid? ParentID { get; set; }
        public Guid GenderId { get; set; }
        public string GenderDescription { get; set; }
        public string Address { get; set; }
        public string StudentNumber { get; set; }
        public byte[] ProfilePic { get; set; }
        public string UserID { get; set; }
        public DateTime? DateOnBoarded { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public Guid? SchoolID { get; set; }
        public string GradeSection { get; set; }
        public string LevelName { get; set; }
        public Guid AllocationId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string Status { get; set; }
    }
}
