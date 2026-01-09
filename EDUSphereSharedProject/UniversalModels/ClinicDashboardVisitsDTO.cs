using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class ClinicDashboardVisitsDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public byte[] ProfilePic { get; set; }
        public DateTime VisitDate { get; set; }
        public string Symptoms { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
    }
}
