using EDUSphereSharedProject.PaymentDTos.Lipila;

namespace IgnisEducationSuite.ServerServices.PaymentsServices
{
    public interface IPaymentService
    {
        /// <summary>
        /// Resolves the Lipila wallet for the invoice's fee bucket(s) and initiates
        /// a mobile money collection. Does not mark anything as paid - the webhook
        /// (or a status check) determines the final outcome.
        /// </summary>
        Task<LipilaPaymentInitiationResult> InitiateLipilaMobileMoneyAsync(
            Guid invoiceId,
            decimal amount,
            string phoneNumber,
            string? email = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the Lipila wallet for the invoice's fee bucket(s) and initiates
        /// a card collection. The caller must redirect the customer to the returned
        /// CardRedirectionUrl to complete payment.
        /// </summary>
        Task<LipilaPaymentInitiationResult> InitiateLipilaCardAsync(
            Guid invoiceId,
            decimal amount,
            LipilaCardCustomerInfo customer,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Actively polls Lipila for the current status of a previously initiated
        /// collection and reconciles it through the same idempotent processing
        /// path as the webhook. Useful for manual reconciliation or when a
        /// webhook delivery may have been missed.
        /// </summary>
        Task<LipilaCollectionResponse> CheckLipilaPaymentStatusAsync(
            string referenceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a Lipila collection callback (webhook or manual status check).
        /// Idempotent: repeated callbacks for the same referenceId are safe and will
        /// not create duplicate Payments/PaymentAllocations or double-apply amounts.
        /// </summary>
        Task ProcessLipilaCallbackAsync(
            LipilaCollectionResponse callback,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves the Lipila wallet balance for a school's fee bucket.
        /// </summary>
        Task<LipilaWalletBalanceResponse> GetWalletBalanceAsync(
            Guid bucketId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// True if this invoice can be paid through Lipila right now. Lets the
        /// frontend decide gateway-vs-manual UI without attempting a real collection.
        /// </summary>
        Task<bool> IsInvoiceGatewayEligibleAsync(
            Guid invoiceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Every fee bucket this student currently owes money in, with the combined
        /// outstanding total across all of their invoices in each bucket. This is
        /// the payable unit the parent-facing UI lists - one bucket, one wallet,
        /// however many invoices happen to feed into it.
        /// </summary>
        Task<List<StudentBucketSummaryDto>> GetOutstandingBucketsAsync(
            Guid studentFinanceId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The invoice-level breakdown behind one student's outstanding total in
        /// one bucket, for display before paying.
        /// </summary>
        Task<BucketOutstandingDto> GetBucketOutstandingAsync(
            Guid studentFinanceId,
            Guid bucketId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// True if this bucket has an active Lipila wallet with a credential
        /// configured right now.
        /// </summary>
        Task<bool> IsBucketGatewayEligibleAsync(
            Guid bucketId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Initiates one Lipila mobile money collection covering (up to) a
        /// student's entire outstanding balance in one bucket, regardless of how
        /// many separate invoices that spans. The split across invoices is decided
        /// now and only applied - as individual Payment rows, so the existing
        /// per-invoice DB triggers still fire correctly - once Lipila confirms.
        /// </summary>
        Task<LipilaPaymentInitiationResult> InitiateLipilaBucketMobileMoneyAsync(
            Guid studentFinanceId,
            Guid bucketId,
            decimal amount,
            string phoneNumber,
            string? email = null,
            CancellationToken cancellationToken = default);
    }
}
