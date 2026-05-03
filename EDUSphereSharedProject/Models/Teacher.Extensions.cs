using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.Models
{
    public partial class Teacher
    {
        [NotMapped]
        public string GeneratedUserName { get; set; } = "";

        [NotMapped]
        public string FullName { get { return $"{FirstName} {LastName}"; } }
    }
}
