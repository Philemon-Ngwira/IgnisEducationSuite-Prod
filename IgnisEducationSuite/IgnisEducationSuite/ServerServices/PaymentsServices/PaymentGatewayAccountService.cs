using EduSphereDomain.FinanceData;
using EDUSphereSharedProject.FinanceModels;
using EDUSphereSharedProject.PaymentDTos.Lipila;
using IgnisEducationSuite.ServerServices.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IgnisEducationSuite.ServerServices.PaymentsServices;

public sealed class PaymentGatewayAccountService : IPaymentGatewayAccountService
{
    private const string Provider = "Lipila";

    private readonly PhoenixEdusphereFinanceContext _context;
    private readonly ILipilaService _lipilaService;
    private readonly IEncryptionService _encryptionService;
    private readonly EncryptionConfiguration _encryptionOptions;

    public PaymentGatewayAccountService(
        PhoenixEdusphereFinanceContext context,
        ILipilaService lipilaService,
        IEncryptionService encryptionService,
        IOptions<EncryptionConfiguration> encryptionOptions)
    {
        _context = context;
        _lipilaService = lipilaService;
        _encryptionService = encryptionService;
        _encryptionOptions = encryptionOptions.Value;
    }

    public async Task<List<FeeBucketGatewayDto>> GetForSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var buckets = await _context.FeeBuckets
            .AsNoTracking()
            .Where(b => b.SchoolId == schoolId)
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var bucketIds = buckets.Select(b => b.Id).ToList();

        var accounts = await _context.PaymentGatewayAccounts
            .AsNoTracking()
            .Where(a => bucketIds.Contains(a.BucketId) && a.Provider == Provider)
            .Include(a => a.PaymentGatewayCredentials)
            .ToListAsync(cancellationToken);

        return buckets
            .Select(b => new FeeBucketGatewayDto
            {
                BucketId = b.Id,
                BucketName = b.Name,
                Accounts = accounts
                    .Where(a => a.BucketId == b.Id)
                    .OrderBy(a => a.Environment)
                    .Select(ToDto)
                    .ToList()
            })
            .ToList();
    }

    public async Task<PaymentGatewayAccountDto> CreateAsync(
        CreatePaymentGatewayAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.WalletId))
        {
            throw new ArgumentException("Wallet ID is required.", nameof(request));
        }

        var environment = NormalizeEnvironment(request.Environment);

        var bucket = await _context.FeeBuckets
            .AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == request.BucketId && b.SchoolId == request.SchoolId,
                cancellationToken);

        if (bucket is null)
        {
            throw new KeyNotFoundException(
                $"Fee bucket '{request.BucketId}' was not found for school '{request.SchoolId}'.");
        }

        var duplicate = await _context.PaymentGatewayAccounts
            .AnyAsync(
                a =>
                    a.BucketId == request.BucketId &&
                    a.Provider == Provider &&
                    a.Environment == environment,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException(
                $"A Lipila {environment} wallet is already configured for bucket '{bucket.Name}'. " +
                "Edit the existing one instead.");
        }

        var now = DateTime.UtcNow;

        var account = new PaymentGatewayAccount
        {
            Id = Guid.NewGuid(),
            SchoolId = request.SchoolId,
            BucketId = request.BucketId,
            Provider = Provider,
            WalletId = request.WalletId.Trim(),
            Environment = environment,
            IsActive = true,
            CreatedAt = now
        };

        await _context.PaymentGatewayAccounts.AddAsync(account, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(account);
    }

    public async Task<PaymentGatewayAccountDto> UpdateAsync(
        Guid id,
        UpdatePaymentGatewayAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.WalletId))
        {
            throw new ArgumentException("Wallet ID is required.", nameof(request));
        }

        var account = await _context.PaymentGatewayAccounts
            .Include(a => a.PaymentGatewayCredentials)
            .FirstOrDefaultAsync(a => a.Id == id && a.Provider == Provider, cancellationToken);

        if (account is null)
        {
            throw new KeyNotFoundException($"Payment gateway account '{id}' was not found.");
        }

        account.WalletId = request.WalletId.Trim();
        account.Environment = NormalizeEnvironment(request.Environment);
        account.IsActive = request.IsActive;
        account.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return ToDto(account);
    }

    public async Task SetCredentialAsync(
        Guid id,
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key is required.", nameof(apiKey));
        }

        var accountExists = await _context.PaymentGatewayAccounts
            .AnyAsync(a => a.Id == id && a.Provider == Provider, cancellationToken);

        if (!accountExists)
        {
            throw new KeyNotFoundException($"Payment gateway account '{id}' was not found.");
        }

        var credential = new PaymentGatewayCredential
        {
            Id = Guid.NewGuid(),
            PaymentGatewayAccountId = id,
            EncryptedSecret = _encryptionService.Encrypt(apiKey.Trim()),
            KeyVersion = _encryptionOptions.CurrentKeyVersion,
            CreatedAt = DateTime.UtcNow
        };

        await _context.PaymentGatewayCredentials.AddAsync(credential, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<LipilaWalletBalanceResponse> TestConnectionAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var accountExists = await _context.PaymentGatewayAccounts
            .AnyAsync(a => a.Id == id && a.Provider == Provider, cancellationToken);

        if (!accountExists)
        {
            throw new KeyNotFoundException($"Payment gateway account '{id}' was not found.");
        }

        return await _lipilaService.GetWalletBalanceAsync(id, cancellationToken);
    }

    private static PaymentGatewayAccountDto ToDto(PaymentGatewayAccount account)
    {
        var latestCredential = account.PaymentGatewayCredentials?
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();

        return new PaymentGatewayAccountDto
        {
            Id = account.Id,
            BucketId = account.BucketId,
            Provider = account.Provider,
            WalletId = account.WalletId,
            Environment = account.Environment,
            IsActive = account.IsActive ?? false,
            HasCredential = latestCredential is not null,
            CredentialSetAt = latestCredential?.CreatedAt,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }

    private static string NormalizeEnvironment(string? environment)
    {
        var trimmed = environment?.Trim();

        if (string.Equals(trimmed, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return "Production";
        }

        if (string.Equals(trimmed, "Sandbox", StringComparison.OrdinalIgnoreCase))
        {
            return "Sandbox";
        }

        throw new ArgumentException("Environment must be 'Sandbox' or 'Production'.", nameof(environment));
    }
}
