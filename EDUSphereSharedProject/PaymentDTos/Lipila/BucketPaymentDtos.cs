using System;
using System.Collections.Generic;

namespace EDUSphereSharedProject.PaymentDTos.Lipila
{
    /// <summary>
    /// One fee bucket's combined outstanding total for a student, across every
    /// invoice that allocates money to it. This is the payable unit parents see -
    /// paying it settles all of that student's invoices in this bucket at once.
    /// </summary>
    public class StudentBucketSummaryDto
    {
        public Guid BucketId { get; set; }

        public string BucketName { get; set; } = default!;

        public decimal TotalOutstanding { get; set; }
    }

    /// <summary>
    /// The invoice-level breakdown behind a bucket's outstanding total, shown
    /// before a parent commits to paying it.
    /// </summary>
    public class BucketInvoiceOutstandingDto
    {
        public Guid InvoiceId { get; set; }

        public string? InvoiceType { get; set; }

        public DateTime TermStartDate { get; set; }

        public DateTime TermEndDate { get; set; }

        public decimal OutstandingForBucket { get; set; }
    }

    public class BucketOutstandingDto
    {
        public Guid BucketId { get; set; }

        public decimal TotalOutstanding { get; set; }

        public List<BucketInvoiceOutstandingDto> Invoices { get; set; } = new();
    }

    /// <summary>
    /// Request body to pay an entire bucket in one Lipila collection. The client
    /// names a student and a bucket, never individual invoices or a gateway
    /// account - the server resolves which invoices that covers and splits the
    /// collected amount across them once Lipila confirms.
    /// </summary>
    public class InitiateLipilaBucketMobileMoneyRequest
    {
        public Guid StudentFinanceId { get; set; }

        public Guid BucketId { get; set; }

        public decimal Amount { get; set; }

        public string PhoneNumber { get; set; } = default!;

        public string? Email { get; set; }
    }

    /// <summary>
    /// Request body to pay an entire bucket by card. Same bucket-resolution rules
    /// as <see cref="InitiateLipilaBucketMobileMoneyRequest"/> - the split across
    /// invoices is only decided and applied once Lipila confirms the card charge.
    /// </summary>
    public class InitiateLipilaBucketCardRequest
    {
        public Guid StudentFinanceId { get; set; }

        public Guid BucketId { get; set; }

        public decimal Amount { get; set; }

        public LipilaCardCustomerInfo Customer { get; set; } = default!;
    }
}
