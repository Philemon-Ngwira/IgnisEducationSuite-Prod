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
    }
}
