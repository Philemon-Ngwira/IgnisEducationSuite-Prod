using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDUSphereSharedProject.PaymentDTos.Lipila
{
    public class LipilaModels
    {
    }
    public class LipilaMobileMoneyCollectionRequest
    {
        public string ReferenceId { get; set; } = default!;

        public decimal Amount { get; set; }

        public string Narration { get; set; } = default!;

        public string AccountNumber { get; set; } = default!;

        public string Currency { get; set; } = "ZMW";

        public string? Email { get; set; }

        public string? ReferenceData { get; set; }
    }
    public class LipilaCardCollectionRequest
    {
        public LipilaCustomerInfo CustomerInfo { get; set; } = default!;

        public LipilaCardCollectionDetails CollectionRequest { get; set; } = default!;
    }
    public class LipilaCustomerInfo
    {
        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string City { get; set; } = default!;

        public string Country { get; set; } = default!;

        public string Address { get; set; } = default!;

        public string Email { get; set; } = default!;

        public string Zip { get; set; } = default!;
    }
    public class LipilaCardCollectionDetails
    {
        public string ReferenceId { get; set; } = default!;

        public decimal Amount { get; set; }

        public string Narration { get; set; } = default!;

        public string AccountNumber { get; set; } = default!;

        public string Currency { get; set; } = "ZMW";

        public string BackUrl { get; set; } = default!;

        public string ReferenceData { get; set; } = default!;
    }

    /// <summary>
    /// App-facing customer info accepted by <c>IPaymentService.InitiateLipilaCardAsync</c>.
    /// Mapped internally to <see cref="LipilaCustomerInfo"/> for the outbound Lipila request.
    /// </summary>
    public class LipilaCardCustomerInfo
    {
        public string FirstName { get; set; } = default!;

        public string LastName { get; set; } = default!;

        public string PhoneNumber { get; set; } = default!;

        public string Email { get; set; } = default!;

        public string City { get; set; } = default!;

        public string Country { get; set; } = default!;

        public string Address { get; set; } = default!;

        public string Zip { get; set; } = default!;
    }
}
