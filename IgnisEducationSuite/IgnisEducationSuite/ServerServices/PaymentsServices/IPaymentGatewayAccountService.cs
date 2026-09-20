using EDUSphereSharedProject.PaymentDTos.Lipila;

namespace IgnisEducationSuite.ServerServices.PaymentsServices;

/// <summary>
/// Admin-facing configuration surface for Lipila wallets: one PaymentGatewayAccount
/// per FeeBucket per environment (Sandbox/Production), plus its encrypted API key.
/// Distinct from IPaymentService, which drives customer-facing collections and never
/// touches credentials directly.
/// </summary>
public interface IPaymentGatewayAccountService
{
    Task<List<FeeBucketGatewayDto>> GetForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayAccountDto> CreateAsync(
        CreatePaymentGatewayAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayAccountDto> UpdateAsync(
        Guid id,
        UpdatePaymentGatewayAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypts and stores a new credential row for the account. Rotation, not
    /// replacement - the previous row is left in place for audit history, and
    /// LipilaService always resolves the most recently created one.
    /// </summary>
    Task SetCredentialAsync(
        Guid id,
        string apiKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls Lipila's wallet balance endpoint with the account's current
    /// credential so an admin can verify a key was entered correctly.
    /// </summary>
    Task<LipilaWalletBalanceResponse> TestConnectionAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
