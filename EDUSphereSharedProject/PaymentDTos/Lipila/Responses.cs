using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.PaymentDTos.Lipila
{
    public class Responses
    {
    }
    public class LipilaCollectionResponse
    {
        public string? ReferenceId { get; set; }

        public string? Currency { get; set; }

        public decimal Amount { get; set; }

        public string? AccountNumber { get; set; }

        public string? Status { get; set; }

        public string? PaymentType { get; set; }

        public string? Type { get; set; }

        public string? IpAddress { get; set; }

        public string? Identifier { get; set; }

        public string? ExternalId { get; set; }

        public string? Message { get; set; }

        public string? ReferenceData { get; set; }

        public string? Narration { get; set; }

        public string? CardRedirectionUrl { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
    }
    public class LipilaWalletBalanceResponse
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public LipilaWalletBalanceData? Data { get; set; }
    }
    public class LipilaWalletBalanceData
    {
        public decimal Balance { get; set; }
    }
}
