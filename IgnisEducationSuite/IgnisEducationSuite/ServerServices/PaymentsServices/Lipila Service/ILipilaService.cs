using EDUSphereSharedProject.PaymentDTos.Lipila;

public interface ILipilaService
{
    Task<LipilaCollectionResponse> CreateMobileMoneyCollectionAsync(
        Guid paymentGatewayAccountId,
        LipilaMobileMoneyCollectionRequest request,
        CancellationToken cancellationToken = default);

    Task<LipilaCollectionResponse> CreateCardCollectionAsync(
        Guid paymentGatewayAccountId,
        LipilaCardCollectionRequest request,
        CancellationToken cancellationToken = default);

    Task<LipilaCollectionResponse> CheckCollectionStatusAsync(
        Guid paymentGatewayAccountId,
        string referenceId,
        CancellationToken cancellationToken = default);

    Task<LipilaWalletBalanceResponse> GetWalletBalanceAsync(
        Guid paymentGatewayAccountId,
        CancellationToken cancellationToken = default);
}