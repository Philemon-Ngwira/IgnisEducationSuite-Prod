using System;

namespace EDUSphereSharedProject.PaymentDTos.Lipila
{
    /// <summary>
    /// Request body accepted by the "initiate mobile money payment" API endpoint.
    /// The client only ever names an invoice and an amount - it never selects a
    /// gateway account or wallet, that routing is resolved server-side.
    /// </summary>
    public class InitiateLipilaMobileMoneyRequest
    {
        public Guid InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public string PhoneNumber { get; set; } = default!;

        public string? Email { get; set; }
    }

    /// <summary>
    /// Request body accepted by the "initiate card payment" API endpoint.
    /// </summary>
    public class InitiateLipilaCardPaymentRequest
    {
        public Guid InvoiceId { get; set; }

        public decimal Amount { get; set; }

        public LipilaCardCustomerInfo Customer { get; set; } = default!;
    }
}
