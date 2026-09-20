using System.Data;
using System.Text.Json;
using EduSphereDomain.FinanceData;
using EDUSphereSharedProject.FinanceModels;
using EDUSphereSharedProject.PaymentDTos.Lipila;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IgnisEducationSuite.ServerServices.PaymentsServices;

public sealed class PaymentService : IPaymentService
{
    private const string Provider = "Lipila";
    private const string DefaultCurrency = "ZMW";

    private readonly PhoenixEdusphereFinanceContext _context;
    private readonly ILipilaService _lipilaService;
    private readonly LipilaConfiguration _lipilaOptions;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        PhoenixEdusphereFinanceContext context,
        ILipilaService lipilaService,
        IOptions<LipilaConfiguration> lipilaOptions,
        ILogger<PaymentService> logger)
    {
        _context = context;
        _lipilaService = lipilaService;
        _lipilaOptions = lipilaOptions.Value;
        _logger = logger;
    }

    public async Task<LipilaPaymentInitiationResult> InitiateLipilaMobileMoneyAsync(
        Guid invoiceId,
        decimal amount,
        string phoneNumber,
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizeZambianPhoneNumber(phoneNumber);

        var (invoice, gatewayAccount) =
            await ResolveInvoiceAndGatewayAccountAsync(invoiceId, amount, cancellationToken);

        var internalReference = GenerateInternalReference();
        var narration = $"Payment for invoice {invoice.InvoiceNumber ?? invoice.Id.ToString()}";
        var now = DateTime.UtcNow;

        // No Payment row yet - it's only created once Lipila confirms success
        // (see ProcessLipilaCallbackAsync), so the DB's payment triggers never
        // touch the invoice for a collection that hasn't actually settled.
        var gatewayTransaction = new PaymentGatewayTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = null,
            InvoiceId = invoice.Id,
            PaymentGatewayAccountId = gatewayAccount.Id,
            InternalReference = internalReference,
            Provider = Provider,
            TransactionType = "Collection",
            PaymentType = "MobileMoney",
            Status = "Pending",
            Amount = amount,
            Currency = DefaultCurrency,
            AccountNumber = normalizedPhone,
            ReferenceData = invoice.InvoiceNumber,
            Narration = narration,
            CreatedAt = now
        };

        await _context.PaymentGatewayTransactions.AddAsync(gatewayTransaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var lipilaRequest = new LipilaMobileMoneyCollectionRequest
        {
            ReferenceId = internalReference,
            Amount = amount,
            Narration = narration,
            AccountNumber = normalizedPhone,
            Currency = DefaultCurrency,
            Email = email,
            ReferenceData = invoice.InvoiceNumber
        };

        try
        {
            var response = await _lipilaService.CreateMobileMoneyCollectionAsync(
                gatewayAccount.Id,
                lipilaRequest,
                cancellationToken);

            return await ApplyProviderResponseAsync(gatewayTransaction, response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initiate Lipila mobile money collection for reference {InternalReference}.",
                internalReference);

            await MarkTransactionFailedAsync(
                gatewayTransaction,
                "Failed to initiate collection with Lipila.",
                cancellationToken);

            throw;
        }
    }

    public async Task<LipilaPaymentInitiationResult> InitiateLipilaCardAsync(
        Guid invoiceId,
        decimal amount,
        LipilaCardCustomerInfo customer,
        CancellationToken cancellationToken = default)
    {
        if (customer is null)
        {
            throw new ArgumentNullException(nameof(customer));
        }

        var (invoice, gatewayAccount) =
            await ResolveInvoiceAndGatewayAccountAsync(invoiceId, amount, cancellationToken);

        var internalReference = GenerateInternalReference();
        var narration = $"Payment for invoice {invoice.InvoiceNumber ?? invoice.Id.ToString()}";
        var now = DateTime.UtcNow;

        // No Payment row yet - it's only created once Lipila confirms success
        // (see ProcessLipilaCallbackAsync), so the DB's payment triggers never
        // touch the invoice for a collection that hasn't actually settled.
        var gatewayTransaction = new PaymentGatewayTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = null,
            InvoiceId = invoice.Id,
            PaymentGatewayAccountId = gatewayAccount.Id,
            InternalReference = internalReference,
            Provider = Provider,
            TransactionType = "Collection",
            PaymentType = "Card",
            Status = "Pending",
            Amount = amount,
            Currency = DefaultCurrency,
            AccountNumber = customer.PhoneNumber,
            ReferenceData = invoice.InvoiceNumber,
            Narration = narration,
            CreatedAt = now
        };

        await _context.PaymentGatewayTransactions.AddAsync(gatewayTransaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        var lipilaRequest = new LipilaCardCollectionRequest
        {
            CustomerInfo = new LipilaCustomerInfo
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                PhoneNumber = customer.PhoneNumber,
                City = customer.City,
                Country = customer.Country,
                Address = customer.Address,
                Email = customer.Email,
                Zip = customer.Zip
            },
            CollectionRequest = new LipilaCardCollectionDetails
            {
                ReferenceId = internalReference,
                Amount = amount,
                Narration = narration,
                AccountNumber = customer.PhoneNumber,
                Currency = DefaultCurrency,
                BackUrl = _lipilaOptions.CardReturnUrl,
                ReferenceData = invoice.InvoiceNumber
            }
        };

        try
        {
            var response = await _lipilaService.CreateCardCollectionAsync(
                gatewayAccount.Id,
                lipilaRequest,
                cancellationToken);

            return await ApplyProviderResponseAsync(gatewayTransaction, response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initiate Lipila card collection for reference {InternalReference}.",
                internalReference);

            await MarkTransactionFailedAsync(
                gatewayTransaction,
                "Failed to initiate collection with Lipila.",
                cancellationToken);

            throw;
        }
    }

    public async Task<List<StudentBucketSummaryDto>> GetOutstandingBucketsAsync(
        Guid studentFinanceId,
        CancellationToken cancellationToken = default)
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceBucketAllocations)
                .ThenInclude(a => a.Bucket)
            .Where(i => i.StudentFinanceId == studentFinanceId)
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
        {
            return new List<StudentBucketSummaryDto>();
        }

        var paidByInvoiceBucket = await LoadPaidByInvoiceBucketAsync(
            invoices.Select(i => i.Id).ToList(),
            cancellationToken);

        var totals = new Dictionary<Guid, (string Name, decimal Outstanding)>();

        foreach (var invoice in invoices)
        {
            foreach (var allocation in invoice.InvoiceBucketAllocations)
            {
                var paid = paidByInvoiceBucket.TryGetValue((invoice.Id, allocation.BucketId), out var p) ? p : 0m;
                var outstanding = allocation.Amount - paid;

                if (outstanding <= 0)
                {
                    continue;
                }

                var bucketName = allocation.Bucket?.Name ?? "Fees";

                totals[allocation.BucketId] = totals.TryGetValue(allocation.BucketId, out var existing)
                    ? (existing.Name, existing.Outstanding + outstanding)
                    : (bucketName, outstanding);
            }
        }

        return totals
            .Select(kvp => new StudentBucketSummaryDto
            {
                BucketId = kvp.Key,
                BucketName = kvp.Value.Name,
                TotalOutstanding = kvp.Value.Outstanding
            })
            .OrderBy(x => x.BucketName)
            .ToList();
    }

    public async Task<BucketOutstandingDto> GetBucketOutstandingAsync(
        Guid studentFinanceId,
        Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        var invoices = await LoadBucketInvoicesAsync(studentFinanceId, bucketId, cancellationToken);

        return new BucketOutstandingDto
        {
            BucketId = bucketId,
            TotalOutstanding = invoices.Sum(i => i.Outstanding),
            Invoices = invoices
                .Select(i => new BucketInvoiceOutstandingDto
                {
                    InvoiceId = i.Invoice.Id,
                    InvoiceType = i.Invoice.InvoiceType,
                    TermStartDate = i.Invoice.TermStartDate,
                    TermEndDate = i.Invoice.TermEndDate,
                    OutstandingForBucket = i.Outstanding
                })
                .OrderBy(i => i.TermStartDate)
                .ToList()
        };
    }

    public async Task<bool> IsBucketGatewayEligibleAsync(
        Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ResolveBucketGatewayAccountAsync(bucketId, cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<LipilaPaymentInitiationResult> InitiateLipilaBucketMobileMoneyAsync(
        Guid studentFinanceId,
        Guid bucketId,
        decimal amount,
        string phoneNumber,
        string? email = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = NormalizeZambianPhoneNumber(phoneNumber);

        var ctx = await PrepareBucketCollectionAsync(
            studentFinanceId,
            bucketId,
            amount,
            "MobileMoney",
            normalizedPhone,
            cancellationToken);

        var lipilaRequest = new LipilaMobileMoneyCollectionRequest
        {
            ReferenceId = ctx.InternalReference,
            Amount = amount,
            Narration = ctx.Narration,
            AccountNumber = normalizedPhone,
            Currency = DefaultCurrency,
            Email = email,
            ReferenceData = bucketId.ToString()
        };

        try
        {
            var response = await _lipilaService.CreateMobileMoneyCollectionAsync(
                ctx.GatewayAccount.Id,
                lipilaRequest,
                cancellationToken);

            return await ApplyProviderResponseAsync(ctx.Transaction, response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initiate Lipila bucket collection for reference {InternalReference}.",
                ctx.InternalReference);

            await MarkTransactionFailedAsync(
                ctx.Transaction,
                "Failed to initiate collection with Lipila.",
                cancellationToken);

            throw;
        }
    }

    public async Task<LipilaPaymentInitiationResult> InitiateLipilaBucketCardAsync(
        Guid studentFinanceId,
        Guid bucketId,
        decimal amount,
        LipilaCardCustomerInfo customer,
        CancellationToken cancellationToken = default)
    {
        if (customer is null)
        {
            throw new ArgumentNullException(nameof(customer));
        }

        var ctx = await PrepareBucketCollectionAsync(
            studentFinanceId,
            bucketId,
            amount,
            "Card",
            customer.PhoneNumber,
            cancellationToken);

        var lipilaRequest = new LipilaCardCollectionRequest
        {
            CustomerInfo = new LipilaCustomerInfo
            {
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                PhoneNumber = customer.PhoneNumber,
                City = customer.City,
                Country = customer.Country,
                Address = customer.Address,
                Email = customer.Email,
                Zip = customer.Zip
            },
            CollectionRequest = new LipilaCardCollectionDetails
            {
                ReferenceId = ctx.InternalReference,
                Amount = amount,
                Narration = ctx.Narration,
                AccountNumber = customer.PhoneNumber,
                Currency = DefaultCurrency,
                BackUrl = BuildCardReturnUrl(ctx.InternalReference),
                ReferenceData = bucketId.ToString()
            }
        };

        try
        {
            var response = await _lipilaService.CreateCardCollectionAsync(
                ctx.GatewayAccount.Id,
                lipilaRequest,
                cancellationToken);

            return await ApplyProviderResponseAsync(ctx.Transaction, response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to initiate Lipila bucket card collection for reference {InternalReference}.",
                ctx.InternalReference);

            await MarkTransactionFailedAsync(
                ctx.Transaction,
                "Failed to initiate collection with Lipila.",
                cancellationToken);

            throw;
        }
    }

    /// <summary>
    /// Shared setup for a bucket-level Lipila collection, regardless of payment
    /// type: resolves the bucket's wallet, loads what's still owed across the
    /// student's invoices in that bucket, validates the amount, decides the
    /// pro-rata invoice split, and persists the Pending PaymentGatewayTransaction
    /// row that ProcessLipilaCallbackAsync will later stage Payments from.
    /// </summary>
    private async Task<BucketCollectionContext> PrepareBucketCollectionAsync(
        Guid studentFinanceId,
        Guid bucketId,
        decimal amount,
        string paymentType,
        string accountNumber,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        var gatewayAccount = await ResolveBucketGatewayAccountAsync(bucketId, cancellationToken);
        var invoices = await LoadBucketInvoicesAsync(studentFinanceId, bucketId, cancellationToken);

        var totalOutstanding = invoices.Sum(i => i.Outstanding);

        if (invoices.Count == 0 || totalOutstanding <= 0)
        {
            throw new InvalidOperationException("There is nothing outstanding for this bucket.");
        }

        if (amount > totalOutstanding)
        {
            throw new InvalidOperationException(
                $"Payment amount {amount:F2} exceeds the outstanding balance of {totalOutstanding:F2} for this bucket.");
        }

        var breakdown = BuildInvoiceBreakdown(invoices, amount);

        var internalReference = GenerateInternalReference();
        var narration = invoices.Count == 1
            ? $"Payment for invoice {invoices[0].Invoice.InvoiceNumber ?? invoices[0].Invoice.Id.ToString()}"
            : $"Bucket payment covering {invoices.Count} invoices";
        var now = DateTime.UtcNow;

        // No Payment row(s) yet - they're only created once Lipila confirms success
        // (see StageBucketPaymentAsync), so the DB's payment triggers never touch
        // any invoice for a collection that hasn't actually settled. The breakdown
        // recorded here is what tells the webhook how to split the confirmed amount.
        var gatewayTransaction = new PaymentGatewayTransaction
        {
            Id = Guid.NewGuid(),
            PaymentId = null,
            InvoiceId = null,
            PaymentGatewayAccountId = gatewayAccount.Id,
            InternalReference = internalReference,
            Provider = Provider,
            TransactionType = "Collection",
            PaymentType = paymentType,
            Status = "Pending",
            Amount = amount,
            Currency = DefaultCurrency,
            AccountNumber = accountNumber,
            ReferenceData = bucketId.ToString(),
            Narration = narration,
            InvoiceBreakdownJson = JsonSerializer.Serialize(breakdown),
            CreatedAt = now
        };

        await _context.PaymentGatewayTransactions.AddAsync(gatewayTransaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new BucketCollectionContext(gatewayAccount, internalReference, narration, gatewayTransaction);
    }

    /// <summary>
    /// Embeds our own internal reference into the configured card return URL, so
    /// the return page can look up and display the outcome of the transaction the
    /// browser just came back from - independent of whatever query parameters
    /// Lipila itself may or may not append.
    /// </summary>
    private string BuildCardReturnUrl(string internalReference)
    {
        var baseUrl = _lipilaOptions.CardReturnUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return baseUrl;
        }

        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}ref={Uri.EscapeDataString(internalReference)}";
    }

    private sealed record BucketCollectionContext(
        PaymentGatewayAccount GatewayAccount,
        string InternalReference,
        string Narration,
        PaymentGatewayTransaction Transaction);

    public async Task<LipilaCollectionResponse> CheckLipilaPaymentStatusAsync(
        string referenceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
        {
            throw new ArgumentException("Reference ID is required.", nameof(referenceId));
        }

        var gatewayTransaction = await _context.PaymentGatewayTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.InternalReference == referenceId, cancellationToken);

        if (gatewayTransaction is null)
        {
            throw new KeyNotFoundException($"No Lipila transaction found for reference '{referenceId}'.");
        }

        var response = await _lipilaService.CheckCollectionStatusAsync(
            gatewayTransaction.PaymentGatewayAccountId,
            referenceId,
            cancellationToken);

        await ProcessLipilaCallbackAsync(response, cancellationToken);

        return response;
    }

    public async Task ProcessLipilaCallbackAsync(
        LipilaCollectionResponse callback,
        CancellationToken cancellationToken = default)
    {
        if (callback is null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        if (string.IsNullOrWhiteSpace(callback.ReferenceId))
        {
            _logger.LogWarning("Lipila callback rejected: missing referenceId.");
            return;
        }

        // Serializable isolation guarantees that two near-simultaneous deliveries
        // of the same callback (a real risk with webhooks) cannot both observe the
        // transaction as "not yet finalized" and double-process it. Disposing an
        // uncommitted transaction automatically rolls it back, so an unhandled
        // exception anywhere below is safe by default.
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var gatewayTransaction = await _context.PaymentGatewayTransactions
            .Include(t => t.Invoice)
                .ThenInclude(i => i.InvoiceBucketAllocations)
            .FirstOrDefaultAsync(t => t.InternalReference == callback.ReferenceId, cancellationToken);

        if (gatewayTransaction is null)
        {
            _logger.LogWarning(
                "Lipila callback received for unknown reference {ReferenceId}.",
                callback.ReferenceId);

            await dbTransaction.CommitAsync(cancellationToken);
            return;
        }

        if (IsTerminalStatus(gatewayTransaction.Status))
        {
            _logger.LogInformation(
                "Duplicate Lipila callback ignored for reference {ReferenceId}, already {Status}.",
                callback.ReferenceId,
                gatewayTransaction.Status);

            await dbTransaction.CommitAsync(cancellationToken);
            return;
        }

        var now = DateTime.UtcNow;

        gatewayTransaction.ProviderReferenceId = callback.ReferenceId;
        gatewayTransaction.ProviderIdentifier = callback.Identifier;
        gatewayTransaction.ProviderExternalId = callback.ExternalId;
        gatewayTransaction.PaymentType = callback.PaymentType ?? gatewayTransaction.PaymentType;
        gatewayTransaction.UpdatedAt = now;

        var incomingStatus = callback.Status?.Trim();

        if (string.Equals(incomingStatus, "Successful", StringComparison.OrdinalIgnoreCase))
        {
            var amountMatches = callback.Amount == gatewayTransaction.Amount;
            var currencyMatches = string.Equals(
                callback.Currency,
                gatewayTransaction.Currency,
                StringComparison.OrdinalIgnoreCase);

            if (!amountMatches || !currencyMatches)
            {
                gatewayTransaction.Status = "Failed";
                gatewayTransaction.Message =
                    $"Amount/currency mismatch. Expected {gatewayTransaction.Amount} {gatewayTransaction.Currency}, " +
                    $"received {callback.Amount} {callback.Currency}.";
                gatewayTransaction.CompletedAt = now;

                _logger.LogError(
                    "Lipila callback for reference {ReferenceId} failed validation: {Message}",
                    callback.ReferenceId,
                    gatewayTransaction.Message);

                await _context.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);
                return;
            }

            // Creating the Payment row(s) below is what triggers Finance.TR_Payment_UpdateInvoice,
            // TR_Payment_PreventOverpayment and trg_Payment_InsertLedger in the database - this is
            // deliberately the first and only place a Payment is created for a Lipila collection,
            // so the invoice/ledger only ever move for confirmed money. A bucket payment stages one
            // Payment per invoice it covers; a single-invoice payment stages exactly one.
            var staged = !string.IsNullOrWhiteSpace(gatewayTransaction.InvoiceBreakdownJson)
                ? await StageBucketPaymentAsync(gatewayTransaction, now, cancellationToken)
                : await StageSingleInvoicePaymentAsync(gatewayTransaction, now, cancellationToken);

            if (!staged)
            {
                gatewayTransaction.Status = "Failed";
                gatewayTransaction.Message = "This payment could not be matched to an invoice.";
                gatewayTransaction.CompletedAt = now;

                await _context.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);
                return;
            }

            gatewayTransaction.Status = "Successful";
            gatewayTransaction.Message = callback.Message;
            gatewayTransaction.CompletedAt = now;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await dbTransaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (IsOverpaymentRejection(ex))
            {
                // TR_Payment_PreventOverpayment rejected the insert (the invoice was
                // settled by something else between initiation and this confirmation).
                // Its RAISERROR + ROLLBACK happens server-side and takes our whole
                // ambient transaction with it, so dbTransaction is already dead -
                // detach everything staged here and record the failure in a fresh save.
                foreach (var entry in _context.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                try
                {
                    await dbTransaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    // The trigger's own ROLLBACK likely already tore this down server-side;
                    // this call is just to sync the client-side wrapper. Swallow and continue -
                    // the fresh save below is what actually matters.
                    _logger.LogDebug(rollbackEx, "Rollback after overpayment rejection was a no-op.");
                }

                _logger.LogWarning(
                    ex,
                    "Lipila payment for reference {ReferenceId} rejected: invoice already settled.",
                    callback.ReferenceId);

                var current = await _context.PaymentGatewayTransactions
                    .FirstAsync(t => t.Id == gatewayTransaction.Id, cancellationToken);

                current.Status = "Failed";
                current.Message = "Invoice was already settled before this payment could be confirmed.";
                current.CompletedAt = DateTime.UtcNow;
                current.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        if (string.Equals(incomingStatus, "Failed", StringComparison.OrdinalIgnoreCase))
        {
            gatewayTransaction.Status = "Failed";
            gatewayTransaction.Message = callback.Message;
            gatewayTransaction.CompletedAt = now;
        }
        else
        {
            // Not yet a terminal state (e.g. still "Pending") - persist the
            // provider metadata we have so far but wait for a terminal callback.
            if (!string.IsNullOrWhiteSpace(incomingStatus))
            {
                gatewayTransaction.Status = incomingStatus;
            }

            gatewayTransaction.Message = callback.Message;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
    }

    public async Task<LipilaWalletBalanceResponse> GetWalletBalanceAsync(
        Guid bucketId,
        CancellationToken cancellationToken = default)
    {
        var environment = NormalizeEnvironment(_lipilaOptions.Environment);

        var gatewayAccount = await _context.PaymentGatewayAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                g =>
                    g.BucketId == bucketId &&
                    g.Provider == Provider &&
                    g.IsActive == true &&
                    g.Environment == environment,
                cancellationToken);

        if (gatewayAccount is null)
        {
            throw new KeyNotFoundException(
                $"No active Lipila wallet is configured for bucket '{bucketId}' in the {environment} environment.");
        }

        return await _lipilaService.GetWalletBalanceAsync(gatewayAccount.Id, cancellationToken);
    }

    /// <summary>
    /// True if this invoice can be paid through Lipila right now (its fee buckets
    /// all resolve to a single active wallet with a credential configured). Lets
    /// the frontend decide gateway-vs-manual UI without attempting a real collection.
    /// </summary>
    public async Task<bool> IsInvoiceGatewayEligibleAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ResolveInvoiceBucketGatewayAsync(invoiceId, cancellationToken);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private async Task<(Invoice Invoice, PaymentGatewayAccount GatewayAccount)> ResolveInvoiceAndGatewayAccountAsync(
        Guid invoiceId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Payment amount must be greater than zero.");
        }

        var (invoice, gatewayAccount) = await ResolveInvoiceBucketGatewayAsync(invoiceId, cancellationToken);

        var outstanding = invoice.Amount - invoice.PaidAmount;

        if (amount > outstanding)
        {
            throw new InvalidOperationException(
                $"Payment amount {amount:F2} exceeds the outstanding balance of {outstanding:F2} " +
                $"on invoice {invoice.InvoiceNumber ?? invoice.Id.ToString()}.");
        }

        return (invoice, gatewayAccount);
    }

    /// <summary>
    /// Resolves the single Lipila wallet that covers all of an invoice's fee
    /// buckets, independent of any particular payment amount. Throws
    /// KeyNotFoundException if the invoice itself doesn't exist, or
    /// InvalidOperationException for any reason the invoice can't be paid via
    /// Lipila right now (no buckets, no active wallet, no credential, split wallets).
    /// </summary>
    private async Task<(Invoice Invoice, PaymentGatewayAccount GatewayAccount)> ResolveInvoiceBucketGatewayAsync(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .Include(i => i.InvoiceBucketAllocations)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            throw new KeyNotFoundException($"Invoice '{invoiceId}' was not found.");
        }

        if (invoice.InvoiceBucketAllocations.Count == 0)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.InvoiceNumber ?? invoice.Id.ToString()} has no fee bucket allocations configured.");
        }

        var bucketIds = invoice.InvoiceBucketAllocations
            .Select(a => a.BucketId)
            .Distinct()
            .ToList();

        var environment = NormalizeEnvironment(_lipilaOptions.Environment);

        var gatewayAccounts = await _context.PaymentGatewayAccounts
            .AsNoTracking()
            .Where(g =>
                bucketIds.Contains(g.BucketId) &&
                g.Provider == Provider &&
                g.IsActive == true &&
                g.Environment == environment)
            .Include(g => g.PaymentGatewayCredentials)
            .ToListAsync(cancellationToken);

        var missingBuckets = bucketIds.Except(gatewayAccounts.Select(g => g.BucketId)).Any();

        if (missingBuckets)
        {
            throw new InvalidOperationException(
                $"No active Lipila wallet is configured for invoice {invoice.InvoiceNumber ?? invoice.Id.ToString()} " +
                $"in the {environment} environment.");
        }

        var distinctAccountIds = gatewayAccounts.Select(g => g.Id).Distinct().ToList();

        if (distinctAccountIds.Count > 1)
        {
            throw new InvalidOperationException(
                $"Invoice {invoice.InvoiceNumber ?? invoice.Id.ToString()} spans multiple Lipila wallets. " +
                "Split-wallet online payments are not supported yet.");
        }

        var gatewayAccount = gatewayAccounts[0];

        if (!gatewayAccount.PaymentGatewayCredentials.Any())
        {
            throw new InvalidOperationException(
                $"The Lipila wallet for invoice {invoice.InvoiceNumber ?? invoice.Id.ToString()} " +
                "has no API key configured yet.");
        }

        return (invoice, gatewayAccount);
    }

    /// <summary>
    /// Resolves the single active Lipila wallet configured directly for a bucket,
    /// independent of any particular invoice or amount.
    /// </summary>
    private async Task<PaymentGatewayAccount> ResolveBucketGatewayAccountAsync(
        Guid bucketId,
        CancellationToken cancellationToken)
    {
        var environment = NormalizeEnvironment(_lipilaOptions.Environment);

        var gatewayAccount = await _context.PaymentGatewayAccounts
            .AsNoTracking()
            .Include(g => g.PaymentGatewayCredentials)
            .FirstOrDefaultAsync(
                g =>
                    g.BucketId == bucketId &&
                    g.Provider == Provider &&
                    g.IsActive == true &&
                    g.Environment == environment,
                cancellationToken);

        if (gatewayAccount is null)
        {
            throw new InvalidOperationException(
                $"No active Lipila wallet is configured for this bucket in the {environment} environment.");
        }

        if (!gatewayAccount.PaymentGatewayCredentials.Any())
        {
            throw new InvalidOperationException(
                "The Lipila wallet for this bucket has no API key configured yet.");
        }

        return gatewayAccount;
    }

    /// <summary>
    /// All of one student's invoices that allocate money to this bucket, paired
    /// with how much of that allocation is still outstanding (original allocation
    /// minus whatever's already been paid toward this specific invoice+bucket).
    /// Only invoices with a positive remainder are included.
    /// </summary>
    private async Task<List<(Invoice Invoice, decimal Outstanding)>> LoadBucketInvoicesAsync(
        Guid studentFinanceId,
        Guid bucketId,
        CancellationToken cancellationToken)
    {
        var invoices = await _context.Invoices
            .Include(i => i.InvoiceBucketAllocations)
            .Where(i =>
                i.StudentFinanceId == studentFinanceId &&
                i.InvoiceBucketAllocations.Any(a => a.BucketId == bucketId))
            .ToListAsync(cancellationToken);

        if (invoices.Count == 0)
        {
            return new List<(Invoice, decimal)>();
        }

        var paidByInvoiceBucket = await LoadPaidByInvoiceBucketAsync(
            invoices.Select(i => i.Id).ToList(),
            cancellationToken);

        var result = new List<(Invoice, decimal)>();

        foreach (var invoice in invoices)
        {
            var allocated = invoice.InvoiceBucketAllocations
                .Where(a => a.BucketId == bucketId)
                .Sum(a => a.Amount);

            var paid = paidByInvoiceBucket.TryGetValue((invoice.Id, bucketId), out var p) ? p : 0m;
            var outstanding = allocated - paid;

            if (outstanding > 0)
            {
                result.Add((invoice, outstanding));
            }
        }

        return result;
    }

    private async Task<Dictionary<(Guid InvoiceId, Guid BucketId), decimal>> LoadPaidByInvoiceBucketAsync(
        List<Guid> invoiceIds,
        CancellationToken cancellationToken)
    {
        var rows = await _context.PaymentAllocations
            .AsNoTracking()
            .Where(a => invoiceIds.Contains(a.InvoiceId))
            .GroupBy(a => new { a.InvoiceId, a.BucketId })
            .Select(g => new { g.Key.InvoiceId, g.Key.BucketId, Paid = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => (x.InvoiceId, x.BucketId), x => x.Paid);
    }

    /// <summary>
    /// Splits an amount being paid toward a bucket across the invoices that make
    /// it up, pro-rata to each invoice's outstanding share, oldest due date first.
    /// The last invoice absorbs any rounding remainder so the parts always sum
    /// exactly to amountToPay.
    /// </summary>
    private static List<InvoiceBreakdownEntry> BuildInvoiceBreakdown(
        List<(Invoice Invoice, decimal Outstanding)> invoices,
        decimal amountToPay)
    {
        var ordered = invoices.OrderBy(i => i.Invoice.DueDate).ToList();
        var totalOutstanding = ordered.Sum(i => i.Outstanding);
        var remaining = amountToPay;
        var breakdown = new List<InvoiceBreakdownEntry>();

        for (var i = 0; i < ordered.Count; i++)
        {
            decimal share;

            if (i == ordered.Count - 1)
            {
                share = remaining;
            }
            else
            {
                share = totalOutstanding == 0
                    ? 0m
                    : Math.Round(
                        amountToPay * (ordered[i].Outstanding / totalOutstanding),
                        2,
                        MidpointRounding.AwayFromZero);

                remaining -= share;
            }

            if (share <= 0)
            {
                continue;
            }

            breakdown.Add(new InvoiceBreakdownEntry
            {
                InvoiceId = ordered[i].Invoice.Id,
                Amount = share
            });
        }

        return breakdown;
    }

    private sealed class InvoiceBreakdownEntry
    {
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Stages a Payment for a legacy single-invoice gateway transaction (one
    /// InvoiceId, no breakdown). Returns false if the invoice couldn't be loaded.
    /// </summary>
    private async Task<bool> StageSingleInvoicePaymentAsync(
        PaymentGatewayTransaction gatewayTransaction,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var invoice = gatewayTransaction.Invoice;

        if (invoice is null)
        {
            return false;
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            AmountPaid = gatewayTransaction.Amount,
            PaymentDate = now,
            PaymentMethod = BuildPaymentMethodLabel(gatewayTransaction.PaymentType),
            CreatedAt = now,
            SchoolID = invoice.SchoolID
        };

        var allocations = BuildAllocations(invoice, payment, gatewayTransaction.Amount, now);

        await _context.Payments.AddAsync(payment, cancellationToken);
        await _context.PaymentAllocations.AddRangeAsync(allocations, cancellationToken);

        gatewayTransaction.PaymentId = payment.Id;
        return true;
    }

    /// <summary>
    /// Stages one Payment per invoice recorded in a bucket transaction's
    /// InvoiceBreakdownJson, each allocated to the bucket the transaction's
    /// wallet belongs to. Returns false if the breakdown is missing/empty or
    /// none of its invoices could be resolved.
    /// </summary>
    private async Task<bool> StageBucketPaymentAsync(
        PaymentGatewayTransaction gatewayTransaction,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var breakdown = JsonSerializer.Deserialize<List<InvoiceBreakdownEntry>>(
            gatewayTransaction.InvoiceBreakdownJson!);

        if (breakdown is null || breakdown.Count == 0)
        {
            return false;
        }

        var bucketId = await _context.PaymentGatewayAccounts
            .AsNoTracking()
            .Where(g => g.Id == gatewayTransaction.PaymentGatewayAccountId)
            .Select(g => g.BucketId)
            .FirstOrDefaultAsync(cancellationToken);

        var invoiceIds = breakdown.Select(b => b.InvoiceId).ToList();

        var invoices = await _context.Invoices
            .Where(i => invoiceIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        Guid? firstPaymentId = null;

        foreach (var entry in breakdown)
        {
            if (entry.Amount <= 0 || !invoices.TryGetValue(entry.InvoiceId, out var invoice))
            {
                continue;
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                AmountPaid = entry.Amount,
                PaymentDate = now,
                PaymentMethod = BuildPaymentMethodLabel(gatewayTransaction.PaymentType),
                CreatedAt = now,
                SchoolID = invoice.SchoolID
            };

            var allocation = new PaymentAllocation
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                BucketId = bucketId,
                Amount = entry.Amount,
                CreatedAt = now
            };

            await _context.Payments.AddAsync(payment, cancellationToken);
            await _context.PaymentAllocations.AddAsync(allocation, cancellationToken);

            firstPaymentId ??= payment.Id;
        }

        if (firstPaymentId is null)
        {
            return false;
        }

        // A bucket payment covers several invoices, so several Payment rows get
        // created above - PaymentId can only reference one. It's kept as a
        // best-effort pointer to the first for quick lookups; InvoiceBreakdownJson
        // (already on this row) remains the authoritative record of the full split.
        gatewayTransaction.PaymentId = firstPaymentId;
        return true;
    }

    private static List<PaymentAllocation> BuildAllocations(
        Invoice invoice,
        Payment payment,
        decimal amountPaid,
        DateTime now)
    {
        var buckets = invoice.InvoiceBucketAllocations
            .OrderBy(a => a.CreatedAt)
            .ToList();

        var totalBucketAmount = buckets.Sum(a => a.Amount);
        var remaining = amountPaid;
        var allocations = new List<PaymentAllocation>();

        for (var i = 0; i < buckets.Count; i++)
        {
            decimal share;

            if (i == buckets.Count - 1)
            {
                share = remaining;
            }
            else
            {
                share = totalBucketAmount == 0
                    ? 0m
                    : Math.Round(
                        amountPaid * (buckets[i].Amount / totalBucketAmount),
                        2,
                        MidpointRounding.AwayFromZero);

                remaining -= share;
            }

            if (share <= 0)
            {
                continue;
            }

            allocations.Add(new PaymentAllocation
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                BucketId = buckets[i].BucketId,
                Amount = share,
                CreatedAt = now
            });
        }

        return allocations;
    }

    private async Task<LipilaPaymentInitiationResult> ApplyProviderResponseAsync(
        PaymentGatewayTransaction transaction,
        LipilaCollectionResponse response,
        CancellationToken cancellationToken)
    {
        transaction.ProviderReferenceId = response.ReferenceId;
        transaction.ProviderIdentifier = response.Identifier;
        transaction.ProviderExternalId = response.ExternalId;
        transaction.Status = string.IsNullOrWhiteSpace(response.Status) ? "Pending" : response.Status;
        transaction.Message = response.Message;
        transaction.CheckoutUrl = response.CardRedirectionUrl;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new LipilaPaymentInitiationResult
        {
            TransactionId = transaction.Id,
            ReferenceId = transaction.InternalReference,
            Status = transaction.Status,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            PaymentType = transaction.PaymentType,
            CardRedirectionUrl = response.CardRedirectionUrl
        };
    }

    private async Task MarkTransactionFailedAsync(
        PaymentGatewayTransaction transaction,
        string message,
        CancellationToken cancellationToken)
    {
        transaction.Status = "Failed";
        transaction.Message = message;
        transaction.CompletedAt = DateTime.UtcNow;
        transaction.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string BuildPaymentMethodLabel(string? paymentType) =>
        string.Equals(paymentType, "Card", StringComparison.OrdinalIgnoreCase)
            ? "Lipila-Card"
            : "Lipila-MobileMoney";

    private static bool IsOverpaymentRejection(DbUpdateException ex) =>
        ex.InnerException is SqlException sqlEx &&
        sqlEx.Message.Contains("exceeds invoice amount", StringComparison.OrdinalIgnoreCase);

    private static bool IsTerminalStatus(string? status) =>
        string.Equals(status, "Successful", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeEnvironment(string? environment) =>
        string.IsNullOrWhiteSpace(environment) ? "Sandbox" : environment.Trim();

    private static string GenerateInternalReference() =>
        $"IGN-{Guid.NewGuid():N}";

    private static string NormalizeZambianPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        }

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (digits.Length == 10 && digits.StartsWith('0'))
        {
            digits = "260" + digits[1..];
        }
        else if (digits.Length == 9)
        {
            digits = "260" + digits;
        }

        if (digits.Length != 12 || !digits.StartsWith("260"))
        {
            throw new ArgumentException(
                "Phone number must be a valid Zambian mobile number, e.g. 260977123456.",
                nameof(phoneNumber));
        }

        return digits;
    }
}
