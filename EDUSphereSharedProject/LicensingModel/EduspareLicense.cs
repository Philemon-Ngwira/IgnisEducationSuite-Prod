using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.LicensingModel
{
    public class EduspareLicense
    {
        public Guid ClientID { get; set; }
        public string LicenseStatus { get; set; } = "";
        public int NumberofUsers { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

    }
}
