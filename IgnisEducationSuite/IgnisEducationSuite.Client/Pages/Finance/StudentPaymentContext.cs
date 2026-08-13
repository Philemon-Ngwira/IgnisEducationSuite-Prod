namespace IgnisEducationSuite.Client.Pages.Finance;

/// <summary>
/// One student a logged-in user (themselves, or a parent's child) can pay fees
/// for. StudentFinanceId is the key the bucket-payment endpoints key off.
/// </summary>
public sealed class StudentPaymentContext
{
    public Guid StudentFinanceId { get; set; }

    public string StudentName { get; set; } = string.Empty;
}
