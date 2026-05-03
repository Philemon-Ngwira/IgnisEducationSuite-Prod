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
        public string Email { get; set; } = string.Empty;
        [NotMapped]
        public string GeneratedUserName { get; set; } = string.Empty;

        [NotMapped]
        public List<ClinicVisit> clinicVisits { get; set; } = new List<ClinicVisit>();
        [NotMapped]
        public bool isCurrentlyInHospital { get; set; }
    }
}
