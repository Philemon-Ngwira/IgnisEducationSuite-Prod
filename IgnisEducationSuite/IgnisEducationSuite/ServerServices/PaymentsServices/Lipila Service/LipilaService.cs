using EduSphereDomain.FinanceData;
using EDUSphereSharedProject.FinanceModels;
using EDUSphereSharedProject.PaymentDTos.Lipila;
using IgnisEducationSuite.ServerServices.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace IgnisEducationSuite.ServerServices.PaymentsServices.Lipila_Service;

public sealed class LipilaService : ILipilaService
{
    private readonly HttpClient _httpClient;
    private readonly PhoenixEdusphereFinanceContext _context;
    private readonly LipilaConfiguration _options;
    private readonly IEncryptionService _encryptionService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LipilaService(
        HttpClient httpClient,
        PhoenixEdusphereFinanceContext context,
        IOptions<LipilaConfiguration> options,
        IEncryptionService encryptionService)
    {
        _httpClient = httpClient;
        _context = context;
        _options = options.Value;
        _encryptionService = encryptionService;
    }

    public async Task<LipilaCollectionResponse>
        CreateMobileMoneyCollectionAsync(
            Guid paymentGatewayAccountId,
            LipilaMobileMoneyCollectionRequest request,
            CancellationToken cancellationToken = default)
    {
        var (account, apiKey) =
            await GetGatewayAccountAsync(
                paymentGatewayAccountId,
                cancellationToken);

        var baseUrl = GetBaseUrl();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/api/v1/collections/mobile-money");

        AddCommonHeaders(
            httpRequest,
            apiKey);

        httpRequest.Content =
            JsonContent.Create(
                request,
                options: RequestJsonOptions);

        return await SendAsync<LipilaCollectionResponse>(
            httpRequest,
            cancellationToken);
    }

    public async Task<LipilaCollectionResponse>
        CreateCardCollectionAsync(
            Guid paymentGatewayAccountId,
            LipilaCardCollectionRequest request,
            CancellationToken cancellationToken = default)
    {
        var (account, apiKey) =
            await GetGatewayAccountAsync(
                paymentGatewayAccountId,
                cancellationToken);

        var baseUrl = GetBaseUrl();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/api/v1/collections/card");

        AddCommonHeaders(
            httpRequest,
            apiKey);

        httpRequest.Content =
            JsonContent.Create(
                request,
                options: RequestJsonOptions);

        return await SendAsync<LipilaCollectionResponse>(
            httpRequest,
            cancellationToken);
    }

    public async Task<LipilaCollectionResponse>
        CheckCollectionStatusAsync(
            Guid paymentGatewayAccountId,
            string referenceId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
        {
            throw new ArgumentException(
                "Reference ID is required.",
                nameof(referenceId));
        }

        var (account, apiKey) =
            await GetGatewayAccountAsync(
                paymentGatewayAccountId,
                cancellationToken);

        var baseUrl = GetBaseUrl();

        var encodedReference =
            Uri.EscapeDataString(referenceId);

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/api/v1/collections/check-status" +
            $"?referenceId={encodedReference}");

        AddCommonHeaders(
            httpRequest,
            apiKey,
            includeCallback: false);

        return await SendAsync<LipilaCollectionResponse>(
            httpRequest,
            cancellationToken);
    }

    public async Task<LipilaWalletBalanceResponse>
        GetWalletBalanceAsync(
            Guid paymentGatewayAccountId,
            CancellationToken cancellationToken = default)
    {
        var (account, apiKey) =
            await GetGatewayAccountAsync(
                paymentGatewayAccountId,
                cancellationToken);

        var baseUrl = GetBaseUrl();

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/api/v1/merchants/balance");

        AddCommonHeaders(
            httpRequest,
            apiKey,
            includeCallback: false);

        return await SendAsync<LipilaWalletBalanceResponse>(
            httpRequest,
            cancellationToken);
    }

    private async Task<(
        PaymentGatewayAccount Account,
        string ApiKey)>
        GetGatewayAccountAsync(
            Guid paymentGatewayAccountId,
            CancellationToken cancellationToken)
    {
        var account =
            await _context.PaymentGatewayAccounts
                .AsNoTracking()
                .Include(x => x.PaymentGatewayCredentials)
                .FirstOrDefaultAsync(
                    x =>
                        x.Id == paymentGatewayAccountId &&
                        x.IsActive == true,
                    cancellationToken);

        if (account is null)
        {
            throw new InvalidOperationException(
                $"Payment gateway account '{paymentGatewayAccountId}' " +
                "was not found or is inactive.");
        }

        if (!string.Equals(
                account.Provider,
                "Lipila",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Payment gateway account '{paymentGatewayAccountId}' " +
                $"belongs to provider '{account.Provider}', not Lipila.");
        }

        var credential =
            account.PaymentGatewayCredentials
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();

        if (credential is null)
        {
            throw new InvalidOperationException(
                $"No Lipila credentials are configured for " +
                $"payment gateway account '{paymentGatewayAccountId}'.");
        }

        if (string.IsNullOrWhiteSpace(
                credential.EncryptedSecret))
        {
            throw new InvalidOperationException(
                $"The Lipila credential for payment gateway account " +
                $"'{paymentGatewayAccountId}' does not contain an encrypted secret.");
        }

        var apiKey =
            _encryptionService.Decrypt(
                credential.EncryptedSecret,
                credential.KeyVersion);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "The decrypted Lipila API key is empty.");
        }

        return (account, apiKey);
    }

    private void AddCommonHeaders(
        HttpRequestMessage request,
        string apiKey,
        bool includeCallback = true)
    {
        request.Headers.TryAddWithoutValidation(
            "accept",
            "application/json");

        request.Headers.TryAddWithoutValidation(
            "x-api-key",
            apiKey);

        if (includeCallback &&
            !string.IsNullOrWhiteSpace(_options.CallbackUrl))
        {
            request.Headers.TryAddWithoutValidation(
                "callbackUrl",
                _options.CallbackUrl);
        }
    }

    private string GetBaseUrl()
    {
        var environment =
            _options.Environment?.Trim();

        if (string.Equals(
                environment,
                "Production",
                StringComparison.OrdinalIgnoreCase))
        {
            return _options.ProductionBaseUrl.TrimEnd('/');
        }

        if (string.Equals(
                environment,
                "Sandbox",
                StringComparison.OrdinalIgnoreCase))
        {
            return _options.SandboxBaseUrl.TrimEnd('/');
        }

        throw new InvalidOperationException(
            $"Unsupported Lipila environment '{environment}'. " +
            "Expected 'Sandbox' or 'Production'.");
    }

    private async Task<T>
        SendAsync<T>(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
    {
        using var response =
            await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

        var content =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateLipilaException(
                response.StatusCode,
                content);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "Lipila returned an empty response.");
        }

        try
        {
            var result =
                JsonSerializer.Deserialize<T>(
                    content,
                    JsonOptions);

            return result
                ?? throw new InvalidOperationException(
                    "Lipila returned an empty response object.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Lipila returned an invalid response.",
                ex);
        }
    }

    private static Exception CreateLipilaException(
        HttpStatusCode statusCode,
        string responseContent)
    {
        return new HttpRequestException(
            $"Lipila API request failed. " +
            $"HTTP {(int)statusCode} ({statusCode}). " +
            $"Response: {responseContent}");
    }
}