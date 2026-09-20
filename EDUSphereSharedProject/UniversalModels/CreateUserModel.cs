using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class CreateUserModel
    {
        public string UserName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        [NotMapped]
        public string UserID { get; set; } = string.Empty;
        [NotMapped]
        public Guid SchoolID { get; set; } = new Guid();
        [NotMapped]
        public string FirstName { get; set; } = string.Empty;
        [NotMapped]
        public string LastName { get; set; } = string.Empty;
        [NotMapped]
        public byte[] ProfilePic { get; set; } = new byte[0];
        [NotMapped]
        public string Gender { get; set; }  = string.Empty;
        [NotMapped]
        public DateTime DateEngaged { get; set; }
        [NotMapped]
        public string Address { get; set; } = string.Empty;
        [NotMapped]
        public int GradeLevel { get; set; } = 0;
        [NotMapped]
        public string GradeSection { get;set; } =  string.Empty;

    }
}
