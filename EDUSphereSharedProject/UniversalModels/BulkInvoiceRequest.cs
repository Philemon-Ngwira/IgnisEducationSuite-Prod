using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.UniversalModels
{
    public class BulkInvoiceRequest
    {
        public DateTime TermStartDate { get; set; }
        public DateTime TermEndDate { get; set; }
        public DateTime DueDate { get; set; }

        public Guid InvoiceTypeId { get; set; }

        public Guid FeeStructureId { get; set; }
        public string InvoiceType { get; set; } = "";
        public decimal Amount {get; set;  }
        public Guid SchoolID { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string SchoolName { get; set; }
    }
}
