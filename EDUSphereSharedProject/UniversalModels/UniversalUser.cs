using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class UniversalUser
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int GradeLevel { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string Country {  get; set; }
        public string City { get; set; }
        public string UserID { get; set; }
        public string Role { get; set; }
        public string ContactNo { get; set; }
        public string GradeSection { get; set; }

        public string EmployeeID { get; set; }
        public DateTime? DateEngaged { get; set; }
        public byte[] profilePic { get; set; }
    }
}
