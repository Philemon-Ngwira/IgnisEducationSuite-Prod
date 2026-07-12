using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.FinanceModels.DummyData
{
    public class PaymentBucketVM
    {
        public Guid BucketId { get; set; }

        public string Name { get; set; } = "";

        public decimal OutstandingAmount { get; set; }

        public int InvoiceCount { get; set; }

        public string Status { get; set; } = "Payment Required";

        public  bool IsSubmitted { get; set; }

        public List<InvoiceLineVM> Invoices { get; set; } = new();
    }
    public record InvoiceLineVM(string Name, decimal Amount);
}
