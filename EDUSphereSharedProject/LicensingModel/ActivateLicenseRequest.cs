using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.LicensingModel
{
    public class ActivateLicenseRequest
    {
        public string ClientId { get; set; } // The unique identifier of the client
        public string PlanType { get; set; } // e.g., Monthly, Quarterly, Biannual, Annual
        public int UserLimit { get; set; }   // Maximum number of users for this license
        public string ClientName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Application { get; set; }
        public DateTime StartDate { get; set; } // License start date
        public DateTime EndDate { get; set; }   // License end date
    }
}
