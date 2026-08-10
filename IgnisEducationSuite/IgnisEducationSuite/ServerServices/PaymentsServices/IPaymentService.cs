namespace IgnisEducationSuite.ServerServices.PaymentsServices
{
    public interface IPaymentService
    {
        Task<LipilaPaymentInitiationResult> InitiateLipilaMobileMoneyAsync(
            Guid invoiceId,
            decimal amount,
            string phoneNumber,
            string? email = null,
            CancellationToken cancellationToken = default);

        Task<LipilaPaymentInitiationResult> InitiateLipilaCardAsync(
            Guid invoiceId,
            decimal amount,
            LipilaCardCustomerInfo customer,
            CancellationToken cancellationToken = default);
    }
}
