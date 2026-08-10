using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.PaymentDTos.Lipila
{
    public class LipilaPaymentInitiationResult
    {
        public Guid TransactionId { get; set; }

        public string ReferenceId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "ZMW";

        public string PaymentType { get; set; } = string.Empty;

        public string? CardRedirectionUrl { get; set; }
    }
}
