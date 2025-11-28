using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class usp_GetPharmacyLicenseStatusResult
    {
        public Guid LicenseId { get; set; }
        public Guid ClientId { get; set; }
        public string LicenseKey { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int IsValid { get; set; }
        public int? DaysUntilExpiry { get; set; }
    }
}
