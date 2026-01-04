using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class Student
    {
        [NotMapped]
        public string Email { get; set; }
        [NotMapped]
        public string GeneratedUserName { get; set; }
    }
}
